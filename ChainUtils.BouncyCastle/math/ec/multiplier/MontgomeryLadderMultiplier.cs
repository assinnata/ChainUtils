namespace ChainUtils.BouncyCastle.Math.EC.Multiplier
{
    public class MontgomeryLadderMultiplier 
        : AbstractECMultiplier
    {
        /**
         * Montgomery ladder.
         */
        protected override ECPoint MultiplyPositive(ECPoint p, BigInteger k)
        {
            var R = new ECPoint[]{ p.Curve.Infinity, p };

            var n = k.BitLength;
            var i = n;
            while (--i >= 0)
            {
                var b = k.TestBit(i) ? 1 : 0;
                var bp = 1 - b;
                R[bp] = R[bp].Add(R[b]);
                R[b] = R[b].Twice();
            }
            return R[0];
        }
    }
}
