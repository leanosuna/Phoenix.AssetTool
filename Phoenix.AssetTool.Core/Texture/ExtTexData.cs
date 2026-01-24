using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Phoenix.AssetTool.Core.Texture
{
    public class ExtTexData
    {
        public string Name = default!;
        public string OutputPath= default!;
        public TextureLoadOptions Options= default!;
        public byte[] PixelData= default!;
        public Vector2 Size = default!;
        public bool Compressed => Size.Y == 0;
        public Task BuildTask = default!;

    }
}
