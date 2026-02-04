using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class ModelBoneHierarchyNode
    {
        public string Name { get; private set; }

        public Matrix4x4 Transform { get; private set; }

        public bool IsBone { get; private set; }
        public Matrix4x4 Offset { get; private set; }
        public List<ModelBoneHierarchyNode> Children { get; private set; } = new List<ModelBoneHierarchyNode>();

        public ModelBoneHierarchyNode(string name, Matrix4x4 transform, List<ModelBoneHierarchyNode> children)
        {
            Name = name;
            Transform = transform;
            Children = children;
            IsBone = false;
            Offset = Matrix4x4.Identity;
        }
        public ModelBoneHierarchyNode(string name, Matrix4x4 transform, List<ModelBoneHierarchyNode> children, Matrix4x4 offset)
        {
            Name = name;
            Transform = transform;
            Children = children;
            IsBone = true;
            Offset = offset;
        }

    }
}
