using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class Transform
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

        public Transform Interpolate(Transform other, float factor)
        {
            return new Transform(
                Vector3.Lerp(Scale, other.Scale, factor),
                Quaternion.Slerp(Rotation, other.Rotation, factor),
                Vector3.Lerp(Translation, other.Translation, factor));
        }

        public Matrix4x4 AsMatrix()
        {
            return Matrix4x4.CreateScale(Scale)
                   * Matrix4x4.CreateFromQuaternion(Rotation)
                   * Matrix4x4.CreateTranslation(Translation);
        }
    }
}
