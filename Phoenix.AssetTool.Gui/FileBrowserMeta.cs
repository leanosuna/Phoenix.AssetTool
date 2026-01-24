using Phoenix.AssetTool.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetTool.Gui
{
    internal class FileBrowserMeta
    {
        public string RelativePath { get; set; } = default!;
        public string Name { get; set; } = default!;
        public AssetEntry Asset { get; set; } = default!;
        public AssetType Type { get; set; } = default!;
        public Vector4 Color { get; set; } = default!;
        public bool Tracked { get; set; } = false;
        public bool Built { get; set; } = false;

    }
}
