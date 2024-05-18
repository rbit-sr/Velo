using System.Runtime.InteropServices;

namespace Velo
{
    public class Bytes
    {
        public static void Write(short value, byte[] buffer, ref int off)
        {
            buffer[off] = (byte)(value);
            buffer[off + 1] = (byte)(value >> 8);
            off += 2;
        }

        public static void Read(ref short value, byte[] buffer, ref int off)
        {
            value = (short)(
                buffer[off] |
                buffer[off + 1] << 8);
            off += 2;
        }

        public static void Write(int value, byte[] buffer, ref int off)
        {
            buffer[off]     = (byte)(value);
            buffer[off + 1] = (byte)(value >> 8);
            buffer[off + 2] = (byte)(value >> 16);
            buffer[off + 3] = (byte)(value >> 24);
            off += 4;
        }

        public static void Read(ref int value, byte[] buffer, ref int off)
        {
            value =
                buffer[off] |
                buffer[off + 1] << 8 |
                buffer[off + 2] << 16 |
                buffer[off + 3] << 24;
            off += 4;
        }

        public static void Write(uint value, byte[] buffer, ref int off)
        {
            buffer[off]     = (byte)(value);
            buffer[off + 1] = (byte)(value >> 8);
            buffer[off + 2] = (byte)(value >> 16);
            buffer[off + 3] = (byte)(value >> 24);
            off += 4;
        }

        public static void Read(ref uint value, byte[] buffer, ref int off)
        {
            value =
                (uint)buffer[off] |
                (uint)buffer[off + 1] << 8 |
                (uint)buffer[off + 2] << 16 |
                (uint)buffer[off + 3] << 24;
            off += 4;
        }

        public static void Write(long value, byte[] buffer, ref int off)
        {
            buffer[off]     = (byte)(value);
            buffer[off + 1] = (byte)(value >> 8);
            buffer[off + 2] = (byte)(value >> 16);
            buffer[off + 3] = (byte)(value >> 24);
            buffer[off + 4] = (byte)(value >> 32);
            buffer[off + 5] = (byte)(value >> 40);
            buffer[off + 6] = (byte)(value >> 48);
            buffer[off + 7] = (byte)(value >> 56);
            off += 8;
        }

        public static void Read(ref long value, byte[] buffer, ref int off)
        {
            value =
                (long)buffer[off] |
                (long)buffer[off + 1] << 8 |
                (long)buffer[off + 2] << 16 |
                (long)buffer[off + 3] << 24 |
                (long)buffer[off + 4] << 32 |
                (long)buffer[off + 5] << 40 |
                (long)buffer[off + 6] << 48 |
                (long)buffer[off + 7] << 56;
            off += 8;
        }

        public static void Write(ulong value, byte[] buffer, ref int off)
        {
            buffer[off]     = (byte)(value);
            buffer[off + 1] = (byte)(value >> 8);
            buffer[off + 2] = (byte)(value >> 16);
            buffer[off + 3] = (byte)(value >> 24);
            buffer[off + 4] = (byte)(value >> 32);
            buffer[off + 5] = (byte)(value >> 40);
            buffer[off + 6] = (byte)(value >> 48);
            buffer[off + 7] = (byte)(value >> 56);
            off += 8;
        }

        public static void Read(ref ulong value, byte[] buffer, ref int off)
        {
            value =
                (ulong)buffer[off] |
                (ulong)buffer[off + 1] << 8 |
                (ulong)buffer[off + 2] << 16 |
                (ulong)buffer[off + 3] << 24 |
                (ulong)buffer[off + 4] << 32 |
                (ulong)buffer[off + 5] << 40 |
                (ulong)buffer[off + 6] << 48 |
                (ulong)buffer[off + 7] << 56;
            off += 8;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct FloatIntConv
        {
            [FieldOffset(0)]
            public float F;
            [FieldOffset(0)]
            public int I;
        }

        public static void Write(float value, byte[] buffer, ref int off)
        {
            Write(new FloatIntConv { F = value }.I, buffer, ref off);
        }

        public static void Read(ref float value, byte[] buffer, ref int off)
        {
            int iValue = 0;
            Read(ref iValue, buffer, ref off);
            value = new FloatIntConv { I = iValue }.F;
        }
    }
}
