using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Parameters
{
    public abstract class DsaKeyParameters
		: AsymmetricKeyParameter
    {
		private readonly DsaParameters parameters;

		protected DsaKeyParameters(
            bool			isPrivate,
            DsaParameters	parameters)
			: base(isPrivate)
        {
			// Note: parameters may be null
            this.parameters = parameters;
        }

		public DsaParameters Parameters
        {
            get { return parameters; }
        }

		public override bool Equals(
			object obj)
		{
			if (obj == this)
				return true;

			var other = obj as DsaKeyParameters;

			if (other == null)
				return false;

			return Equals(other);
		}

		protected bool Equals(
			DsaKeyParameters other)
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
