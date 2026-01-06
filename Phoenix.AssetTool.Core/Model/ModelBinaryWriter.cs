using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpPPS = Silk.NET.Assimp.PostProcessSteps;

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
        public static unsafe void Build(ModelLoadOptions options, string sourcePath, string outputPath)
        {

            var assimp = Assimp.GetApi();
            var scene = assimp.ImportFile(sourcePath, options.AssimpFlags);

            if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
            {
                var error = assimp.GetErrorStringS();
                throw new Exception(error);
            }

            var parts = new List<ModelPart>();

            ProcessNode(scene->MRootNode, scene, Matrix4x4.Identity, parts);

            WriteBinary(options, outputPath, parts);

            ReadBinary(outputPath);
        }

        private static void WriteBinary(ModelLoadOptions options, string outputPath, List<ModelPart> parts)
        {
            //Console.WriteLine("saving...");
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

        private unsafe static void ProcessNode(Node* node, Scene* scene, Matrix4x4 parentTransform, List<ModelPart> parts)
        {
            var meshes = new List<Mesh>();

            var nTransform = node->MTransformation;
            nTransform = Matrix4x4.Transpose(nTransform);
            Matrix4x4 currentTransform = parentTransform * nTransform;
            var relativeTransform = currentTransform;

            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var assimpMesh = scene->MMeshes[node->MMeshes[i]];

                var mesh = ProcessMesh(assimpMesh, scene, relativeTransform);
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
                ProcessNode(node->MChildren[i], scene, currentTransform, parts);
            }
        }


        
        private unsafe static Mesh ProcessMesh(AssimpMesh* mesh, Scene* scene, Matrix4x4 relativeTransform)
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


            //if (_meshAttributes.HasFlag(MeshAttributes.boneIds) && _meshAttributes.HasFlag(MeshAttributes.boneWeights))
            //{
            //    ExtractBoneWeights(vertices, mesh, scene);
            //}


            //var result = new Mesh(GL, _meshAttributes, vertices, indices.ToArray(), _saveVerticesIndices);
            var aabb = mesh->MAABB;
            var materialIndex = mesh->MMaterialIndex;
            var name = "no-name";
            if (!string.IsNullOrEmpty(mesh->MName))
                name = mesh->MName;
            return new Mesh(vertices, indices, relativeTransform, name, aabb, materialIndex);
        }

    }

}
