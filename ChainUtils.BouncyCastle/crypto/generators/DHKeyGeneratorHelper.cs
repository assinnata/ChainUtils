using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Math.EC.Multiplier;
using ChainUtils.BouncyCastle.Security;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Generators
{
    class DHKeyGeneratorHelper
    {
        internal static readonly DHKeyGeneratorHelper Instance = new DHKeyGeneratorHelper();

        private DHKeyGeneratorHelper()
        {
        }

        internal BigInteger CalculatePrivate(
            DHParameters	dhParams,
            SecureRandom	random)
        {
            var limit = dhParams.L;

            if (limit != 0)
            {
                var minWeight = limit >> 2;
                for (;;)
                {
                    var x = new BigInteger(limit, random).SetBit(limit - 1);
                    if (WNafUtilities.GetNafWeight(x) >= minWeight)
                    {
                        return x;
                    }
                }
            }

            var min = BigInteger.Two;
            var m = dhParams.M;
            if (m != 0)
            {
                min = BigInteger.One.ShiftLeft(m - 1);
            }

            var q = dhParams.Q;
            if (q == null)
            {
                q = dhParams.P;
            }
            var max = q.Subtract(BigInteger.Two);

            {
                var minWeight = max.BitLength >> 2;
                for (;;)
                {
                    var x = BigIntegers.CreateRandomInRange(min, max, random);
                    if (WNafUtilities.GetNafWeight(x) >= minWeight)
                    {
                        return x;
                    }
                }
            }
        }

        internal BigInteger CalculatePublic(
            DHParameters	dhParams,
            BigInteger		x)
        {
            return dhParams.G.ModPow(x, dhParams.P);
        }
    }
}
