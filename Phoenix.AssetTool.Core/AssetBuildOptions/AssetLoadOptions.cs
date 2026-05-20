using Phoenix.AssetTool.Core.Model;
using Phoenix.AssetTool.Core.Shader;
using Phoenix.AssetTool.Core.Texture;
using Phoenix.AssetTool.Core.Video;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.AssetBuildOptions
{
    public class AssetLoadOptions
    {
        public ConcurrentDictionary<string, ModelLoadOptions> Models{ get; set; } = new();
        public ConcurrentDictionary<string, TextureLoadOptions> Textures { get; set; } = new();
        public ConcurrentDictionary<string, ShaderLoadOptions> Shaders { get; set; } = new();
        public ConcurrentDictionary<string, VideoLoadOptions> Videos { get; set; } = new();
    }
}
