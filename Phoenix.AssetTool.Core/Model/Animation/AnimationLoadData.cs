using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class AnimationLoadData
    {
        public Matrix4x4 InverseGlobalTransform { get; internal set; } = Matrix4x4.Identity;
        public List<AnimatorNode> AnimatorNodes { get; internal set; } = new List<AnimatorNode>();
        public bool ModelHierarchySet { get; internal set; } = false;
        public Dictionary<string, BoneInfo> BoneInfoMap { get; internal set; } = default!;
    }
}
