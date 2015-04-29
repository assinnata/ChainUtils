namespace ChainUtils.BouncyCastle.Crypto.Utilities
{
    internal sealed class Pack
    {
        private Pack()
        {
        }

        internal static void UInt16_To_BE(ushort n, byte[] bs)
        {
            bs[0] = (byte)(n >> 8);
            bs[1] = (byte)(n);
        }

        internal static void UInt16_To_BE(ushort n, byte[] bs, int off)
        {
            bs[off] = (byte)(n >> 8);
            bs[off + 1] = (byte)(n);
        }

        internal static ushort BE_To_UInt16(byte[] bs)
        {
            var n = (uint)bs[0] << 8
                | (uint)bs[1];
            return (ushort)n;
        }

        internal static ushort BE_To_UInt16(byte[] bs, int off)
        {
            var n = (uint)bs[off] << 8
                | (uint)bs[off + 1];
            return (ushort)n;
        }

        internal static byte[] UInt32_To_BE(uint n)
        {
            var bs = new byte[4];
            UInt32_To_BE(n, bs, 0);
            return bs;
        }

        internal static void UInt32_To_BE(uint n, byte[] bs)
        {
            bs[0] = (byte)(n >> 24);
            bs[1] = (byte)(n >> 16);
            bs[2] = (byte)(n >> 8);
            bs[3] = (byte)(n);
        }

        internal static void UInt32_To_BE(uint n, byte[] bs, int off)
        {
            bs[off] = (byte)(n >> 24);
            bs[off + 1] = (byte)(n >> 16);
            bs[off + 2] = (byte)(n >> 8);
            bs[off + 3] = (byte)(n);
        }

        internal static byte[] UInt32_To_BE(uint[] ns)
        {
            var bs = new byte[4 * ns.Length];
            UInt32_To_BE(ns, bs, 0);
            return bs;
        }

        internal static void UInt32_To_BE(uint[] ns, byte[] bs, int off)
        {
            for (var i = 0; i < ns.Length; ++i)
            {
                UInt32_To_BE(ns[i], bs, off);
                off += 4;
            }
        }

        internal static uint BE_To_UInt32(byte[] bs)
        {
            return (uint)bs[0] << 24
                | (uint)bs[1] << 16
                | (uint)bs[2] << 8
                | (uint)bs[3];
        }

        internal static uint BE_To_UInt32(byte[] bs, int off)
        {
            return (uint)bs[off] << 24
                | (uint)bs[off + 1] << 16
                | (uint)bs[off + 2] << 8
                | (uint)bs[off + 3];
        }

        internal static void BE_To_UInt32(byte[] bs, int off, uint[] ns)
        {
            for (var i = 0; i < ns.Length; ++i)
            {
                ns[i] = BE_To_UInt32(bs, off);
                off += 4;
            }
        }

        internal static byte[] UInt64_To_BE(ulong n)
        {
            var bs = new byte[8];
            UInt64_To_BE(n, bs, 0);
            return bs;
        }

        internal static void UInt64_To_BE(ulong n, byte[] bs)
        {
            UInt32_To_BE((uint)(n >> 32), bs);
            UInt32_To_BE((uint)(n), bs, 4);
        }

        internal static void UInt64_To_BE(ulong n, byte[] bs, int off)
        {
            UInt32_To_BE((uint)(n >> 32), bs, off);
            UInt32_To_BE((uint)(n), bs, off + 4);
        }

        internal static ulong BE_To_UInt64(byte[] bs)
        {
            var hi = BE_To_UInt32(bs);
            var lo = BE_To_UInt32(bs, 4);
            return ((ulong)hi << 32) | (ulong)lo;
        }

        internal static ulong BE_To_UInt64(byte[] bs, int off)
        {
            var hi = BE_To_UInt32(bs, off);
            var lo = BE_To_UInt32(bs, off + 4);
            return ((ulong)hi << 32) | (ulong)lo;
        }

        internal static void UInt16_To_LE(ushort n, byte[] bs)
        {
            bs[0] = (byte)(n);
            bs[1] = (byte)(n >> 8);
        }

        internal static void UInt16_To_LE(ushort n, byte[] bs, int off)
        {
            bs[off] = (byte)(n);
            bs[off + 1] = (byte)(n >> 8);
        }

        internal static ushort LE_To_UInt16(byte[] bs)
        {
            var n = (uint)bs[0]
                | (uint)bs[1] << 8;
            return (ushort)n;
        }

        internal static ushort LE_To_UInt16(byte[] bs, int off)
        {
            var n = (uint)bs[off]
                | (uint)bs[off + 1] << 8;
            return (ushort)n;
        }

        internal static byte[] UInt32_To_LE(uint n)
        {
            var bs = new byte[4];
            UInt32_To_LE(n, bs, 0);
            return bs;
        }

        internal static void UInt32_To_LE(uint n, byte[] bs)
        {
            bs[0] = (byte)(n);
            bs[1] = (byte)(n >> 8);
            bs[2] = (byte)(n >> 16);
            bs[3] = (byte)(n >> 24);
        }

        internal static void UInt32_To_LE(uint n, byte[] bs, int off)
        {
            bs[off] = (byte)(n);
            bs[off + 1] = (byte)(n >> 8);
            bs[off + 2] = (byte)(n >> 16);
            bs[off + 3] = (byte)(n >> 24);
        }

        internal static byte[] UInt32_To_LE(uint[] ns)
        {
            var bs = new byte[4 * ns.Length];
            UInt32_To_LE(ns, bs, 0);
            return bs;
        }

        internal static void UInt32_To_LE(uint[] ns, byte[] bs, int off)
        {
            for (var i = 0; i < ns.Length; ++i)
            {
                UInt32_To_LE(ns[i], bs, off);
                off += 4;
            }
        }

        internal static uint LE_To_UInt32(byte[] bs)
        {
            return (uint)bs[0]
                | (uint)bs[1] << 8
                | (uint)bs[2] << 16
                | (uint)bs[3] << 24;
        }

        internal static uint LE_To_UInt32(byte[] bs, int off)
        {
            return (uint)bs[off]
                | (uint)bs[off + 1] << 8
                | (uint)bs[off + 2] << 16
                | (uint)bs[off + 3] << 24;
        }

        internal static void LE_To_UInt32(byte[] bs, int off, uint[] ns)
        {
            for (var i = 0; i < ns.Length; ++i)
            {
                ns[i] = LE_To_UInt32(bs, off);
                off += 4;
            }
        }

        internal static void LE_To_UInt32(byte[] bs, int bOff, uint[] ns, int nOff, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                ns[nOff + i] = LE_To_UInt32(bs, bOff);
                bOff += 4;
            }
        }

        internal static byte[] UInt64_To_LE(ulong n)
        {
            var bs = new byte[8];
            UInt64_To_LE(n, bs, 0);
            return bs;
        }

        internal static void UInt64_To_LE(ulong n, byte[] bs)
        {
            UInt32_To_LE((uint)(n), bs);
            UInt32_To_LE((uint)(n >> 32), bs, 4);
        }

        internal static void UInt64_To_LE(ulong n, byte[] bs, int off)
        {
            UInt32_To_LE((uint)(n), bs, off);
            UInt32_To_LE((uint)(n >> 32), bs, off + 4);
        }

        internal static ulong LE_To_UInt64(byte[] bs)
        {
            var lo = LE_To_UInt32(bs);
            var hi = LE_To_UInt32(bs, 4);
            return ((ulong)hi << 32) | (ulong)lo;
        }

        internal static ulong LE_To_UInt64(byte[] bs, int off)
        {
            var lo = LE_To_UInt32(bs, off);
            var hi = LE_To_UInt32(bs, off + 4);
            return ((ulong)hi << 32) | (ulong)lo;
        }
    }
}
