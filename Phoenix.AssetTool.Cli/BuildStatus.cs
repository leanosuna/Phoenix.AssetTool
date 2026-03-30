using Phoenix.AssetTool.Core.Build;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Cli
{
    public class BuildStatus
    {
        public BuildState State { get; internal set; }
        public string Message { get; internal set; } = default!;
        public List<AssetBuildStatus> BuildList { get; internal set; } = default!;
        public Action? OnChange { get; set; }
    }

    public enum BuildState
    {
        OK,
        FAILED,
        BUSY
    }
}
