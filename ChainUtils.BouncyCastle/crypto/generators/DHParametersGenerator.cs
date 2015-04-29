using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Generators
{
    public class DHParametersGenerator
    {
        private int				size;
        private int				certainty;
        private SecureRandom	random;

        public virtual void Init(
            int				size,
            int				certainty,
            SecureRandom	random)
        {
            this.size = size;
            this.certainty = certainty;
            this.random = random;
        }

        /**
         * which Generates the p and g values from the given parameters,
         * returning the DHParameters object.
         * <p>
         * Note: can take a while...</p>
         */
        public virtual DHParameters GenerateParameters()
        {
			//
			// find a safe prime p where p = 2*q + 1, where p and q are prime.
			//
			var safePrimes = DHParametersHelper.GenerateSafePrimes(size, certainty, random);

			var p = safePrimes[0];
			var q = safePrimes[1];
			var g = DHParametersHelper.SelectGenerator(p, q, random);

			return new DHParameters(p, g, q, BigInteger.Two, null);
		}
	}
}
