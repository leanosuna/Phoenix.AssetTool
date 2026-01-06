using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Model
{
    public class ModelLoadOptions
    {
        public bool ExtractTextures { get; set; } = false;
        public uint AssimpFlags { get; set; } = (uint)
           (PostProcessSteps.Triangulate |
            PostProcessSteps.GenerateSmoothNormals |
            PostProcessSteps.GenerateUVCoords |
            PostProcessSteps.FindInvalidData |
            PostProcessSteps.FlipUVs |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.ImproveCacheLocality |
            PostProcessSteps.SortByPrimitiveType |
            PostProcessSteps.LimitBoneWeights);

        public bool IsAnimated { get; set; } = false;
        public List<string> AnimationFiles = new();
    }
}
