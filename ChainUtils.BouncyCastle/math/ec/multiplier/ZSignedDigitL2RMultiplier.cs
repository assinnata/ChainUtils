namespace ChainUtils.BouncyCastle.Math.EC.Multiplier
{
    public class ZSignedDigitL2RMultiplier 
        : AbstractECMultiplier
    {
        /**
         * 'Zeroless' Signed Digit Left-to-Right.
         */
        protected override ECPoint MultiplyPositive(ECPoint p, BigInteger k)
        {
            ECPoint addP = p.Normalize(), subP = addP.Negate();

            var R0 = addP;

            var n = k.BitLength;
            var s = k.GetLowestSetBit();

            var i = n;
            while (--i > s)
            {
                R0 = R0.TwicePlus(k.TestBit(i) ? addP : subP);
            }

            R0 = R0.TimesPow2(s);

            return R0;
        }
    }
}
