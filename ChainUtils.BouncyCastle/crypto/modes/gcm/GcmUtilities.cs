using System;
using ChainUtils.BouncyCastle.Crypto.Utilities;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Modes.Gcm
{
    internal abstract class GcmUtilities
    {
        internal static byte[] OneAsBytes()
        {
            var tmp = new byte[16];
            tmp[0] = 0x80;
            return tmp;
        }

        internal static uint[] OneAsUints()
        {
            var tmp = new uint[4];
            tmp[0] = 0x80000000;
            return tmp;
        }

        internal static uint[] AsUints(byte[] bs)
        {
            var output = new uint[4];
            Pack.BE_To_UInt32(bs, 0, output);
            return output;
        }

        internal static void AsUints(byte[] bs, uint[] output)
        {
            Pack.BE_To_UInt32(bs, 0, output);
        }

        internal static void Multiply(byte[] block, byte[] val)
        {
            var tmp = Arrays.Clone(block);
            var c = new byte[16];

            for (var i = 0; i < 16; ++i)
            {
                var bits = val[i];
                for (var j = 7; j >= 0; --j)
                {
                    if ((bits & (1 << j)) != 0)
                    {
                        Xor(c, tmp);
                    }

                    var lsb = (tmp[15] & 1) != 0;
                    ShiftRight(tmp);
                    if (lsb)
                    {
                        // R = new byte[]{ 0xe1, ... };
                        //GCMUtilities.Xor(tmp, R);
                        tmp[0] ^= (byte)0xe1;
                    }
                }
            }

            Array.Copy(c, 0, block, 0, 16);
        }

        // P is the value with only bit i=1 set
        internal static void MultiplyP(uint[] x)
        {
            var lsb = (x[3] & 1) != 0;
            ShiftRight(x);
            if (lsb)
            {
                // R = new uint[]{ 0xe1000000, 0, 0, 0 };
                //Xor(v, R);
                x[0] ^= 0xe1000000;
            }
        }

        internal static void MultiplyP(uint[] x, uint[] output)
        {
            var lsb = (x[3] & 1) != 0;
            ShiftRight(x, output);
            if (lsb)
            {
                output[0] ^= 0xe1000000;
            }
        }

        internal static void MultiplyP8(uint[] x)
        {
//			for (int i = 8; i != 0; --i)
//			{
//				MultiplyP(x);
//			}

            var lsw = x[3];
            ShiftRightN(x, 8);
            for (var i = 7; i >= 0; --i)
            {
                if ((lsw & (1 << i)) != 0)
                {
                    x[0] ^= (0xe1000000 >> (7 - i));
                }
            }
        }

        internal static void MultiplyP8(uint[] x, uint[] output)
        {
            var lsw = x[3];
            ShiftRightN(x, 8, output);
            for (var i = 7; i >= 0; --i)
            {
                if ((lsw & (1 << i)) != 0)
                {
                    output[0] ^= (0xe1000000 >> (7 - i));
                }
            }
        }

        internal static void ShiftRight(byte[] block)
        {
            var i = 0;
            byte bit = 0;
            for (; ; )
            {
                var b = block[i];
                block[i] = (byte)((b >> 1) | bit);
                if (++i == 16) break;
                bit = (byte)(b << 7);
            }
        }

        static void ShiftRight(byte[] block, byte[] output)
        {
            var i = 0;
            byte bit = 0;
            for (;;)
            {
                var b = block[i];
                output[i] = (byte)((b >> 1) | bit);
                if (++i == 16) break;
                bit = (byte)(b << 7);
            }
        }

        internal static void ShiftRight(uint[] block)
        {
            var i = 0;
            uint bit = 0;
            for (; ; )
            {
                var b = block[i];
                block[i] = (b >> 1) | bit;
                if (++i == 4) break;
                bit = b << 31;
            }
        }

        internal static void ShiftRight(uint[] block, uint[] output)
        {
            var i = 0;
            uint bit = 0;
            for (; ; )
            {
                var b = block[i];
                output[i] = (b >> 1) | bit;
                if (++i == 4) break;
                bit = b << 31;
            }
        }

        internal static void ShiftRightN(uint[] block, int n)
        {
            var i = 0;
            uint bit = 0;
            for (; ; )
            {
                var b = block[i];
                block[i] = (b >> n) | bit;
                if (++i == 4) break;
                bit = b << (32 - n);
            }
        }

        internal static void ShiftRightN(uint[] block, int n, uint[] output)
        {
            var i = 0;
            uint bit = 0;
            for (; ; )
            {
                var b = block[i];
                output[i] = (b >> n) | bit;
                if (++i == 4) break;
                bit = b << (32 - n);
            }
        }

        internal static void Xor(byte[] block, byte[] val)
        {
            for (var i = 15; i >= 0; --i)
            {
                block[i] ^= val[i];
            }
        }

        internal static void Xor(byte[] block, byte[] val, int off, int len)
        {
            while (--len >= 0)
            {
                block[len] ^= val[off + len];
            }
        }

        internal static void Xor(byte[] block, byte[] val, byte[] output)
        {
            for (var i = 15; i >= 0; --i)
            {
                output[i] = (byte)(block[i] ^ val[i]);
            }
        }

        internal static void Xor(uint[] block, uint[] val)
        {
            for (var i = 3; i >= 0; --i)
            {
                block[i] ^= val[i];
            }
        }

        internal static void Xor(uint[] block, uint[] val, uint[] output)
        {
            for (var i = 3; i >= 0; --i)
            {
                output[i] = block[i] ^ val[i];
            }
        }
    }
}
