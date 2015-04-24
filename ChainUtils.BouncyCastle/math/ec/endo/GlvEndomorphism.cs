using System;

namespace ChainUtils.BouncyCastle.Math.EC.Endo
{
    public interface GlvEndomorphism
        :   ECEndomorphism
    {
        BigInteger[] DecomposeScalar(BigInteger k);
    }
}
