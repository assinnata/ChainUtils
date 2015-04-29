using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Asn1.Pkcs;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Parameters
{
    public class DHKeyParameters
		: AsymmetricKeyParameter
    {
        private readonly DHParameters parameters;
		private readonly DerObjectIdentifier algorithmOid;

		protected DHKeyParameters(
            bool			isPrivate,
            DHParameters	parameters)
			: this(isPrivate, parameters, PkcsObjectIdentifiers.DhKeyAgreement)
        {
        }

		protected DHKeyParameters(
            bool				isPrivate,
            DHParameters		parameters,
			DerObjectIdentifier	algorithmOid)
			: base(isPrivate)
        {
			// TODO Should we allow parameters to be null?
            this.parameters = parameters;
			this.algorithmOid = algorithmOid;
        }

		public DHParameters Parameters
        {
            get { return parameters; }
        }

		public DerObjectIdentifier AlgorithmOid
		{
			get { return algorithmOid; }
		}

		public override bool Equals(
			object obj)
        {
			if (obj == this)
				return true;

			var other = obj as DHKeyParameters;

			if (other == null)
				return false;

			return Equals(other);
        }

		protected bool Equals(
			DHKeyParameters other)
		{
			return Equals(parameters, other.parameters)
				&& base.Equals(other);
		}

		public override int GetHashCode()
        {
			var hc = base.GetHashCode();

			if (parameters != null)
			{
				hc ^= parameters.GetHashCode();
			}

			return hc;
        }
    }
}
