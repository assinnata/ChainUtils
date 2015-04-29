using System;
using System.Linq;
using System.Numerics;
using ChainUtils.Crypto;

namespace ChainUtils.DataEncoders
{
	public class Base58Encoder : DataEncoder
	{
		public override string EncodeData(byte[] data, int length)
		{
			if(Check)
			{
				var calculatedHash = Hashes.Hash256(data, length).ToBytes().Take(4).ToArray();
				var toEncode = data.Take(length).Concat(calculatedHash).ToArray();
				return EncodeDataCore(toEncode, toEncode.Length);
			}
			else
				return EncodeDataCore(data, length);
		}

		private static string EncodeDataCore(byte[] data, int length)
		{
			BigInteger bn58 = 58;
			BigInteger bn0 = 0;

			// Convert big endian data to little endian
			// Extra zero at the end make sure bignum will interpret as a positive number
			var vchTmp = data.Take(length).Reverse().Concat(new byte[] { 0x00 }).ToArray();

			// Convert little endian data to bignum
			var bn = new BigInteger(vchTmp);

			// Convert bignum to std::string
			var str = "";
			// Expected size increase from base58 conversion is approximately 137%
			// use 138% to be safe

			var dv = BigInteger.Zero;
			var rem = BigInteger.Zero;
			while(bn > bn0)
			{
				dv = BigInteger.DivRem(bn, bn58, out rem);
				bn = dv;
				var c = (int)rem;
				str += PszBase58[c];
			}

			// Leading zeroes encoded as base58 zeros
			for(var i = 0 ; i < length && data[i] == 0 ; i++)
				str += PszBase58[0];

			// Convert little endian std::string to big endian
			str = new String(str.ToCharArray().Reverse().ToArray());
			return str;
		}


		const string PszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";


		public override byte[] DecodeData(string encoded)
		{
			if(Check)
			{
				var vchRet = DecodeDataCore(encoded);
				if(vchRet.Length < 4)
				{
					Array.Clear(vchRet, 0, vchRet.Length);
					throw new FormatException("Invalid checked base 58 string");
				}
				var calculatedHash = Hashes.Hash256(vchRet, vchRet.Length - 4).ToBytes().Take(4).ToArray();
				var expectedHash = vchRet.Skip(vchRet.Length - 4).Take(4).ToArray();

				if(!Utils.ArrayEqual(calculatedHash, expectedHash))
				{
					Array.Clear(vchRet, 0, vchRet.Length);
					throw new FormatException("Invalid hash of the base 58 string");
				}
				vchRet = vchRet.Take(vchRet.Length - 4).ToArray();
				return vchRet;
			}
			else
				return DecodeDataCore(encoded);
		}

		private static byte[] DecodeDataCore(string encoded)
		{
			var result = new byte[0];
			if(encoded.Length == 0)
				return result;
			BigInteger bn58 = 58;
			BigInteger bn = 0;
			BigInteger bnChar;
			var i = 0;
			while(IsSpace(encoded[i]))
			{
				i++;
				if(i >= encoded.Length)
					return result;
			}

			for(var y = i ; y < encoded.Length ; y++)
			{
				var p1 = PszBase58.IndexOf(encoded[y]);
				if(p1 == -1)
				{
					while(IsSpace(encoded[y]))
					{
						y++;
						if(y >= encoded.Length)
							break;
					}
					if(y != encoded.Length)
						throw new FormatException("Invalid base 58 string");
					break;
				}
				bnChar = new BigInteger(p1);
				bn = BigInteger.Multiply(bn, bn58);
				bn += bnChar;
			}

			// Get bignum as little endian data
			var vchTmp = bn.ToByteArray();
			if(vchTmp.All(b => b == 0))
				vchTmp = new byte[0];

			// Trim off sign byte if present
			if(vchTmp.Length >= 2 && vchTmp[vchTmp.Length - 1] == 0 && vchTmp[vchTmp.Length - 2] >= 0x80)
				vchTmp = vchTmp.Take(vchTmp.Length - 1).ToArray();

			// Restore leading zeros
			var nLeadingZeros = 0;
			for(var y = i ; y < encoded.Length && encoded[y] == PszBase58[0] ; y++)
				nLeadingZeros++;


			result = new byte[nLeadingZeros + vchTmp.Length];
			Array.Copy(vchTmp.Reverse().ToArray(), 0, result, nLeadingZeros, vchTmp.Length);
			return result;
		}


		public bool Check
		{
			get;
			set;
		}
	}
}
