using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.X509;

namespace ChainUtils.BouncyCastle.Asn1.Pkcs
{
	public class KeyDerivationFunc
		: AlgorithmIdentifier
	{
		internal KeyDerivationFunc(Asn1Sequence seq)
			: base(seq)
		{
		}

		public KeyDerivationFunc(
			DerObjectIdentifier	id,
			Asn1Encodable		parameters)
			: base(id, parameters)
		{
		}
	}
}