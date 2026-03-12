using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Phoenix.AssetTool.Core
{
    public static class BinaryWriterTools
    {
        public static int Write<T>(this BinaryWriter bw, T[] value)
            where T : unmanaged
        {
            var spanT = value.AsSpan();
            var span = MemoryMarshal.AsBytes(spanT);
            
            bw.Write(span);
            return span.Length;
        }

        public static int Write<T>(this BinaryWriter bw, T value)
            where T : unmanaged
        {
            var span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateSpan(ref value, 1));
            bw.Write(span);
            return span.Length;
        }
    }
}
