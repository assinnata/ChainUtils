using System;
using ChainUtils.BouncyCastle.Crypto.Modes;
using ChainUtils.BouncyCastle.Crypto.Parameters;
using ChainUtils.BouncyCastle.Security;

namespace ChainUtils.BouncyCastle.Crypto.Engines
{
	/**
	 * an implementation of the RFC 3211 Key Wrap
	 * Specification.
	 */
	public class Rfc3211WrapEngine
		: IWrapper
	{
		private CbcBlockCipher		engine;
		private ParametersWithIV	param;
		private bool				forWrapping;
		private SecureRandom		rand;

		public Rfc3211WrapEngine(
			IBlockCipher engine)
		{
			this.engine = new CbcBlockCipher(engine);
		}

		public void Init(
			bool				forWrapping,
			ICipherParameters	param)
		{
			this.forWrapping = forWrapping;

			if (param is ParametersWithRandom)
			{
				var p = (ParametersWithRandom) param;

				rand = p.Random;
				this.param = (ParametersWithIV) p.Parameters;
			}
			else
			{
				if (forWrapping)
				{
					rand = new SecureRandom();
				}

				this.param = (ParametersWithIV) param;
			}
		}

		public string AlgorithmName
		{
			get { return engine.GetUnderlyingCipher().AlgorithmName + "/RFC3211Wrap"; }
		}

		public byte[] Wrap(
			byte[]	inBytes,
			int		inOff,
			int		inLen)
		{
			if (!forWrapping)
			{
				throw new InvalidOperationException("not set for wrapping");
			}

			engine.Init(true, param);

			var blockSize = engine.GetBlockSize();
			byte[] cekBlock;

			if (inLen + 4 < blockSize * 2)
			{
				cekBlock = new byte[blockSize * 2];
			}
			else
			{
				cekBlock = new byte[(inLen + 4) % blockSize == 0 ? inLen + 4 : ((inLen + 4) / blockSize + 1) * blockSize];
			}

			cekBlock[0] = (byte)inLen;
			cekBlock[1] = (byte)~inBytes[inOff];
			cekBlock[2] = (byte)~inBytes[inOff + 1];
			cekBlock[3] = (byte)~inBytes[inOff + 2];

			Array.Copy(inBytes, inOff, cekBlock, 4, inLen);

			rand.NextBytes(cekBlock, inLen + 4, cekBlock.Length - inLen - 4);

			for (var i = 0; i < cekBlock.Length; i += blockSize)
			{
				engine.ProcessBlock(cekBlock, i, cekBlock, i);
			}

			for (var i = 0; i < cekBlock.Length; i += blockSize)
			{
				engine.ProcessBlock(cekBlock, i, cekBlock, i);
			}

			return cekBlock;
		}

		public byte[] Unwrap(
			byte[]	inBytes,
			int		inOff,
			int		inLen)
		{
			if (forWrapping)
			{
				throw new InvalidOperationException("not set for unwrapping");
			}

			var blockSize = engine.GetBlockSize();

			if (inLen < 2 * blockSize)
			{
				throw new InvalidCipherTextException("input too short");
			}

			var cekBlock = new byte[inLen];
			var iv = new byte[blockSize];

			Array.Copy(inBytes, inOff, cekBlock, 0, inLen);
			Array.Copy(inBytes, inOff, iv, 0, iv.Length);

			engine.Init(false, new ParametersWithIV(param.Parameters, iv));

			for (var i = blockSize; i < cekBlock.Length; i += blockSize)
			{
				engine.ProcessBlock(cekBlock, i, cekBlock, i);    
			}

			Array.Copy(cekBlock, cekBlock.Length - iv.Length, iv, 0, iv.Length);

			engine.Init(false, new ParametersWithIV(param.Parameters, iv));

			engine.ProcessBlock(cekBlock, 0, cekBlock, 0);

			engine.Init(false, param);

			for (var i = 0; i < cekBlock.Length; i += blockSize)
			{
				engine.ProcessBlock(cekBlock, i, cekBlock, i);
			}

			if ((cekBlock[0] & 0xff) > cekBlock.Length - 4)
			{
				throw new InvalidCipherTextException("wrapped key corrupted");
			}

			var key = new byte[cekBlock[0] & 0xff];

			Array.Copy(cekBlock, 4, key, 0, cekBlock[0]);

			// Note: Using constant time comparison
			var nonEqual = 0;
			for (var i = 0; i != 3; i++)
			{
				var check = (byte)~cekBlock[1 + i];
				nonEqual |= (check ^ key[i]);
			}

			if (nonEqual != 0)
				throw new InvalidCipherTextException("wrapped key fails checksum");

			return key;
		}
	}
}
