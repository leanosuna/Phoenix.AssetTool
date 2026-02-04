using BCnEncoder.Encoder;
using Phoenix.AssetTool.Core.Build;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Numerics;


namespace Phoenix.AssetTool.Core.Texture
{
    public static class TextureBinaryWriter
    {
        public static void Build(byte[] compressed, AssetBuildStatus status, 
            TextureLoadOptions options, string outputPath)
        {
            
            using Image<Rgba32> image = Image.Load<Rgba32>(compressed);

            (Vector2 size, byte[] buffer) data = ImageToBytes(image);
            InternalBuild(data.size, data.buffer, status, options, outputPath);
            
        }
        public static void Build(AssetBuildStatus status, TextureLoadOptions options, 
            string sourcePath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using Image<Rgba32> image = Image.Load<Rgba32>(sourcePath);
            
            (Vector2 size, byte[] buffer) data = ImageToBytes(image);
            InternalBuild(data.size, data.buffer, status, options, outputPath);
        }

        public static void Build(Vector2 size, byte[] buffer, AssetBuildStatus status, 
            TextureLoadOptions options, string outputPath)
        {
            InternalBuild(size, buffer, status, options, outputPath);
        }


        private static void InternalBuild(Vector2 size, byte[] pixelData, AssetBuildStatus status, 
            TextureLoadOptions options, string outputPath)
        {
            var w = (int)size.X;
            var h = (int)size.Y;

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
            List<Vector2> mipSizes = [new Vector2(w, h)];

            if (options.GenerateMipMaps)
            {
                mipCount = encoder.CalculateNumberOfMipLevels(w, h);
                for (int i = 1; i < mipCount; i++)
                {
                    encoder.CalculateMipMapSize(w, h, i, out int mWidth, out int mHeight);
                    mipSizes.Add(new Vector2(mWidth, mHeight));
                }
            }

            status.State = AssetBuildState.Encoding;
            var encodedBytes = encoder.EncodeToRawBytes(pixelData, w, h, PixelFormat.Rgba32);
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
                bw.Write((int)mipSizes[i].X);
                bw.Write((int)mipSizes[i].Y);
                bw.Write(encodedBytes[i].Length);
                bw.Write(encodedBytes[i]);

            }
        }

        private static (Vector2 size, byte[] data) ImageToBytes(Image<Rgba32> image)
        {
            int w = image.Width;
            int h = image.Height;

            (Vector2 s, byte[] d) = (new Vector2(w, h), new byte[w * h * 4]);
            image.CopyPixelDataTo(d);

            return (s, d);
        }

    }

}
