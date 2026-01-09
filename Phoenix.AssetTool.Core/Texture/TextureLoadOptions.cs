using BCnEncoder.Shared;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Texture
{
    public class TextureLoadOptions
    {
        public bool GenerateMipMaps { get; set; } = true;

        public bool WrapS { get; set; } = GLEnum.DecrWrap;

        public CompressionFormat Format { get; set; } = CompressionFormat.Rgba;
    }
}
