using BCnEncoder.Shared;
using Phoenix.AssetImport.Texture;
using Phoenix.AssetTool.Core.Texture;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public static class EnumExtensions
    {
        public static CompressionFormat ConvertFormat(this AssetCompressionFormat format)
        {
            return format switch
            {
                AssetCompressionFormat.RGBA => CompressionFormat.Rgba,
                AssetCompressionFormat.BC1 => CompressionFormat.Bc1,
                AssetCompressionFormat.BC3 => CompressionFormat.Bc3,
                AssetCompressionFormat.BC5 => CompressionFormat.Bc5,
                _ => CompressionFormat.Rgba,
            };
        }

        public static string[] Strings(this AssetCompressionFormat format)
        {
            return [
                "RGBA - No Compression",
                "BC1 - DXT1",
                "BC3 - DXT5",
                "BC5 - RGTC2"
            ];
        }

        public static TextureWrap At(this TextureWrap t, int index)
        {
            return index switch
            {
                0 => TextureWrap.Repeat,
                1 => TextureWrap.MirroredRepeat,
                2 => TextureWrap.ClampToEdge,
                3 => TextureWrap.ClampToBorder,
                _ => TextureWrap.Repeat
            };
        }


        public static int Index(this TextureWrap val)
        {
            return val switch
            {
                TextureWrap.Repeat => 0,
                TextureWrap.MirroredRepeat => 1,
                TextureWrap.ClampToEdge => 2,
                TextureWrap.ClampToBorder => 3,
                _=> 0
            };
        }
        public static string[] Strings(this TextureWrap wrap)
        {
            return [
                "Repeat",
                "Mirrored Repeat",
                "Clamp To Edge",
                "Clamp To Border"
            ];
        }

        public static TextureFilter At(this TextureFilter t, int index)
        {
            return index switch
            {
                0 => TextureFilter.Nearest,
                1 => TextureFilter.Linear,
                2 => TextureFilter.NearestMipmapNearest,
                3 => TextureFilter.LinearMipmapNearest,
                4 => TextureFilter.NearestMipmapLinear,
                5 => TextureFilter.LinearMipmapLinear,
                _ => TextureFilter.Linear
            };
        }
        public static int Index(this TextureFilter val)
        {
            return val switch
            {
                TextureFilter.Nearest => 0,
                TextureFilter.Linear => 1,
                TextureFilter.NearestMipmapNearest => 2,
                TextureFilter.LinearMipmapNearest => 3,
                TextureFilter.NearestMipmapLinear => 4,
                TextureFilter.LinearMipmapLinear => 5,
                _ => 0
            };
        }
        public static string[] Strings(this TextureFilter filter)
        {
            return [
                "Nearest",
                "Linear",
                "Nearest Mipmap Nearest",
                "Linear Mipmap Nearest",
                "Nearest Mipmap Linear",
                "Linear Mipmap Linear"
            ];
        }

    }
}
