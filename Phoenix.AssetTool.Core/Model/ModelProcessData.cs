using Phoenix.AssetTool.Core.Build;
using Phoenix.AssetTool.Core.Model.Animation;
using Phoenix.AssetTool.Core.Texture;
using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Model
{
    internal unsafe class ModelProcessData
    {
        public AssetBuildStatus Status = default!;
        public ModelLoadOptions LoadOptions = default!;
        public AnimationLoadData AnimationLoadData = default!;


        public List<ModelPart> Parts = new List<ModelPart>();
        public Dictionary<string, BoneInfo> BoneInfoMap = new Dictionary<string, BoneInfo>();
        public Scene* Scene = default!;
        public ExtTexData[] textureData = default!;
        public List<Animation.Animation> Animations = default!;
    }
}
