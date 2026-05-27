using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public sealed class AssetManifest
    {
        public List<AssetEntry> Assets { get; set; } = new();
        public string Namespace { get; set; } = "Phoenix.Framework.ShaderHelpers";
        public bool DarkTheme { get; set; } = true;
    }
}
