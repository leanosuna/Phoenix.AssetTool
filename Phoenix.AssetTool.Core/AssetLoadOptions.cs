using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public class AssetLoadOptions
    {
        public Dictionary<string, ModelLoadOptions> Models{ get; set; } = new();
        public Dictionary<string, TextureLoadOptions> Textures { get; set; } = new();
        public Dictionary<string, ShaderLoadOptions> Shaders { get; set; } = new();
    }
}
