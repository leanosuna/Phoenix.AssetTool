using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class AnimatorNode
    {
        public string Name;
        public int Level;
        public Matrix4x4 BindTransform { get; private set; }
        public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;

        public Matrix4x4 Offset { get; private set; }
        public int ParentID { get; private set; }
        public int ModelBoneID { get; private set; }
        public bool IsBone { get; private set; }

        public AnimatorNode(Matrix4x4 transform, int parentID, int modelBoneID, Matrix4x4 offset)
        {
            BindTransform = transform;
            ParentID = parentID;
            ModelBoneID = modelBoneID;
            IsBone = true;
            Offset = offset;
        }

        public AnimatorNode(Matrix4x4 transform, int parentID)
        {
            BindTransform = transform;
            ParentID = parentID;
            ModelBoneID = -1;
            IsBone = false;
            Offset = Matrix4x4.Identity;
        }
    }
}
