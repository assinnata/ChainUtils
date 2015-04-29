using ChainUtils.BouncyCastle.Crypto.Parameters;

namespace ChainUtils.BouncyCastle.Crypto.Generators
{
    /**
     * a ElGamal key pair generator.
     * <p>
     * This Generates keys consistent for use with ElGamal as described in
     * page 164 of "Handbook of Applied Cryptography".</p>
     */
    public class ElGamalKeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
    {
        private ElGamalKeyGenerationParameters param;

        public void Init(
			KeyGenerationParameters parameters)
        {
            param = (ElGamalKeyGenerationParameters) parameters;
        }

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
			var helper = DHKeyGeneratorHelper.Instance;
			var egp = param.Parameters;
			var dhp = new DHParameters(egp.P, egp.G, null, 0, egp.L);

			var x = helper.CalculatePrivate(dhp, param.Random);
			var y = helper.CalculatePublic(dhp, x);

			return new AsymmetricCipherKeyPair(
                new ElGamalPublicKeyParameters(y, egp),
                new ElGamalPrivateKeyParameters(x, egp));
        }
    }

}
