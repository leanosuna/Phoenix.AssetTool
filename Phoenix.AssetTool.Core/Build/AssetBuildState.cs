using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Build
{
    public enum AssetBuildState
    {
        Pending,
        Building,
        Encoding,
        Built,
        Skipped,
        Failed
    }
}
