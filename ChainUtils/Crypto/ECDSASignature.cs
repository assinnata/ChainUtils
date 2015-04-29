using System;
using System.IO;
using ChainUtils.BouncyCastle.Asn1;
using ChainUtils.BouncyCastle.Math;

namespace ChainUtils.Crypto
{
	public class EcdsaSignature
	{
		private readonly BigInteger _r;
		public BigInteger R
		{
			get
			{
				return _r;
			}
		}
		private BigInteger _s;
		public BigInteger S
		{
			get
			{
				return _s;
			}
		}
		public EcdsaSignature(BigInteger r, BigInteger s)
		{
			_r = r;
			_s = s;
		}

		public EcdsaSignature(BigInteger[] rs)
		{
			_r = rs[0];
			_s = rs[1];
		}

		/**
		* What we get back from the signer are the two components of a signature, r and s. To get a flat byte stream
		* of the type used by Bitcoin we have to encode them using DER encoding, which is just a way to pack the two
		* components into a structure.
		*/
		public byte[] ToDer()
		{
			// Usually 70-72 bytes.
			var bos = new MemoryStream(72);
			var seq = new DerSequenceGenerator(bos);
			seq.AddObject(new DerInteger(R));
			seq.AddObject(new DerInteger(S));
			seq.Close();
			return bos.ToArray();

		}
		const string InvalidDerSignature = "Invalid DER signature";
		public static EcdsaSignature FromDer(byte[] sig)
		{
			try
			{
				var decoder = new Asn1InputStream(sig);
				var seq = decoder.ReadObject() as DerSequence;
				if(seq == null || seq.Count != 2)
					throw new FormatException(InvalidDerSignature);
				return new EcdsaSignature(((DerInteger)seq[0]).Value, ((DerInteger)seq[1]).Value);
			}
			catch(IOException ex)
			{
				throw new FormatException(InvalidDerSignature, ex);
			}
		}

		public EcdsaSignature MakeCanonical()
		{
			if(S.CompareTo(EcKey.HalfCurveOrder) > 0)
			{
				return new EcdsaSignature(R, EcKey.CreateCurve().N.Subtract(S));
			}
			else
				return this;
		}



		public static bool IsValidDer(byte[] bytes)
		{
			try
			{
				FromDer(bytes);
				return true;
			}
			catch(FormatException)
			{
				return false;
			}
			catch(Exception ex)
			{
				Utils.error("Unexpected exception in ECDSASignature.IsValidDER " + ex.Message);
				return false;
			}
		}
	}
}
