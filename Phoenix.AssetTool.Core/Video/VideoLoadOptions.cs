using Phoenix.AssetTool.Core.Texture;

namespace Phoenix.AssetTool.Core.Video
{
    public class VideoLoadOptions
    {
        public float FrameRate { get; set; } = 30f;
        public int MaxWidth { get; set; } = 1920;
        public int MaxHeight { get; set; } = 1080;
        public AssetCompressionFormat Format { get; set; } = AssetCompressionFormat.BC3;
        public bool GenerateMipMaps { get; set; } = false;
        public bool ExtractAudio { get; set; } = true;
    }
}
