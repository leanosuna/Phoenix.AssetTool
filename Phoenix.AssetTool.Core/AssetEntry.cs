using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public sealed class AssetEntry
    {
        public string RelativePath { get; set; } = "";

        public string OutputFilePath { get; set; } = "";
        
        public AssetType Type { get; set; }
    }
}
