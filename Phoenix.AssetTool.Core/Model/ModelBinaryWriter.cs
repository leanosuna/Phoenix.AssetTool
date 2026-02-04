using Phoenix.AssetImport.Texture;
using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Texture;
using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using Silk.NET.Core.Native;
using System;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AssimpTex = Silk.NET.Assimp.Texture;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpPPS = Silk.NET.Assimp.PostProcessSteps;
using Buffer = System.Buffer;
using Phoenix.AssetTool.Core.Model.Animation;
namespace Phoenix.AssetTool.Core.Model
{
    public sealed class ModelBinaryWriter
    {
        //TODO: this is isnt async safe
        private static List<Task> _tasks = new List<Task>();
        private static ExtTexData[] textureData;
        private static Dictionary<string, BoneInfo> BoneInfoMap = new Dictionary<string, BoneInfo>();
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

            var parts = new List<ModelPart>();
            BoneInfoMap.Clear();
            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, parts, options);

            status.Step = 0;
            status.MaxSteps = 1;
            if (options.ExtractTextures)
                texNames = ExtractTextures(status, scene, outputPath);
            
            if(options.IsAnimated)
            {
                AnimationLoader.ProcessAnimations(options.AnimationFiles, BoneInfoMap);
            }


            WriteBinary(options, outputPath, parts);
            Interlocked.Increment(ref status.Step);
            _ = AwaitTexBinaries(status);
            //ReadBinary(outputPath);
            return texNames;
        }
        private static unsafe List<string> ExtractTextures(AssetBuildStatus status, Scene* scene, string modelOutputPath)
        {
            List<string> texNames = new List<string>();
            int texCount = (int)scene->MNumTextures;
            if (scene == null ||  texCount == 0)
                return texNames;
            status.MaxSteps += texCount;

            var modelRoot = Path.GetDirectoryName(modelOutputPath)!;
                        
            textureData = new ExtTexData[texCount];
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
                
                textureData[i] = new ExtTexData()
                {
                    Name = name,
                    OutputPath = outputPath,
                    Options = loadOptions,
                    PixelData = new byte[sizeInBytes],
                    Size = size
                };
                fixed (byte* dst = textureData[i].PixelData)
                {
                    Buffer.MemoryCopy(tex->PcData, dst, sizeInBytes, sizeInBytes);
                }
                
            }
            for (int i = 0; i < texCount; i++)
            {
                int index = i;
                var etd = textureData[index];

                etd.BuildTask = etd.Compressed ?
                    Task.Run(() => {
                        TextureBinaryWriter.Build(etd.PixelData, status, etd.Options, etd.OutputPath);
                        Interlocked.Increment(ref status.Step);

                    }) :
                    Task.Run(() => {
                        TextureBinaryWriter.Build(etd.Size, etd.PixelData, status, etd.Options, etd.OutputPath);
                        Interlocked.Increment(ref status.Step);

                    });
            }


            return texNames;
        }
        static async Task AwaitTexBinaries(AssetBuildStatus state)
        {
            await Task.WhenAll(textureData.Select(t => t.BuildTask));
            state.State = AssetBuildState.Built;
        }

        private static void WriteBinary(ModelLoadOptions options, string outputPath, List<ModelPart> parts)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var fs = System.IO.File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            bw.Write("PHXM");   // custom file identifier
            bw.Write((uint)1);  // version
            bw.Write(options.ExtractTextures);
            bw.Write(parts.Count);
            //Console.WriteLine($"parts {parts.Count}");
            foreach (var part in parts)
            {
                bw.Write(part.Name);
                bw.Write(part.Meshes.Count);
                //Console.WriteLine($"name {part.Name}");
                //Console.WriteLine($"meshes {part.Meshes.Count}");
                foreach (var mesh in part.Meshes)
                {
                    bw.Write(mesh.Name);
                    bw.Write(mesh.MaterialIndex);
                    WriteStruct(bw, mesh.Transform);

                    bw.Write(mesh.Indices.Length);
                    WriteArray(bw, mesh.Indices);
                    
                    bw.Write(mesh.Vertices.Length);
                    WriteArray(bw, mesh.Vertices);
                    
                    //Console.WriteLine($"indices {mesh.Indices.Length}");
                    //Console.WriteLine($"vertices {mesh.Vertices.Length}");
                }
            }
        }

        public static void ReadBinary(string path)
        {
            //Console.WriteLine("reading...");
            using var fs = System.IO.File.OpenRead(path);
            using var br = new BinaryReader(fs);

            var fileID = br.ReadString();
            var ver = br.ReadUInt32();

            var extractTextures = br.ReadBoolean();
            Console.WriteLine($"extract {extractTextures}");
            var partsCount = br.ReadInt32();
            //Console.WriteLine($"FILE ID {fileID}");
            //Console.WriteLine($"VER {ver}");
            //Console.WriteLine($"parts: {partsCount}");

            for(int p = 0; p < partsCount; p++)
            {
                var partName = br.ReadString();
                var meshCount = br.ReadInt32();

                //Console.WriteLine($"name: {partName}");
                //Console.WriteLine($"meshes: {meshCount}");

                for(int m = 0; m < meshCount; m++)
                {
                    var meshName = br.ReadString();
                    var index = br.ReadInt32();
                    var transform = ReadStruct<Matrix4x4>(br);
                    var indicesLength = br.ReadInt32();
                    var indices = ReadArray<uint>(br, indicesLength);
                    var verticesLength = br.ReadInt32();
                    var vertices = ReadArray<Vertex>(br, verticesLength);
                    
                    //Console.WriteLine($"mName: {meshName}");
                    //Console.WriteLine($"mIndex {index}");
                    //Console.WriteLine($"indices {indicesLength}");
                    //Console.WriteLine($"vertices {verticesLength}");

                }
            }

        }

        public static void WriteArray<T>(BinaryWriter bw, T[] value)
            where T : unmanaged
        {
            var spanT = value.AsSpan();
            var span = MemoryMarshal.AsBytes(spanT);
            bw.Write(span);           
        }

        public static void WriteStruct<T>(BinaryWriter bw, T value)
            where T : unmanaged
        {
            var span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateSpan(ref value, 1));
            bw.Write(span);
        }

        public static T ReadStruct<T>(BinaryReader br)
            where T : unmanaged
        {
            T value = default;
            var span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateSpan(ref value, 1));
            br.Read(span);
            return value;
        }

        public static T[] ReadArray<T>(BinaryReader br, int count)
            where T : unmanaged
        {
            var array = new T[count];
            var span = MemoryMarshal.AsBytes(array.AsSpan());

            int bytesToRead = span.Length;
            int bytesRead = 0;

            while (bytesRead < bytesToRead)
            {
                int read = br.Read(span.Slice(bytesRead));
                if (read == 0)
                    throw new EndOfStreamException();

                bytesRead += read;
            }

            return array;
        }

        private unsafe static void ProcessNode(Node* node, Scene* scene, 
            Matrix4x4 parentTransform, List<ModelPart> parts,
            ModelLoadOptions options)
        {
            var meshes = new List<Mesh>();

            var nTransform = node->MTransformation;
            nTransform = Matrix4x4.Transpose(nTransform);
            Matrix4x4 currentTransform = parentTransform * nTransform;
            var relativeTransform = currentTransform;

            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var assimpMesh = scene->MMeshes[node->MMeshes[i]];

                var mesh = ProcessMesh(assimpMesh, scene, relativeTransform, options);
                mesh.Name = assimpMesh->MName;
                mesh.AABB = assimpMesh->MAABB;
                mesh.Transform = relativeTransform;

                meshes.Add(mesh);

            }

            var name = "no-name";
            if (!string.IsNullOrEmpty(node->MName))
                name = node->MName;

            if (meshes.Count > 0)
                parts.Add(new ModelPart(name, meshes));

            for (var i = 0; i < node->MNumChildren; i++)
            {
                ProcessNode(node->MChildren[i], scene, currentTransform, parts, options);
            }
        }


        
        private unsafe static Mesh ProcessMesh(AssimpMesh* mesh, Scene* scene, 
            Matrix4x4 relativeTransform, ModelLoadOptions options)
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

            if(options.IsAnimated)
                ExtractBoneWeights(vertices, mesh, scene);

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

        private unsafe static void ExtractBoneWeights(List<Vertex> vertices, AssimpMesh* mesh, Scene* scene)
        {
            // Temporary dictionary to collect all influences per vertex
            var vertexInfluences = new Dictionary<int, List<(int BoneId, float Weight)>>();

            for (int boneID = 0; boneID < mesh->MNumBones; boneID++)
            {
                string boneName = mesh->MBones[boneID]->MName;


                var offset = mesh->MBones[boneID]->MOffsetMatrix;
                var weights = mesh->MBones[boneID]->MWeights;
                var numWeights = mesh->MBones[boneID]->MNumWeights;

                int trueBoneId;
                if (BoneInfoMap.TryGetValue(boneName, out var boneInfo))
                {
                    trueBoneId = boneInfo.ID;
                }
                else
                {
                    trueBoneId = BoneInfoMap.Count;
                    BoneInfoMap.Add(boneName, new BoneInfo(trueBoneId, offset));

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
