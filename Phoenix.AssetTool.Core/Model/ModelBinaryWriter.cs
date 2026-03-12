using Phoenix.AssetImport.Texture;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Model.Animation;
using Phoenix.AssetTool.Core.Texture;
using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using Silk.NET.Core.Native;
using System;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpPPS = Silk.NET.Assimp.PostProcessSteps;
using AssimpTex = Silk.NET.Assimp.Texture;
using Buffer = System.Buffer;
namespace Phoenix.AssetTool.Core.Model
{
    public sealed class ModelBinaryWriter
    {
        public const AssimpPPS DefaultAssimpPost =
            AssimpPPS.Triangulate |
            AssimpPPS.GenerateSmoothNormals |
            AssimpPPS.GenerateUVCoords |
            AssimpPPS.FindInvalidData |
            AssimpPPS.FlipUVs |
            AssimpPPS.JoinIdenticalVertices |
            AssimpPPS.ImproveCacheLocality |
            AssimpPPS.SortByPrimitiveType |
            AssimpPPS.LimitBoneWeights;


        public static unsafe List<string> Build(AssetBuildStatus status, ModelLoadOptions options, 
            string sourcePath, string outputPath)
        {
            List<string> texNames = new List<string>();

            var assimp = Assimp.GetApi();
            var scene = assimp.ImportFile(sourcePath, options.AssimpFlags);

            if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
            {
                var error = assimp.GetErrorStringS();
                status.State = AssetBuildState.Failed;
                status.Error = error;
                throw new Exception(error);
            }

            var modelProcessData = new ModelProcessData { Status = status, Scene = scene, LoadOptions = options };

            ProcessNode(scene->MRootNode, Matrix4x4.Identity, modelProcessData);

            status.Step = 0;
            status.MaxSteps = 1;

            if (options.ExtractTextures)
                texNames = ExtractTextures(outputPath, modelProcessData);
            
            if(options.IsAnimated)
            {
                var (animations, data) = AnimationLoader.ProcessAnimations(options.AnimationFiles, modelProcessData.BoneInfoMap);
                modelProcessData.Animations = animations;
                modelProcessData.AnimationLoadData = data;
            }


            WriteBinary(outputPath, modelProcessData);
            Interlocked.Increment(ref status.Step);
            _ = AwaitTexBinaries(modelProcessData);
            //ReadBinary(outputPath);
            return texNames;
        }
        private static unsafe List<string> ExtractTextures(string modelOutputPath, ModelProcessData modelProcessData)
        {
            List<string> texNames = new List<string>();
            var scene = modelProcessData.Scene;
            int texCount = (int)scene->MNumTextures;
            if (scene == null ||  texCount == 0)
                return texNames;

            modelProcessData.Status.MaxSteps += texCount;

            var modelRoot = Path.GetDirectoryName(modelOutputPath)!;

            modelProcessData.textureData = new ExtTexData[texCount];
            for (int i = 0; i < texCount; i++)
            {
                var tex = scene->MTextures[i];

                string name = Marshal.PtrToStringAnsi((nint)tex->MFilename.Data) ?? $"*{i}";
                name = Path.GetFileNameWithoutExtension(name);
                texNames.Add(name);
                
                var outputPath = Path.Combine(modelRoot, $"{name}.bin");
                var isNormal = name.Contains("Normal", StringComparison.InvariantCultureIgnoreCase);
                var loadOptions = new TextureLoadOptions
                {
                    GenerateMipMaps = true,
                    Format = isNormal? 
                            AssetCompressionFormat.BC5: 
                            AssetCompressionFormat.BC3,
                    WrapS = TextureWrap.Repeat,
                    WrapT = TextureWrap.Repeat,
                    Min = TextureFilter.LinearMipmapLinear,
                    Mag = TextureFilter.Linear
                };


                var sizeInBytes = 0;
                var size = new Vector2(tex->MWidth, tex->MHeight);
                var compressed = tex->MHeight == 0;
                sizeInBytes = compressed? (int)tex->MWidth : (int)(size.X * size.Y * 4);

                modelProcessData.textureData[i] = new ExtTexData()
                {
                    Name = name,
                    OutputPath = outputPath,
                    Options = loadOptions,
                    PixelData = new byte[sizeInBytes],
                    Size = size
                };
                fixed (byte* dst = modelProcessData.textureData[i].PixelData)
                {
                    Buffer.MemoryCopy(tex->PcData, dst, sizeInBytes, sizeInBytes);
                }
                
            }
            for (int i = 0; i < texCount; i++)
            {
                int index = i;
                var etd = modelProcessData.textureData[index];

                etd.BuildTask = etd.Compressed ?
                    Task.Run(() => {
                        TextureBinaryWriter.Build(etd.PixelData, modelProcessData.Status, etd.Options, etd.OutputPath);
                        Interlocked.Increment(ref modelProcessData.Status.Step);

                    }) :
                    Task.Run(() => {
                        TextureBinaryWriter.Build(etd.Size, etd.PixelData, modelProcessData.Status, etd.Options, etd.OutputPath);
                        Interlocked.Increment(ref modelProcessData.Status.Step);

                    });
            }


            return texNames;
        }
        static async Task AwaitTexBinaries(ModelProcessData modelProcessData)
        {
            await Task.WhenAll(modelProcessData.textureData.Select(t => t.BuildTask));
            modelProcessData.Status.State = AssetBuildState.Built;
        }

        private static void WriteBinary(string outputPath, ModelProcessData modelProcessData)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var fs = System.IO.File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            var options = modelProcessData.LoadOptions;
            var parts = modelProcessData.Parts;

            bw.Write("PHXM");   // custom file identifier
            bw.Write((uint)1);  // version
            bw.Write(options.IsAnimated);
            bw.Write(parts.Count);
            //Log.Debug($"parts {parts.Count}");

            foreach (var part in parts)
            {
                bw.Write(part.Name);
                bw.Write(part.Meshes.Count);
                //Log.Debug($"part {part.Name}");

                foreach (var mesh in part.Meshes)
                {
                    bw.Write(mesh.Name);
                    bw.Write(mesh.MaterialIndex);
                    bw.Write(mesh.Transform);

                    bw.Write(mesh.Indices.Length);
                    bw.Write(mesh.Indices);
                    
                    //Log.Debug($"m {mesh.Name}");
                    //var tv = mesh.Vertices[0];
                    //Log.Debug($"test w{tv.Weights.ToStrF2()} bid {tv.BoneIds.ToStrInt()}");

                    bw.Write(mesh.Vertices.Length);

                    //foreach(var v in mesh.Vertices)
                    //{
                    //    Log.Debug($"bid {v.BoneIds.ToStrInt()} w {v.Weights.ToStrF2()}");
                    //}
                    bw.Write(mesh.Vertices);
                }
            }
            bw.Write(options.ExtractTextures);

            if(options.ExtractTextures)
            {
                bw.Write(modelProcessData.textureData.Length);
                foreach(var tex in modelProcessData.textureData)
                {
                    bw.Write(tex.Name);
                }
            }
            
            if (options.IsAnimated)
            {
                var data = modelProcessData.AnimationLoadData;
                bw.Write(data.InverseGlobalTransform);

                bw.Write(data.AnimatorNodes.Count);
                //Log.Debug($"nodes {data.AnimatorNodes.Count}");

                foreach (var node in data.AnimatorNodes)
                {                   
                    bw.Write(node.Name);
                    bw.Write(node.IsBone);
                    bw.Write(node.ParentID);
                    bw.Write(node.ModelBoneID);
                    bw.Write(node.Offset);
                    bw.Write(node.BindTransform);
                    bw.Write(node.Transform);

                    var b = node.IsBone ? "B" : "N";
                    //Log.Debug($"{b} {node.Name} {node.ParentID} {node.ModelBoneID} {node.Offset.ToStrF2()} {node.BindTransform.ToStrF2()} {node.Transform.ToStrF2()}");

                }

                bw.Write(modelProcessData.Animations.Count());
                var boneCount = data.BoneInfoMap.Count;
                bw.Write(boneCount);
                //Log.Debug($"bc {boneCount}");
                foreach (var an in modelProcessData.Animations)
                {
                    //Log.Debug($"name {an.Name}");
                    //Log.Debug($"d {an.Duration}");
                    //Log.Debug($"tps {an.TicksPerSecond}");

                    bw.Write(an.Name);
                    bw.Write(an.Duration);
                    bw.Write(an.TicksPerSecond);

                    for(var b = 0; b < boneCount; b++)
                    {
                        var boneKeyFrames = an.Keyframes[b];
                        var keyFramesLen = boneKeyFrames.Length;
                        bw.Write(keyFramesLen);
                        //Log.Debug($"b{b} kf {keyFramesLen}");
                        for (var k = 0; k < keyFramesLen; k++)
                        {
                            var keyFrame = boneKeyFrames[k];
                            bw.Write(keyFrame.TimeStamp);
                            bw.Write(keyFrame.SRT);

                            //Log.Debug($"b{b} rot {keyFrame.TimeStamp} {keyFrame.SRT.Rotation.ToStr()}");
                        }
                    }
                }
            }
        }

        private unsafe static void ProcessNode(Node* node, Matrix4x4 parentTransform, ModelProcessData modelProcessData)
        {
            var meshes = new List<Mesh>();

            var nTransform = node->MTransformation;
            nTransform = Matrix4x4.Transpose(nTransform);
            Matrix4x4 currentTransform = parentTransform * nTransform;
            var relativeTransform = currentTransform;
            
            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var assimpMesh = modelProcessData.Scene->MMeshes[node->MMeshes[i]];

                var mesh = ProcessMesh(assimpMesh, relativeTransform, modelProcessData);
                mesh.Name = assimpMesh->MName;
                mesh.AABB = assimpMesh->MAABB;
                mesh.Transform = relativeTransform;

                meshes.Add(mesh);

            }

            var name = "no-name";
            if (!string.IsNullOrEmpty(node->MName))
                name = node->MName;

            if (meshes.Count > 0)
                modelProcessData.Parts.Add(new ModelPart(name, meshes));

            for (var i = 0; i < node->MNumChildren; i++)
            {
                ProcessNode(node->MChildren[i], currentTransform, modelProcessData);
            }
        }
        
        private unsafe static Mesh ProcessMesh(AssimpMesh* mesh, Matrix4x4 relativeTransform, ModelProcessData modelProcessData)
        {
            // data to fill
            List<Vertex> vertices = new List<Vertex>();
            List<uint> indices = new List<uint>();
            //List<GLTexture> textures = new List<GLTexture>();

            // walk through each of the mesh's vertices
            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                Vertex vertex = new Vertex();

                //Console.WriteLine($"mesh bones{mesh->MNumBones}");
                for (int b = 0; b < Vertex.MAX_BONE_INFLUENCE; b++)
                {
                    vertex.BoneIds[b] = -1;
                    vertex.Weights[b] = 0.0f;
                }


                //vertex.Position = Vector3.Transform(mesh->MVertices[i], transform);
                vertex.Position = mesh->MVertices[i];

                // normals
                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];
                // tangent
                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];
                // bitangent
                if (mesh->MBitangents != null)
                    vertex.Bitangent = mesh->MBitangents[i];

                // texture coordinates
                if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
                {
                    // a vertex can contain up to 8 different texture coordinates. We thus make the assumption that we won't 
                    // use models where a vertex can have multiple texture coordinates so we always take the first set (0).
                    Vector3 texcoord3 = mesh->MTextureCoords[0][i];
                    vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
                }

                vertices.Add(vertex);
            }

            // now walk through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                // retrieve all indices of the face and store them in the indices vector
                for (uint j = 0; j < face.MNumIndices; j++)
                    indices.Add(face.MIndices[j]);
            }

            if(modelProcessData.LoadOptions.IsAnimated)
                ExtractBoneWeights(vertices, mesh, modelProcessData);

            //if (_meshAttributes.HasFlag(MeshAttributes.boneIds) && _meshAttributes.HasFlag(MeshAttributes.boneWeights))
            //{
            //}


            //var result = new Mesh(GL, _meshAttributes, vertices, indices.ToArray(), _saveVerticesIndices);
            var aabb = mesh->MAABB;
            var materialIndex = mesh->MMaterialIndex;
            var name = "no-name";
            if (!string.IsNullOrEmpty(mesh->MName))
                name = mesh->MName;
            return new Mesh(vertices, indices, relativeTransform, name, aabb, materialIndex);
        }

        private unsafe static void ExtractBoneWeights(List<Vertex> vertices, AssimpMesh* mesh, ModelProcessData modelProcessData)
        {
            // Temporary dictionary to collect all influences per vertex
            var vertexInfluences = new Dictionary<int, List<(int BoneId, float Weight)>>();

            var boneInfoMap = modelProcessData.BoneInfoMap;
            for (int boneID = 0; boneID < mesh->MNumBones; boneID++)
            {
                string boneName = mesh->MBones[boneID]->MName;


                var offset = mesh->MBones[boneID]->MOffsetMatrix;
                var weights = mesh->MBones[boneID]->MWeights;
                var numWeights = mesh->MBones[boneID]->MNumWeights;

                int trueBoneId;
                if (boneInfoMap.TryGetValue(boneName, out var boneInfo))
                {
                    trueBoneId = boneInfo.ID;
                }
                else
                {
                    trueBoneId = boneInfoMap.Count;
                    boneInfoMap.Add(boneName, new BoneInfo(trueBoneId, offset));

                }

                for (int wi = 0; wi < numWeights; wi++)
                {
                    int vertexId = (int)weights[wi].MVertexId;
                    float weight = weights[wi].MWeight;
                    if (!vertexInfluences.TryGetValue(vertexId, out var list))
                    {
                        list = new List<(int, float)>();
                        vertexInfluences[vertexId] = list;
                    }

                    list.Add((trueBoneId, weight));
                }
            }

            foreach (var kvp in vertexInfluences)
            {
                int vertexId = kvp.Key;
                var influences = kvp.Value;

                List<(int BoneId, float Weight)> topInfluences;

                if (influences.Count > Vertex.MAX_BONE_INFLUENCE)
                {
                    influences.Sort((a, b) => b.Weight.CompareTo(a.Weight));
                    topInfluences = influences.Take(Vertex.MAX_BONE_INFLUENCE).ToList();

                    float total = topInfluences.Sum(x => x.Weight);
                    if (total > 0)
                    {
                        for (int i = 0; i < topInfluences.Count; i++)
                            topInfluences[i] = (topInfluences[i].BoneId, topInfluences[i].Weight / total);
                    }

                }
                else
                {
                    topInfluences = influences;
                    for (int i = topInfluences.Count; i < Vertex.MAX_BONE_INFLUENCE; i++)
                    {
                        topInfluences.Add((-1, 0.0f));
                    }
                }

                var vertex = vertices[vertexId];
                vertex.BoneIds =
                    new Vector4(topInfluences[0].BoneId, topInfluences[1].BoneId, topInfluences[2].BoneId, topInfluences[3].BoneId);
                vertex.Weights =
                    new Vector4(topInfluences[0].Weight, topInfluences[1].Weight, topInfluences[2].Weight, topInfluences[3].Weight);
                vertices[vertexId] = vertex;
            }
        }



    }

}
