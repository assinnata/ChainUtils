using System;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Modes.Gcm
{
	public class BasicGcmExponentiator
		: IGcmExponentiator
	{
		private byte[] x;

		public void Init(byte[] x)
		{
			this.x = Arrays.Clone(x);
		}

		public void ExponentiateX(long pow, byte[] output)
		{
			// Initial value is little-endian 1
			var y = GcmUtilities.OneAsBytes();

			if (pow > 0)
			{
				var powX = Arrays.Clone(x);
				do
				{
					if ((pow & 1L) != 0)
					{
						GcmUtilities.Multiply(y, powX);
					}
					GcmUtilities.Multiply(powX, powX);
					pow >>= 1;
				}
				while (pow > 0);
			}

			Array.Copy(y, 0, output, 0, 16);
		}
	}
}
