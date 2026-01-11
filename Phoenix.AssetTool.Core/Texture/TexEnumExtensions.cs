using Phoenix.AssetImport.Texture;
using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetTool.Core.Texture
{
    public static class TexEnumExtensions
    {
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
                TextureWrap.Repeat=> 0,
                TextureWrap.MirroredRepeat=> 1,
                TextureWrap.ClampToEdge=> 2,
                TextureWrap.ClampToBorder=> 3,
                _=> 0
            };
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
                TextureFilter.Linear =>1 ,
                TextureFilter.NearestMipmapNearest =>2 ,
                TextureFilter.LinearMipmapNearest =>3 ,
                TextureFilter.NearestMipmapLinear =>4 ,
                TextureFilter.LinearMipmapLinear =>5 ,
                _ => 0
            };
        }

    }
}
