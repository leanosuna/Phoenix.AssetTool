using BCnEncoder;
using BCnEncoder.Shared;
using Phoenix.AssetImport.Texture;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Texture
{
    public class TextureLoadOptions
    {
        public bool GenerateMipMaps { get; set; } = true;

        public AssetCompressionFormat Format { get; set; } = AssetCompressionFormat.BC3;

        public TextureWrap WrapS { get; set; } = TextureWrap.Repeat;
        public TextureWrap WrapT { get; set; } = TextureWrap.Repeat;
        public TextureFilter Min { get; set; } = TextureFilter.LinearMipmapLinear;
        public TextureFilter Mag { get; set; } = TextureFilter.Linear;
        public float Anisotropic { get; set; } = 0;

    }
}
