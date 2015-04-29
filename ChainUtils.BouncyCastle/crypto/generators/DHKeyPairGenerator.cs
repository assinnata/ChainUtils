using ChainUtils.BouncyCastle.Crypto.Parameters;

namespace ChainUtils.BouncyCastle.Crypto.Generators
{
    /**
     * a Diffie-Hellman key pair generator.
     *
     * This generates keys consistent for use in the MTI/A0 key agreement protocol
     * as described in "Handbook of Applied Cryptography", Pages 516-519.
     */
    public class DHKeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
    {
		private DHKeyGenerationParameters param;

		public virtual void Init(
			KeyGenerationParameters parameters)
        {
            param = (DHKeyGenerationParameters)parameters;
        }

		public virtual AsymmetricCipherKeyPair GenerateKeyPair()
        {
			var helper = DHKeyGeneratorHelper.Instance;
			var dhp = param.Parameters;

			var x = helper.CalculatePrivate(dhp, param.Random);
			var y = helper.CalculatePublic(dhp, x);

			return new AsymmetricCipherKeyPair(
                new DHPublicKeyParameters(y, dhp),
                new DHPrivateKeyParameters(x, dhp));
        }
    }
}
