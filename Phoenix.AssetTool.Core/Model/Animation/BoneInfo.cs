using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class BoneInfo
    {
        public int ID { get; set; }
        public Matrix4x4 Offset { get; set; }

        public BoneInfo(int id, Matrix4x4 offset)
        {
            ID = id;
            Offset = offset;
        }
    }
}
