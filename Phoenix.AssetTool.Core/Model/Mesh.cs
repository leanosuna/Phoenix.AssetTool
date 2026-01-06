using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetTool.Core.Model
{
    public class Mesh
    {
        public Vertex[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }

        public string Name { get; internal set; } = string.Empty;
        public Box3D<float> AABB { get; internal set; }
        public uint MaterialIndex { get; internal set; }
        public Matrix4x4 Transform { get; internal set; }

        public Mesh(List<Vertex> vertices, List<uint> indices, Matrix4x4 transform, string name, Box3D<float> aABB, uint materialIndex)
        {
            Vertices = vertices.ToArray();
            Indices = indices.ToArray();
            Name = name;
            AABB = aABB;
            MaterialIndex = materialIndex;
            Transform = transform;
        }
    }
}
