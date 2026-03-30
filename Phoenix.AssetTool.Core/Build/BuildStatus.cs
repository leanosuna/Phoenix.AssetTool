namespace Phoenix.AssetTool.Core.Build
{
    public class BuildStatus
    {
        public BuildState State { get; internal set; }
        public string Message { get; internal set; } = default!;
        public List<AssetBuildStatus> BuildList { get; internal set; } = new();
        public Action? OnChange { get; set; }
    }

    public enum BuildState
    {
        OK,
        FAILED,
        BUSY
    }
}
