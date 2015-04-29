using System;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Utilities;

namespace ChainUtils.BouncyCastle.Crypto.Engines
{
	/// <remarks>
	/// An implementation of the AES Key Wrapper from the NIST Key Wrap
	/// Specification as described in RFC 3394.
	/// <p/>
	/// For further details see: <a href="http://www.ietf.org/rfc/rfc3394.txt">http://www.ietf.org/rfc/rfc3394.txt</a>
	/// and  <a href="http://csrc.nist.gov/encryption/kms/key-wrap.pdf">http://csrc.nist.gov/encryption/kms/key-wrap.pdf</a>.
	/// </remarks>
	public class Rfc3394WrapEngine
		: IWrapper
	{
		private readonly IBlockCipher engine;

		private KeyParameter	param;
		private bool			forWrapping;

		private byte[] iv =
		{
			0xa6, 0xa6, 0xa6, 0xa6,
			0xa6, 0xa6, 0xa6, 0xa6
		};

		public Rfc3394WrapEngine(
			IBlockCipher engine)
		{
			this.engine = engine;
		}

		public void Init(
			bool				forWrapping,
			ICipherParameters	parameters)
		{
			this.forWrapping = forWrapping;

			if (parameters is ParametersWithRandom)
			{
				parameters = ((ParametersWithRandom) parameters).Parameters;
			}

			if (parameters is KeyParameter)
			{
				param = (KeyParameter) parameters;
			}
			else if (parameters is ParametersWithIV)
			{
				var pIV = (ParametersWithIV) parameters;
				var iv = pIV.GetIV();

				if (iv.Length != 8)
					throw new ArgumentException("IV length not equal to 8", "parameters");

				this.iv = iv;
				param = (KeyParameter) pIV.Parameters;
			}
			else
			{
				// TODO Throw an exception for bad parameters?
			}
		}

		public string AlgorithmName
		{
			get { return engine.AlgorithmName; }
		}

		public byte[] Wrap(
			byte[]	input,
			int		inOff,
			int		inLen)
		{
			if (!forWrapping)
			{
				throw new InvalidOperationException("not set for wrapping");
			}

			var n = inLen / 8;

			if ((n * 8) != inLen)
			{
				throw new DataLengthException("wrap data must be a multiple of 8 bytes");
			}

			var block = new byte[inLen + iv.Length];
			var buf = new byte[8 + iv.Length];

			Array.Copy(iv, 0, block, 0, iv.Length);
			Array.Copy(input, inOff, block, iv.Length, inLen);

			engine.Init(true, param);

			for (var j = 0; j != 6; j++)
			{
				for (var i = 1; i <= n; i++)
				{
					Array.Copy(block, 0, buf, 0, iv.Length);
					Array.Copy(block, 8 * i, buf, iv.Length, 8);
					engine.ProcessBlock(buf, 0, buf, 0);

					var t = n * j + i;
					for (var k = 1; t != 0; k++)
					{
						var v = (byte)t;

						buf[iv.Length - k] ^= v;
						t = (int) ((uint)t >> 8);
					}

					Array.Copy(buf, 0, block, 0, 8);
					Array.Copy(buf, 8, block, 8 * i, 8);
				}
			}

			return block;
		}

		public byte[] Unwrap(
			byte[]  input,
			int     inOff,
			int     inLen)
		{
			if (forWrapping)
			{
				throw new InvalidOperationException("not set for unwrapping");
			}

			var n = inLen / 8;

			if ((n * 8) != inLen)
			{
				throw new InvalidCipherTextException("unwrap data must be a multiple of 8 bytes");
			}

			var  block = new byte[inLen - iv.Length];
			var  a = new byte[iv.Length];
			var  buf = new byte[8 + iv.Length];

			Array.Copy(input, inOff, a, 0, iv.Length);
            Array.Copy(input, inOff + iv.Length, block, 0, inLen - iv.Length);

			engine.Init(false, param);

			n = n - 1;

			for (var j = 5; j >= 0; j--)
			{
				for (var i = n; i >= 1; i--)
				{
					Array.Copy(a, 0, buf, 0, iv.Length);
					Array.Copy(block, 8 * (i - 1), buf, iv.Length, 8);

					var t = n * j + i;
					for (var k = 1; t != 0; k++)
					{
						var v = (byte)t;

						buf[iv.Length - k] ^= v;
						t = (int) ((uint)t >> 8);
					}

					engine.ProcessBlock(buf, 0, buf, 0);
					Array.Copy(buf, 0, a, 0, 8);
					Array.Copy(buf, 8, block, 8 * (i - 1), 8);
				}
			}

			if (!Arrays.ConstantTimeAreEqual(a, iv))
				throw new InvalidCipherTextException("checksum failed");

			return block;
		}
	}
}
