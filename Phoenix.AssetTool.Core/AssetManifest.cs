using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public sealed class AssetManifest
    {
        public string BaseDirectory { get; set; } = "";
        public List<AssetEntry> Assets { get; set; } = new();
    }
}
