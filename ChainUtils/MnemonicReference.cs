using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChainUtils.Crypto;

namespace ChainUtils
{
	public class InvalidBrainAddressException : Exception
	{
		public InvalidBrainAddressException(string message)
			: base(message)
		{

		}
	}

	public class MnemonicReference
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="chain"></param>
		/// <param name="blockRepository"></param>
		/// <param name="blockHeight"></param>
		/// <param name="txIndex"></param>
		/// <param name="txOutIndex"></param>
		/// <exception cref="ChainUtils.InvalidBrainAddressException"></exception>
		/// <returns></returns>
		public static async Task<MnemonicReference> CreateAsync
			(ChainBase chain,
			IBlockRepository blockRepository,
			int blockHeight, int txIndex, int txOutIndex)
		{
			var header = chain.GetBlock(blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			return Create(chain, block, blockHeight, txIndex, txOutIndex);
		}

		public static async Task<MnemonicReference> ParseAsync
			(ChainBase chain,
			IBlockRepository blockRepository,
			Wordlist wordList,
			string sentence)
		{
			var w = wordList.GetWords(sentence).Length;
			var finalAddress = wordList.ToBits(sentence);
			var rawAddress = DecryptFinalAddress(finalAddress);

			int blockHeight;
			var x = DecodeBlockHeight(rawAddress, out blockHeight);

			var header = chain.GetBlock(blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");

			var y1 = BitCount((int)block.Transactions.Count);
			var y2 = 11 * w - 1 - x - C;
			var y = Math.Min(y1, y2);
			var txIndex = Decode(Substring(rawAddress, x, y));
			if(txIndex >= block.Transactions.Count)
				throw new InvalidBrainAddressException("The Transaction Index is out of the bound of the block");

			var transaction = block.Transactions[(int)txIndex];
			return Parse(chain, wordList, sentence, transaction, block);
		}



		public static MnemonicReference Create
			(
			ChainBase chain,
			Block block,
			int blockHeight,
			int txIndex,
			int txOutIndex)
		{
			var header = chain.GetBlock(blockHeight);
			if(header == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			if(txIndex >= block.Transactions.Count)
				throw new InvalidBrainAddressException("The Transaction Index is out of the bound of the block");
			var transaction = block.Transactions[txIndex];
			return Create(chain, transaction, block, txOutIndex);
		}

		public static MnemonicReference Create(ChainBase chain, Transaction transaction, Block block, int txOutIndex)
		{
			return Create(chain, transaction, block.Filter(transaction.GetHash()), txOutIndex);
		}

		public static MnemonicReference Create
			(ChainBase chain,
			Transaction transaction,
			MerkleBlock merkleBlock,
			int txOutIndex)
		{
			var blockId = merkleBlock.Header.GetHash();
			var merkleRoot = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();
			if(merkleRoot == null || merkleRoot.Hash != merkleBlock.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid merkle block");
			if(txOutIndex >= transaction.Outputs.Count)
				throw new InvalidBrainAddressException("The specified txout index is outside of the transaction bounds");
			var matchedLeaf = merkleRoot.GetLeafs().Select((node, index) => new
			{
				node,
				index
			}).FirstOrDefault(_ => _.node.Hash == transaction.GetHash());
			if(matchedLeaf == null)
				throw new InvalidBrainAddressException("Transaction not included in this merkle block");

			var chainedHeader = chain.GetBlock(blockId);
			if(chainedHeader == null)
				throw new InvalidBrainAddressException("The block provided is not in the current chain");
			var blockHeight = chainedHeader.Height;
			var txIndex = matchedLeaf.index;
			var txOut = transaction.Outputs[txOutIndex];
			var block = chain.GetBlock(blockId);


			var encodedBlockHeight = EncodeBlockHeight(blockHeight);
			var x = encodedBlockHeight.Length;

			//ymin = ceiling(log(txIndex + 1, 2))
			var ymin = BitCount(txIndex + 1);
			//zmin = ceiling(log(outputIndex + 1, 2))
			var zmin = BitCount(txOutIndex + 1);

			//w = ceiling((x + ymin + zmin + c + 1)/11)
			var w = RoundTo(x + ymin + zmin + C + 1, 11) / 11;
			var y = 0;
			var z = 0;
			for( ; ; w++)
			{
				var y1 = BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount);
				var y2 = 11 * w - 1 - x - C;
				y = Math.Min(y1, y2);
				if(ymin > y)
					continue;
				var z1 = BitCount(transaction.Outputs.Count);
				var z2 = 11 * w - 1 - x - y - C;
				z = Math.Min(z1, z2);
				if(zmin > z)
					continue;
				break;
			}

			var cs = 11 * w - 1 - x - y - z;
			var checksum = CalculateChecksum(blockId, txIndex, txOutIndex, txOut.ScriptPubKey, cs);

			var rawAddress = Concat(encodedBlockHeight, Encode(txIndex, y), Encode(txOutIndex, z), checksum);

			var finalAddress = EncryptRawAddress(rawAddress);

			return new MnemonicReference()
			{
				BlockHeight = blockHeight,
				TransactionIndex = txIndex,
				OutputIndex = txOutIndex,
				Checksum = checksum,
				WordIndices = Wordlist.ToIntegers(finalAddress),
				Output = transaction.Outputs[txOutIndex],
				Transaction = transaction,
				BlockId = blockId
			};
		}


		private static BitArray Concat(params BitArray[] arrays)
		{
			var result = new BitArray(arrays.Select(a => a.Length).Sum());
			var i = 0;
			foreach(var v in arrays.SelectMany(a => a.OfType<bool>()))
			{
				result.Set(i, v);
				i++;
			}
			return result;
		}




		public static MnemonicReference Parse
			(ChainBase chain,
			Wordlist wordList,
			string sentence,
			Transaction transaction,
			Block block)
		{
			return Parse(chain, wordList, sentence, transaction, block.Filter(transaction.GetHash()));
		}
		public static MnemonicReference Parse
			(ChainBase chain,
			Wordlist wordList,
			string sentence,
			Transaction transaction,
			MerkleBlock merkleBlock)
		{
			var indices = wordList.ToIndices(sentence);

			//Step1: Determine w = number of words in the mnemonic code 
			var w = indices.Length;

			//Convert mnemonic code into finalAddress following BIP-0039
			var finalAddress = Wordlist.ToBits(indices);

			var rawAddress = DecryptFinalAddress(finalAddress);
			var blockHeight = 0;
			var x = DecodeBlockHeight(rawAddress, out blockHeight);

			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			if(header.HashBlock != merkleBlock.Header.GetHash())
				throw new InvalidBrainAddressException("The provided merkleblock do not match the block of the sentence");
			var blockId = header.HashBlock;
			var root = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();
			if(root == null || root.Hash != header.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid partial merkle tree");

			var y1 = BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount);
			var y2 = 11 * w - 1 - x - C;
			var y = Math.Min(y1, y2);
			var txIndex = Decode(Substring(rawAddress, x, y));

			var txLeaf = root.GetLeafs().Skip((int)txIndex).FirstOrDefault();
			if(txLeaf == null || txLeaf.Hash != transaction.GetHash())
				throw new InvalidBrainAddressException("The transaction do not appear in the block");

			var z1 = BitCount(transaction.Outputs.Count);
			var z2 = 11 * w - 1 - x - y - C;
			var z = Math.Min(z1, z2);
			var outputIndex = Decode(Substring(rawAddress, x + y, z));

			if(outputIndex >= transaction.Outputs.Count)
				throw new InvalidBrainAddressException("The specified txout index is outside of the transaction bounds");
			var txOut = transaction.Outputs[outputIndex];


			var cs = 11 * w - 1 - x - y - z;
			var actualChecksum = Substring(rawAddress, x + y + z, cs);
			var expectedChecksum = CalculateChecksum(blockId, txIndex, outputIndex, txOut.ScriptPubKey, cs);

			if(!actualChecksum.OfType<bool>().SequenceEqual(expectedChecksum.OfType<bool>()))
				throw new InvalidBrainAddressException("Invalid checksum");

			return new MnemonicReference()
			{
				BlockHeight = (int)blockHeight,
				TransactionIndex = (int)txIndex,
				WordIndices = indices,
				Checksum = actualChecksum,
				Output = transaction.Outputs[outputIndex],
				OutputIndex = (int)outputIndex,
				BlockId = blockId,
				Transaction = transaction
			};
		}

		const int C = 20;
		private static BitArray DecryptFinalAddress(BitArray finalAddress)
		{
			if(finalAddress[finalAddress.Length - 1] != false)
				throw new InvalidBrainAddressException("Invalid version bit");
			var encryptionKey = Substring(finalAddress, finalAddress.Length - 1 - C, C);
			var encryptedAddress = Xor(Substring(finalAddress, 0, finalAddress.Length - 1 - C), encryptionKey);
			return Concat(encryptedAddress, encryptionKey);
		}
		private static BitArray EncryptRawAddress(BitArray rawAddress)
		{
			var encryptionKey = Substring(rawAddress, rawAddress.Length - C, C);
			var encryptedAddress = Xor(Substring(rawAddress, 0, rawAddress.Length - C), encryptionKey);
			var finalAddress = Concat(encryptedAddress, encryptionKey, new BitArray(new[] { false }));
			return finalAddress;
		}


		static BitArray Xor(BitArray a, BitArray b)
		{
			var result = new BitArray(a.Length);
			for(int i = 0, y = 0 ; i < a.Length ; i++, y++)
			{
				if(y >= b.Length)
					y = 0;
				result.Set(i, a.Get(i) ^ b.Get(y));
			}
			return result;
		}

		private static BitArray Substring(BitArray input, int from, int count)
		{
			return new BitArray(input.OfType<bool>().Skip(from).Take(count).ToArray());
		}

		private static BitArray CalculateChecksum(Uint256 blockId, int txIndex, int txOutIndex, Script scriptPubKey, int bitCount)
		{
			//All in little endian
			var hashed =
				blockId
				.ToBytes(true)
				.Concat(Utils.ToBytes((uint)txIndex, true))
				.Concat(Utils.ToBytes((uint)txOutIndex, true))
				.Concat(scriptPubKey.ToBytes(true))
				.ToArray();
			var hash = Hashes.Hash256(hashed);
			var bytes = hash.ToBytes(true);
			var result = new BitArray(bitCount);
			for(var i = 0 ; i < bitCount ; i++)
			{
				var byteIndex = i / 8;
				var bitIndex = i % 8;
				result.Set(i, ((bytes[byteIndex] >> bitIndex) & 1) == 1);
			}
			return result;
		}

		//Step1: Determine the number of bits and encoding of blockHeight
		//blockHeight takes x bits and is encoded as follow:

		//	For height =< 1,048,575 (0-1111-1111-1111-1111-1111), blockHeight is the height as 21bit interger
		//	For 1,048,575 < height =< 8,388,607, blockHeight is Concat(1, height as 23 bit integer), which totally takes 24bit. For example, block 1234567 is 1001-0010-1101-0110-1000-0111
		//	For height > 8,388,607, it is undefined and returns error
		private static BitArray EncodeBlockHeight(int blockHeight)
		{
			if(blockHeight <= 1048575)
			{
				return Concat(new BitArray(new[] { false }), Encode(blockHeight, 20));
			}
			else if(1048575 < blockHeight && blockHeight <= 8388607)
			{
				return Concat(new BitArray(new[] { true }), Encode(blockHeight, 23));
			}
			else
			{
				throw new ArgumentOutOfRangeException("Impossible to reference an output after block 8,388,607");
			}
		}
		private static int DecodeBlockHeight(BitArray rawAddress, out int blockHeight)
		{
			if(!rawAddress.Get(0))
			{
				blockHeight = Decode(Substring(rawAddress, 1, 20));
				return 21;
			}
			else
			{
				blockHeight = Decode(Substring(rawAddress, 1, 23));
				return 24;
			}
		}

		private static BitArray Encode(int value, int bitCount)
		{
			var result = new BitArray(bitCount);
			for(var i = 0 ; i < bitCount ; i++)
			{
				result.Set(i, (((value >> i) & 1) == 1));
			}
			return result;
		}
#if DEBUG && !PORTABLE
		static string ToBitString(BitArray bits)
		{
			var sb = new StringBuilder();

			for(var i = 0 ; i < bits.Count ; i++)
			{
				var c = bits[i] ? '1' : '0';
				sb.Append(c);
			}
			return sb.ToString();
		}
#endif

		private static int Decode(BitArray array)
		{
			var result = 0;
			for(var i = 0 ; i < array.Length ; i++)
			{
				if(array.Get(i))
					result += 1 << i;
			}
			return result;
		}

		static int RoundTo(int value, int roundTo)
		{
			var result = (value / roundTo) * roundTo;
			if(value % roundTo != 0)
				result += roundTo;
			return result;
		}
		static int BitCount(int possibilities)
		{
			possibilities = Math.Max(0, possibilities);
			possibilities--;
			var bitCount = 0;
			while(possibilities != 0)
			{
				possibilities = possibilities >> 1;
				bitCount++;
			}
			return bitCount;
		}

		private MnemonicReference()
		{

		}

		public override string ToString()
		{
			return ToString(Wordlist.English);
		}

		public string ToString(Wordlist wordlist)
		{
			return wordlist.GetSentence(WordIndices);
		}

		public int[] WordIndices
		{
			get;
			private set;
		}

		public BitArray Checksum
		{
			get;
			private set;
		}

		public TxOut Output
		{
			get;
			private set;
		}

		public Uint256 BlockId
		{
			get;
			private set;
		}

		public int BlockHeight
		{
			get;
			private set;
		}
		public int TransactionIndex
		{
			get;
			private set;
		}
		public int OutputIndex
		{
			get;
			private set;
		}

		public Transaction Transaction
		{
			get;
			private set;
		}
	}
}
