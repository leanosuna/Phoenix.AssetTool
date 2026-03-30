using System.Numerics;
using System.Runtime.InteropServices;

namespace Phoenix.AssetTool.Core.Model
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoords;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector4 BoneIds;
        public Vector4 Weights;
        
    }
}