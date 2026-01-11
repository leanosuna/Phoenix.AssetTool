using System;
using System.Collections.Generic;
using System.Text;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using Phoenix.AssetTool.Core.Build;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Phoenix.AssetTool.Core.Texture
{
    public static class TextureBinaryWriter
    {
        public static void Build(
            AssetBuildStatus status,
            TextureLoadOptions options,
            string sourcePath,
            string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using Image<Rgba32> image = Image.Load<Rgba32>(sourcePath);

            int width = image.Width;
            int height = image.Height;
            byte[] pixelData = new byte[width * height * 4];
            image.CopyPixelDataTo(pixelData);

            var format = options.Format.ConvertFormat();
            var encoder = new BcEncoder
            {
                OutputOptions =
                {
                    Format = format,
                    GenerateMipMaps = options.GenerateMipMaps,
                    Quality = CompressionQuality.Balanced
                }
            };
            
            var mipCount = 1;
            List<(int w, int h)> mipSizes = [(width, height)];
            
            if (options.GenerateMipMaps)
            {
                mipCount = encoder.CalculateNumberOfMipLevels(width, height);
                for(int i = 1; i < mipCount; i++)
                {
                    encoder.CalculateMipMapSize(width, height, i, out int mWidth, out int mHeight);
                    mipSizes.Add((mWidth, mHeight));
                }
            }

            status.State = AssetBuildState.Encoding;
            var encodedBytes = encoder.EncodeToRawBytes(pixelData, width, height, PixelFormat.Rgba32);
            status.State = AssetBuildState.Building;
            var encodedMips = encodedBytes.GetLength(0);

            if (encodedMips != mipCount)
            {
                throw new Exception($"encoded mips {encodedMips} != {mipCount}");
            }


            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            bw.Write("PHXT");                 // magic
            bw.Write((uint)1);                // version

            bw.Write((int)options.WrapS);
            bw.Write((int)options.WrapT);
            bw.Write((int)options.Min);
            bw.Write((int)options.Mag);

            bw.Write((byte)options.Format);
            bw.Write(mipCount);
            
            for (int i = 0; i < mipCount; i++)
            {
                bw.Write(mipSizes[i].w);
                bw.Write(mipSizes[i].h);
                bw.Write(encodedBytes[i].Length);
                bw.Write(encodedBytes[i]);

            }
        }
    }

}
