using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public sealed class AssetBuildStatus
    {
        public AssetEntry Asset = default!;
        public AssetBuildState State = AssetBuildState.Pending;
        public string? Error;
    }
}
