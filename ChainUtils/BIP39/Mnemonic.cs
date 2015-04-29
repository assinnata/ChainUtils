using System;
using System.Collections;
using System.Linq;
using System.Text;
using ChainUtils.Crypto;
#if !USEBC
using System.Security.Cryptography;
#endif

namespace ChainUtils
{
	/// <summary>
	/// A .NET implementation of the Bitcoin Improvement Proposal - 39 (BIP39)
	/// BIP39 specification used as reference located here: https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki
	/// Made by thashiznets@yahoo.com.au
	/// v1.0.1.1
	/// I ♥ Bitcoin :)
	/// Bitcoin:1ETQjMkR1NNh4jwLuN5LxY7bMsHC9PUPSV
	/// </summary>
	public class Mnemonic
	{
		public Mnemonic(string mnemonic, Wordlist wordlist = null)
		{
			if(mnemonic == null)
				throw new ArgumentNullException("mnemonic");
			_mnemonic = mnemonic.Trim();

			if(wordlist == null)
				wordlist = Wordlist.AutoDetect(mnemonic) ?? Wordlist.English;

			var words = mnemonic.Split(new[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
			//if the sentence is not at least 12 characters or cleanly divisible by 3, it is bad!
			if(!CorrectWordCount(words.Length))
			{
				throw new FormatException("Word count should be equals to 12,15,18,21 or 24");
			}
			_words = words;
			_wordList = wordlist;
			_indices = wordlist.ToIndices(words);
		}

		/// <summary>
		/// Generate a mnemonic
		/// </summary>
		/// <param name="wordList"></param>
		/// <param name="entropy"></param>
		public Mnemonic(Wordlist wordList, byte[] entropy = null)
		{
			wordList = wordList ?? Wordlist.English;
			_wordList = wordList;
			if(entropy == null)
				entropy = RandomUtils.GetBytes(32);

			var i = Array.IndexOf(EntArray, entropy.Length * 8);
			if(i == -1)
				throw new ArgumentException("The length for entropy should be : " + String.Join(",", EntArray), "entropy");

			var entcs = EntcsArray[i];
			var ent = EntArray[i];
			var cs = CsArray[i];
			var checksum = Hashes.SHA256(entropy);
			var entcsResult = new BitWriter();

			entcsResult.Write(entropy);
			entcsResult.Write(checksum, cs);
			_indices = entcsResult.ToIntegers();
			_words = _wordList.GetWords(_indices);
			_mnemonic = _wordList.GetSentence(_indices);
		}

		public Mnemonic(Wordlist wordList, WordCount wordCount)
			: this(wordList, GenerateEntropy(wordCount))
		{

		}

		private static byte[] GenerateEntropy(WordCount wordCount)
		{
			var ms = (int)wordCount;
			if(!CorrectWordCount(ms))
				throw new ArgumentException("Word count should be equal to 12,15,18,21 or 24", "wordCount");
			var i = Array.IndexOf(MsArray, (int)wordCount);
			return RandomUtils.GetBytes(EntArray[i] / 8);
		}

		static readonly int[] MsArray = new[] { 12, 15, 18, 21, 24 };
		static readonly int[] EntcsArray = new[] { 132, 165, 198, 231, 264 };
		static readonly int[] CsArray = new[] { 4, 5, 6, 7, 8 };
		static readonly int[] EntArray = new[] { 128, 160, 192, 224, 256 };

		bool? _isValidChecksum;
		public bool IsValidChecksum
		{
			get
			{
				if(_isValidChecksum == null)
				{
					var i = Array.IndexOf(MsArray, _indices.Length);
					var cs = CsArray[i];
					var ent = EntArray[i];

					var writer = new BitWriter();
					var bits = Wordlist.ToBits(_indices);
					writer.Write(bits, ent);
					var entropy = writer.ToBytes();
					var checksum = Hashes.SHA256(entropy);

					writer.Write(checksum, cs);
					var expectedIndices = writer.ToIntegers();
					_isValidChecksum = expectedIndices.SequenceEqual(_indices);
				}
				return _isValidChecksum.Value;
			}
		}

		//private IEnumerable<bool> ToBits(int value)
		//{
		//	return null;
		//}

		private static bool CorrectWordCount(int ms)
		{
			return MsArray.Any(_ => _ == ms);
		}


		private int ToInt(BitArray bits)
		{
			if(bits.Length != 11)
			{
				throw new InvalidOperationException("should never happen, bug in ChainUtils");
			}

			var number = 0;
			var base2Divide = 1024; //it's all downhill from here...literally we halve this for each bit we move to.

			//literally picture this loop as going from the most significant bit across to the least in the 11 bits, dividing by 2 for each bit as per binary/base 2
			foreach(bool b in bits)
			{
				if(b)
				{
					number = number + base2Divide;
				}

				base2Divide = base2Divide / 2;
			}

			return number;
		}

		private readonly Wordlist _wordList;
		public Wordlist WordList
		{
			get
			{
				return _wordList;
			}
		}

		private readonly int[] _indices;
		public int[] Indices
		{
			get
			{
				return _indices;
			}
		}
		private readonly string[] _words;
		public string[] Words
		{
			get
			{
				return _words;
			}
		}

		public byte[] DeriveSeed(string passphrase = null)
		{
			passphrase = passphrase ?? "";
			var salt = Concat(Encoding.UTF8.GetBytes("mnemonic"), Normalize(passphrase));
			var bytes = Normalize(_mnemonic);

#if !USEBC
			return Pbkdf2.ComputeDerivedKey(new HMACSHA512(bytes), salt, 2048, 64);
#else
			var mac = MacUtilities.GetMac("HMAC-SHA_512");
			mac.Init(new KeyParameter(bytes));
			return Pbkdf2.ComputeDerivedKey(mac, salt, 2048, 64);
#endif

		}

		internal static byte[] Normalize(string str)
		{
			return Encoding.UTF8.GetBytes(NormalizeString(str));
		}

		internal static string NormalizeString(string word)
		{
#if !NOSTRNORMALIZE
			return word.Normalize(NormalizationForm.FormKD);
#else
			return KDTable.NormalizeKD(word);
#endif
		}

		public ExtKey DeriveExtKey(string passphrase = null)
		{
			return new ExtKey(DeriveSeed(passphrase));
		}

		static Byte[] Concat(Byte[] source1, Byte[] source2)
		{
			//Most efficient way to merge two arrays this according to http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
			var buffer = new Byte[source1.Length + source2.Length];
			Buffer.BlockCopy(source1, 0, buffer, 0, source1.Length);
			Buffer.BlockCopy(source2, 0, buffer, source1.Length, source2.Length);

			return buffer;
		}


		string _mnemonic;
		public override string ToString()
		{
			return _mnemonic;
		}


	}
	public enum WordCount : int
	{
		Twelve = 12,
		Fifteen = 15,
		Eighteen = 18,
		TwentyOne = 21,
		TwentyFour = 24
	}
}