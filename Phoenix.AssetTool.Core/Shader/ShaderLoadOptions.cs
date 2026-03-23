using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Shader
{
    public class ShaderLoadOptions
    {
        public bool AutoRebuild { get; set; } = false;

        public string OutputDir { get; set; } = "";
    }
}
