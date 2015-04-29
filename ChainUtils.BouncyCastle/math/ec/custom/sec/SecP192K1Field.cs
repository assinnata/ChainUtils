using System.Diagnostics;

namespace ChainUtils.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecP192K1Field
    {
        // 2^192 - 2^32 - 2^12 - 2^8 - 2^7 - 2^6 - 2^3 - 1
        internal static readonly uint[] P = new uint[]{ 0xFFFFEE37, 0xFFFFFFFE, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        internal static readonly uint[] PExt = new uint[]{ 0x013C4FD1, 0x00002392, 0x00000001, 0x00000000, 0x00000000,
            0x00000000, 0xFFFFDC6E, 0xFFFFFFFD, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        private static readonly uint[] PExtInv = new uint[]{ 0xFEC3B02F, 0xFFFFDC6D, 0xFFFFFFFE, 0xFFFFFFFF, 0xFFFFFFFF,
            0xFFFFFFFF, 0x00002391, 0x00000002 };
        private const uint P5 = 0xFFFFFFFF;
        private const uint PExt11 = 0xFFFFFFFF;
        private const uint PInv33 = 0x11C9;

        public static void Add(uint[] x, uint[] y, uint[] z)
        {
            var c = Nat192.Add(x, y, z);
            if (c != 0 || (z[5] == P5 && Nat192.Gte(z, P)))
            {
                Nat.Add33To(6, PInv33, z);
            }
        }

        public static void AddExt(uint[] xx, uint[] yy, uint[] zz)
        {
            var c = Nat.Add(12, xx, yy, zz);
            if (c != 0 || (zz[11] == PExt11 && Nat.Gte(12, zz, PExt)))
            {
                if (Nat.AddTo(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.IncAt(12, zz, PExtInv.Length);
                }
            }
        }

        public static void AddOne(uint[] x, uint[] z)
        {
            var c = Nat.Inc(6, x, z);
            if (c != 0 || (z[5] == P5 && Nat192.Gte(z, P)))
            {
                Nat.Add33To(6, PInv33, z);
            }
        }

        public static uint[] FromBigInteger(BigInteger x)
        {
            var z = Nat192.FromBigInteger(x);
            if (z[5] == P5 && Nat192.Gte(z, P))
            {
                Nat192.SubFrom(P, z);
            }
            return z;
        }

        public static void Half(uint[] x, uint[] z)
        {
            if ((x[0] & 1) == 0)
            {
                Nat.ShiftDownBit(6, x, 0, z);
            }
            else
            {
                var c = Nat192.Add(x, P, z);
                Nat.ShiftDownBit(6, z, c);
            }
        }

        public static void Multiply(uint[] x, uint[] y, uint[] z)
        {
            var tt = Nat192.CreateExt();
            Nat192.Mul(x, y, tt);
            Reduce(tt, z);
        }

        public static void MultiplyAddToExt(uint[] x, uint[] y, uint[] zz)
        {
            var c = Nat192.MulAddTo(x, y, zz);
            if (c != 0 || (zz[11] == PExt11 && Nat.Gte(12, zz, PExt)))
            {
                if (Nat.AddTo(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.IncAt(12, zz, PExtInv.Length);
                }
            }
        }

        public static void Negate(uint[] x, uint[] z)
        {
            if (Nat192.IsZero(x))
            {
                Nat192.Zero(z);
            }
            else
            {
                Nat192.Sub(P, x, z);
            }
        }

        public static void Reduce(uint[] xx, uint[] z)
        {
            var cc = Nat192.Mul33Add(PInv33, xx, 6, xx, 0, z, 0);
            var c = Nat192.Mul33DWordAdd(PInv33, cc, z, 0);

            Debug.Assert(c == 0 || c == 1);

            if (c != 0 || (z[5] == P5 && Nat192.Gte(z, P)))
            {
                Nat.Add33To(6, PInv33, z);
            }
        }

        public static void Reduce32(uint x, uint[] z)
        {
            if ((x != 0 && Nat192.Mul33WordAdd(PInv33, x, z, 0) != 0)
                || (z[5] == P5 && Nat192.Gte(z, P)))
            {
                Nat.Add33To(6, PInv33, z);
            }
        }

        public static void Square(uint[] x, uint[] z)
        {
            var tt = Nat192.CreateExt();
            Nat192.Square(x, tt);
            Reduce(tt, z);
        }

        public static void SquareN(uint[] x, int n, uint[] z)
        {
            Debug.Assert(n > 0);

            var tt = Nat192.CreateExt();
            Nat192.Square(x, tt);
            Reduce(tt, z);

            while (--n > 0)
            {
                Nat192.Square(z, tt);
                Reduce(tt, z);
            }
        }

        public static void Subtract(uint[] x, uint[] y, uint[] z)
        {
            var c = Nat192.Sub(x, y, z);
            if (c != 0)
            {
                Nat.Sub33From(6, PInv33, z);
            }
        }

        public static void SubtractExt(uint[] xx, uint[] yy, uint[] zz)
        {
            var c = Nat.Sub(12, xx, yy, zz);
            if (c != 0)
            {
                if (Nat.SubFrom(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.DecAt(12, zz, PExtInv.Length);
                }
            }
        }

        public static void Twice(uint[] x, uint[] z)
        {
            var c = Nat.ShiftUpBit(6, x, 0, z);
            if (c != 0 || (z[5] == P5 && Nat192.Gte(z, P)))
            {
                Nat.Add33To(6, PInv33, z);
            }
        }
    }
}
