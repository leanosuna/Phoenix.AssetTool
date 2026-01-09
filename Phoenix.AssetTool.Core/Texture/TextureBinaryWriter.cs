using System;
using System.Collections.Generic;
using System.Text;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Phoenix.AssetTool.Core.Texture
{
    public static class TextureBinaryWriter
    {
        public static void Build(
            TextureLoadOptions options,
            string sourcePath,
            string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using Image<Rgba32> image = Image.Load<Rgba32>(sourcePath);

            Span<byte> pixelData = default!;
            image.CopyPixelDataTo(pixelData);

            int width = image.Width;
            int height = image.Height;
            
            var encoder = new BcEncoder
            {
                OutputOptions =
                {
                    Format = options.Format,
                    GenerateMipMaps = options.GenerateMipMaps,
                    Quality = CompressionQuality.Balanced
                }
            };

            var mipCount = 1;
            var mipSizes = new List<(int,int)>();
            
            if (options.GenerateMipMaps)
                mipCount = encoder.CalculateNumberOfMipLevels(width, height);
            
            for(int i = 0; i < mipCount; i++)
            {
                encoder.CalculateMipMapSize(width, height, i, out int mWidth, out int mHeight);
                mipSizes.Add((mWidth, mHeight));
            }

            var encodedBytes = encoder.EncodeToRawBytes(pixelData, width, height, PixelFormat.Rgba32);

            var encodedMips = encodedBytes.GetLength(0);

            if (encodedMips != mipCount)
            {
                throw new Exception($"encoded mips {encodedMips} != {mipCount}");
            }


            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            bw.Write("PHXT");                 // magic
            bw.Write((uint)1);                // version

            bw.Write(width);
            bw.Write(height);

            bw.Write((byte)options.Format);
            bw.Write(mipCount);
            
            for (int i = 0; i < mipSizes.Count; i++)
            {
                bw.Write(mipSizes[i]);
                bw.Write(mipSizes[i]);
            }

            foreach (var mip in mips)
                mip.Dispose();
        }

        // =====================================================
        // Mip generation
        // =====================================================

        private static List<Image<Rgba32>> GenerateMipChain(Image<Rgba32> baseImage)
        {
            var mips = new List<Image<Rgba32>>();
            mips.Add(baseImage.Clone());

            int w = baseImage.Width;
            int h = baseImage.Height;

            while (w > 1 || h > 1)
            {
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);

                var next = mips[^1].Clone(ctx =>
                    ctx.Resize(w, h, KnownResamplers.Box));

                mips.Add(next);
            }

            return mips;
        }

    }

}
