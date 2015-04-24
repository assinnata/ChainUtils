using System;

namespace ChainUtils.BouncyCastle.Math.Field
{
    public interface IFiniteField
    {
        BigInteger Characteristic { get; }

        int Dimension { get; }
    }
}
