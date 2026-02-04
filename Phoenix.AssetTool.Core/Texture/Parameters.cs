using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetImport.Texture
{
    public enum TextureWrap : int
    {
        Repeat = 0x2901,   // GL_REPEAT
        MirroredRepeat = 0x8370,   // GL_MIRRORED_REPEAT
        ClampToEdge = 0x812F,   // GL_CLAMP_TO_EDGE
        ClampToBorder = 0x812D    // GL_CLAMP_TO_BORDER
    }
    public enum TextureFilter : int
    {
        Nearest = 0x2600,   // GL_NEAREST
        Linear = 0x2601,   // GL_LINEAR

        NearestMipmapNearest = 0x2700,   // GL_NEAREST_MIPMAP_NEAREST
        LinearMipmapNearest = 0x2701,   // GL_LINEAR_MIPMAP_NEAREST
        NearestMipmapLinear = 0x2702,   // GL_NEAREST_MIPMAP_LINEAR
        LinearMipmapLinear = 0x2703    // GL_LINEAR_MIPMAP_LINEAR
    }

    
}
