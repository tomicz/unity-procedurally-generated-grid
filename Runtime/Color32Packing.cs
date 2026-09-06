using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TOMICZ.Grid
{
    /// <summary>
    /// Packs Color32 arrays into raw RGBA bytes for serialization. Unity writes a
    /// byte[] as a single hex string, where a Color32[] costs three YAML lines per
    /// entry, so a large grid's colors would otherwise dominate the scene file.
    /// </summary>
    public static class Color32Packing
    {
        public const int BytesPerColor = 4;

        /// <summary>Copies colors into a byte array, reusing <paramref name="reuse"/> when it is the right size.</summary>
        public static byte[] Pack(Color32[] colors, byte[] reuse = null)
        {
            int length = colors == null ? 0 : colors.Length * BytesPerColor;
            byte[] bytes = reuse != null && reuse.Length == length ? reuse : new byte[length];

            if (length > 0)
            {
                MemoryMarshal.AsBytes(colors.AsSpan()).CopyTo(bytes);
            }

            return bytes;
        }

        /// <summary>Copies packed bytes into a color array, reusing <paramref name="reuse"/> when it is the right size.</summary>
        public static Color32[] Unpack(byte[] bytes, Color32[] reuse = null)
        {
            int length = bytes == null ? 0 : bytes.Length / BytesPerColor;
            Color32[] colors = reuse != null && reuse.Length == length ? reuse : new Color32[length];

            if (length > 0)
            {
                MemoryMarshal.Cast<byte, Color32>(bytes.AsSpan(0, length * BytesPerColor)).CopyTo(colors);
            }

            return colors;
        }
    }
}
