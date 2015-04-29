namespace ChainUtils.BouncyCastle.Math.EC.Multiplier
{
    /**
     * Class implementing the NAF (Non-Adjacent Form) multiplication algorithm (left-to-right).
     */
    public class NafL2RMultiplier
        : AbstractECMultiplier
    {
        protected override ECPoint MultiplyPositive(ECPoint p, BigInteger k)
        {
            var naf = WNafUtilities.GenerateCompactNaf(k);

            ECPoint addP = p.Normalize(), subP = addP.Negate();

            var R = p.Curve.Infinity;

            var i = naf.Length;
            while (--i >= 0)
            {
                var ni = naf[i];
                int digit = ni >> 16, zeroes = ni & 0xFFFF;

                R = R.TwicePlus(digit < 0 ? subP : addP);
                R = R.TimesPow2(zeroes);
            }

            return R;
        }
    }
}
