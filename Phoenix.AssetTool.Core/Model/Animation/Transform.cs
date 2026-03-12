using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public struct Transform
    {
        public Vector3 Scale { get; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Translation { get; }

        public Transform(Vector3 scale, Quaternion rotation, Vector3 translation)
        {
            Scale = scale;
            Rotation = rotation;
            Translation = translation;
        }
    }
}
