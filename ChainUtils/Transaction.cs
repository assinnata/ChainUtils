using System;
using System.Collections.Generic;
using System.Linq;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;
using ChainUtils.Protocol;
using ChainUtils.RPC;

namespace ChainUtils
{
	public class OutPoint : IBitcoinSerializable
	{
		public bool IsNull
		{
			get
			{
				return (_hash == 0 && _n == uint.MaxValue);
			}
		}
		private Uint256 _hash;
		private uint _n;


		public Uint256 Hash
		{
			get
			{
				return _hash;
			}
			set
			{
				_hash = value;
			}
		}
		public uint N
		{
			get
			{
				return _n;
			}
			set
			{
				_n = value;
			}
		}

		public OutPoint()
		{
			SetNull();
		}
		public OutPoint(Uint256 hashIn, uint nIn)
		{
			_hash = hashIn;
			_n = nIn;
		}
		public OutPoint(Uint256 hashIn, int nIn)
		{
			_hash = hashIn;
			_n = nIn == -1 ? _n = uint.MaxValue : (uint)nIn;
		}

		public OutPoint(Transaction tx, uint i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(Transaction tx, int i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(OutPoint outpoint)
		{
			this.FromBytes(outpoint.ToBytes());
		}
		//IMPLEMENT_SERIALIZE( READWRITE(FLATDATA(*this)); )

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _hash);
			stream.ReadWrite(ref _n);
		}


		void SetNull()
		{
			_hash = 0;
			_n = uint.MaxValue;
		}

		public static bool operator <(OutPoint a, OutPoint b)
		{
			return (a._hash < b._hash || (a._hash == b._hash && a._n < b._n));
		}
		public static bool operator >(OutPoint a, OutPoint b)
		{
			return (a._hash > b._hash || (a._hash == b._hash && a._n > b._n));
		}

		public static bool operator ==(OutPoint a, OutPoint b)
		{
			if(ReferenceEquals(a, null))
			{
				return ReferenceEquals(b, null);
			}
			if(ReferenceEquals(b, null))
			{
				return ReferenceEquals(a, null);
			}
			return (a._hash == b._hash && a._n == b._n);
		}

		public static bool operator !=(OutPoint a, OutPoint b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			var item = obj as OutPoint;
			if(ReferenceEquals(null, item))
				return false;
			return item == this;
		}

		public override int GetHashCode()
		{
			return Tuple.Create(_hash, _n).GetHashCode();
		}

		public override string ToString()
		{
			return N + "-" + Hash;
		}
	}


	public class TxIn : IBitcoinSerializable
	{
		public TxIn()
		{

		}
		public TxIn(OutPoint prevout)
		{
			this._prevout = prevout;
		}
		OutPoint _prevout = new OutPoint();
		Script _scriptSig = Script.Empty;
		uint _nSequence = uint.MaxValue;
		public const uint NoSequence = uint.MaxValue;

		public uint Sequence
		{
			get
			{
				return _nSequence;
			}
			set
			{
				_nSequence = value;
			}
		}
		public OutPoint PrevOut
		{
			get
			{
				return _prevout;
			}
			set
			{
				_prevout = value;
			}
		}


		public Script ScriptSig
		{
			get
			{
				return _scriptSig;
			}
			set
			{
				_scriptSig = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _prevout);
			stream.ReadWrite(ref _scriptSig);
			stream.ReadWrite(ref _nSequence);
		}

		#endregion

		public bool IsFrom(PubKey pubKey)
		{
			var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			return result != null && result.PublicKey == pubKey;
		}
	}

	public class TxOutCompressor : IBitcoinSerializable
	{
		// Amount compression:
		// * If the amount is 0, output 0
		// * first, divide the amount (in base units) by the largest power of 10 possible; call the exponent e (e is max 9)
		// * if e<9, the last digit of the resulting number cannot be 0; store it as d, and drop it (divide by 10)
		//   * call the result n
		//   * output 1 + 10*(9*n + d - 1) + e
		// * if e==9, we only know the resulting number is not zero, so output 1 + 10*(n - 1) + 9
		// (this is decodable, as d is in [1-9] and e is in [0-9])

		ulong CompressAmount(ulong n)
		{
			if(n == 0)
				return 0;
			var e = 0;
			while(((n % 10) == 0) && e < 9)
			{
				n /= 10;
				e++;
			}
			if(e < 9)
			{
				var d = (int)(n % 10);
				n /= 10;
				return 1 + (n * 9 + (ulong)(d - 1)) * 10 + (ulong)e;
			}
			else
			{
				return 1 + (n - 1) * 10 + 9;
			}
		}

		ulong DecompressAmount(ulong x)
		{
			// x = 0  OR  x = 1+10*(9*n + d - 1) + e  OR  x = 1+10*(n - 1) + 9
			if(x == 0)
				return 0;
			x--;
			// x = 10*(9*n + d - 1) + e
			var e = (int)(x % 10);
			x /= 10;
			ulong n = 0;
			if(e < 9)
			{
				// x = 9*n + d - 1
				var d = (int)((x % 9) + 1);
				x /= 9;
				// x = n
				n = (x * 10 + (ulong)d);
			}
			else
			{
				n = x + 1;
			}
			while(e != 0)
			{
				n *= 10;
				e--;
			}
			return n;
		}


		private TxOut _txOut = new TxOut();
		public TxOut TxOut
		{
			get
			{
				return _txOut;
			}
		}
		public TxOutCompressor()
		{

		}
		public TxOutCompressor(TxOut txOut)
		{
			_txOut = txOut;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var val = CompressAmount((ulong)_txOut.Value.Satoshi);
				stream.ReadWriteAsCompactVarInt(ref val);
			}
			else
			{
				ulong val = 0;
				stream.ReadWriteAsCompactVarInt(ref val);
				_txOut.Value = new Money(DecompressAmount(val));
			}
			var cscript = new ScriptCompressor(_txOut.ScriptPubKey);
			stream.ReadWrite(ref cscript);
			if(!stream.Serializing)
				_txOut.ScriptPubKey = new Script(cscript.ScriptBytes);
		}

		#endregion
	}

	public class ScriptCompressor : IBitcoinSerializable
	{
		// make this static for now (there are only 6 special scripts defined)
		// this can potentially be extended together with a new nVersion for
		// transactions, in which case this value becomes dependent on nVersion
		// and nHeight of the enclosing transaction.
		const uint NSpecialScripts = 6;
		byte[] _script;
		public byte[] ScriptBytes
		{
			get
			{
				return _script;
			}
		}
		public ScriptCompressor(Script script)
		{
			_script = script.ToBytes(true);
		}
		public ScriptCompressor()
		{

		}

		public Script GetScript()
		{
			return new Script(_script);
		}

		private KeyId GetKeyId()
		{
			if(_script.Length == 25 && _script[0] == (byte)OpcodeType.OpDup && _script[1] == (byte)OpcodeType.OpHash160
								&& _script[2] == 20 && _script[23] == (byte)OpcodeType.OpEqualverify
								&& _script[24] == (byte)OpcodeType.OpChecksig)
			{
				return new KeyId(_script.Skip(3).Take(20).ToArray());
			}
			return null;
		}

		private ScriptId GetScriptId()
		{
			if(_script.Length == 23 && _script[0] == (byte)OpcodeType.OpHash160 && _script[1] == 20
								&& _script[22] == (byte)OpcodeType.OpEqual)
			{
				return new ScriptId(_script.Skip(2).Take(20).ToArray());
			}
			return null;
		}

		private PubKey GetPubKey()
		{
			return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(new Script(_script));
		}

		byte[] Compress()
		{
			byte[] result = null;
			var keyId = GetKeyId();
			if(keyId != null)
			{
				result = new byte[21];
				result[0] = 0x00;
				Array.Copy(keyId.ToBytes(), 0, result, 1, 20);
				return result;
			}
			var scriptId = GetScriptId();
			if(scriptId != null)
			{
				result = new byte[21];
				result[0] = 0x01;
				Array.Copy(scriptId.ToBytes(), 0, result, 1, 20);
				return result;
			}
			var pubkey = GetPubKey();
			if(pubkey != null)
			{
				result = new byte[33];
				var pubBytes = pubkey.ToBytes();
				Array.Copy(pubBytes, 1, result, 1, 32);
				if(pubBytes[0] == 0x02 || pubBytes[0] == 0x03)
				{
					result[0] = pubBytes[0];
					return result;
				}
				else if(pubBytes[0] == 0x04)
				{
					result[0] = (byte)(0x04 | (pubBytes[64] & 0x01));
					return result;
				}
			}
			return null;
		}

		Script Decompress(uint nSize, byte[] data)
		{
			switch(nSize)
			{
				case 0x00:
					return new Script(OpcodeType.OpDup, OpcodeType.OpHash160, Op.GetPushOp(data.Take(20).ToArray()), OpcodeType.OpEqualverify, OpcodeType.OpChecksig);
				case 0x01:
					return new Script(OpcodeType.OpHash160, Op.GetPushOp(data.Take(20).ToArray()), OpcodeType.OpEqual);
				case 0x02:
				case 0x03:
					return new Script(Op.GetPushOp(new[] { (byte)nSize }.Concat(data.Take(32)).ToArray()), OpcodeType.OpChecksig);
				case 0x04:
				case 0x05:
					var vch = new byte[33];
					vch[0] = (byte)(nSize - 2);
					Array.Copy(data, 0, vch, 1, 32);
					var pubkey = new PubKey(vch);
					pubkey = pubkey.Decompress();
					return new Script(Op.GetPushOp(pubkey.ToBytes()), OpcodeType.OpChecksig);
			}
			return null;
		}





		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var compr = Compress();
				if(compr != null)
				{
					stream.ReadWrite(ref compr);
					return;
				}
				var nSize = (uint)_script.Length + NSpecialScripts;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				stream.ReadWrite(ref _script);
			}
			else
			{
				uint nSize = 0;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				if(nSize < NSpecialScripts)
				{
					var vch = new byte[GetSpecialSize(nSize)];
					stream.ReadWrite(ref vch);
					_script = Decompress(nSize, vch).ToBytes();
					return;
				}
				nSize -= NSpecialScripts;
				_script = new byte[nSize];
				stream.ReadWrite(ref _script);
			}
		}

		private int GetSpecialSize(uint nSize)
		{
			if(nSize == 0 || nSize == 1)
				return 20;
			if(nSize == 2 || nSize == 3 || nSize == 4 || nSize == 5)
				return 32;
			return 0;
		}



		#endregion
	}

	public class TxOut : IBitcoinSerializable, IDestination
	{
		Script _publicKey = Script.Empty;
		public Script ScriptPubKey
		{
			get
			{
				return _publicKey;
			}
			set
			{
				_publicKey = value;
			}
		}

		private long _value = -1;
		Money _moneyValue;
		public bool IsNull
		{
			get
			{
				return _value == -1;
			}
		}



		public TxOut()
		{

		}

		public TxOut(Money value, IDestination destination)
		{
			Value = value;
			if(destination != null)
				ScriptPubKey = destination.ScriptPubKey;
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		public Money Value
		{
			get
			{
				if(_moneyValue == null)
					_moneyValue = new Money(_value);
				return _moneyValue;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				if(value.Satoshi > long.MaxValue || value.Satoshi < long.MinValue)
					throw new ArgumentOutOfRangeException("value", "satoshi's value should be between Int64.Max and Int64.Min");
				_moneyValue = value;
				this._value = (long)_moneyValue.Satoshi;
			}
		}


		public bool IsDust
		{
			get
			{
				// "Dust" is defined in terms of CTransaction::nMinRelayTxFee,
				// which has units satoshis-per-kilobyte.
				// If you'd pay more than 1/3 in fees
				// to spend something, then we consider it dust.
				// A typical txout is 34 bytes big, and will
				// need a CTxIn of at least 148 bytes to spend,
				// so dust is a txout less than 546 satoshis 
				// with default nMinRelayTxFee.
				return ((_value * 1000) / (3 * ((int)this.GetSerializedSize() + 148)) < Transaction.NMinRelayTxFee);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _value);
			stream.ReadWrite(ref _publicKey);
			_moneyValue = null; //Might been updated
		}

		#endregion

		public bool IsTo(IDestination destination)
		{
			return ScriptPubKey == destination.ScriptPubKey;
		}

		internal void SetNull()
		{
			_value = -1;
		}
	}

	public class IndexedTxIn
	{
		public TxIn TxIn
		{
			get;
			set;
		}
		public uint N
		{
			get;
			set;
		}

		public OutPoint PrevOut
		{
			get
			{
				return TxIn.PrevOut;
			}
		}

		public Transaction Transaction
		{
			get;
			set;
		}

		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(scriptPubKey, scriptVerify, out unused);
		}
		public bool VerifyScript(Script scriptPubKey, out ScriptError error)
		{
			return Script.VerifyScript(Transaction.Inputs[N].ScriptSig, scriptPubKey, Transaction, (int)N, out error);
		}
		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(Transaction.Inputs[N].ScriptSig, scriptPubKey, Transaction, (int)N, scriptVerify, SigHash.Undefined, out error);
		}

		public Uint256 GetSignatureHash(Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			return scriptPubKey.SignatureHash(Transaction, (int)N, sigHash);
		}
		public TransactionSignature Sign(ISecret secret, Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			return Sign(secret.PrivateKey, scriptPubKey, sigHash);
		}
		public TransactionSignature Sign(Key key, Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			var hash = GetSignatureHash(scriptPubKey, sigHash);
			return key.Sign(hash, sigHash);
		}
	}
	public class TxInList : UnsignedList<TxIn>
	{
		public TxInList()
		{

		}
		public TxInList(Transaction parent)
			: base(parent)
		{

		}
		public TxIn this[OutPoint outpoint]
		{
			get
			{
				return this[outpoint.N];
			}
			set
			{
				this[outpoint.N] = value;
			}
		}

		public IEnumerable<IndexedTxIn> AsIndexedInputs()
		{
			// We want i as the index of txIn in Intputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxIn()
			{
				TxIn = r,
				N = (uint)i,
				Transaction = Transaction
			});
		}
	}

	public class IndexedTxOut
	{
		public TxOut TxOut
		{
			get;
			set;
		}
		public uint N
		{
			get;
			set;
		}

		public Transaction Transaction
		{
			get;
			set;
		}
		public Coin ToCoin()
		{
			return new Coin(this);
		}
	}

	public class TxOutList : UnsignedList<TxOut>
	{
		public TxOutList()
		{

		}
		public TxOutList(Transaction parent)
			: base(parent)
		{

		}
		public IEnumerable<TxOut> To(IDestination destination)
		{
			return this.Where(r => r.IsTo(destination));
		}
		public IEnumerable<TxOut> To(Script scriptPubKey)
		{
			return this.Where(r => r.ScriptPubKey == scriptPubKey);
		}

		public IEnumerable<IndexedTxOut> AsIndexedOutputs()
		{
			// We want i as the index of txOut in Outputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxOut()
			{
				TxOut = r,
				N = (uint)i,
				Transaction = Transaction
			});
		}

		public IEnumerable<Coin> AsCoins()
		{
			return AsIndexedOutputs().Select(i => i.ToCoin());
		}

		public IEnumerable<IndexedTxOut> AsSpendableIndexedOutputs()
		{
			return AsIndexedOutputs()
					.Where(r => !r.TxOut.ScriptPubKey.IsUnspendable);
		}
	}

	public enum RawFormat
	{
		Satoshi,
		BlockExplorer,
	}
	//https://en.bitcoin.it/wiki/Transactions
	//https://en.bitcoin.it/wiki/Protocol_specification
	public class Transaction : IBitcoinSerializable
	{
		uint _nVersion = 1;

		public uint Version
		{
			get
			{
				return _nVersion;
			}
			set
			{
				_nVersion = value;
			}
		}
		TxInList _vin;
		TxOutList _vout;
		LockTime _nLockTime;

		public Transaction()
		{
			Init();
		}

		private void Init()
		{
			_vin = new TxInList(this);
			_vout = new TxOutList(this);
		}
		public Transaction(string hex)
		{
			Init();
			this.FromBytes(Encoders.Hex.DecodeData(hex));
		}
		public Transaction(byte[] bytes)
		{
			Init();
			this.FromBytes(bytes);
		}

		public Money TotalOut
		{
			get
			{
				return Outputs.Sum(v => v.Value);
			}
		}

		public LockTime LockTime
		{
			get
			{
				return _nLockTime;
			}
			set
			{
				_nLockTime = value;
			}
		}

		public TxInList Inputs
		{
			get
			{
				return _vin;
			}
		}
		public TxOutList Outputs
		{
			get
			{
				return _vout;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _nVersion);
			stream.ReadWrite<TxInList, TxIn>(ref _vin);
			_vin.Transaction = this;
			stream.ReadWrite<TxOutList, TxOut>(ref _vout);
			_vout.Transaction = this;
			stream.ReadWriteStruct(ref _nLockTime);
		}

		#endregion

		public Uint256 GetHash()
		{
			return Hashes.Hash256(this.ToBytes());
		}
		public Uint256 GetSignatureHash(Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[nIn].GetSignatureHash(scriptPubKey, sigHash);
		}
		public TransactionSignature SignInput(ISecret secret, Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return SignInput(secret.PrivateKey, scriptPubKey, nIn, sigHash);
		}
		public TransactionSignature SignInput(Key key, Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[nIn].Sign(key, scriptPubKey, sigHash);
		}

		public bool IsCoinBase
		{
			get
			{
				return (Inputs.Count == 1 && Inputs[0].PrevOut.IsNull);
			}
		}

		public const long NMinTxFee = 10000;  // Override with -mintxfee
		public const long NMinRelayTxFee = 1000;

		public static uint CurrentVersion = 2;
		public static uint MaxStandardTxSize = 100000;

		public TxOut AddOutput(Money money, BitcoinAddress address)
		{
			return AddOutput(new TxOut(money, address));
		}
		public TxOut AddOutput(Money money, KeyId keyId)
		{
			return AddOutput(new TxOut(money, keyId));
		}
		public TxOut AddOutput(Money money, Script scriptPubKey)
		{
			return AddOutput(new TxOut(money, scriptPubKey));
		}
		public TxOut AddOutput(TxOut @out)
		{
			_vout.Add(@out);
			return @out;
		}
		public TxIn AddInput(TxIn @in)
		{
			_vin.Add(@in);
			return @in;
		}

		public TxIn AddInput(Transaction prevTx, int outIndex)
		{
			if(outIndex >= prevTx.Outputs.Count)
				throw new InvalidOperationException("Output " + outIndex + " is not present in the prevTx");
			var @in = new TxIn();
			@in.PrevOut.Hash = prevTx.GetHash();
			@in.PrevOut.N = (uint)outIndex;
			AddInput(@in);
			return @in;
		}


		/// <summary>
		/// Sign the transaction with a private key
		/// <para>ScriptSigs should be filled with previous ScriptPubKeys</para>
		/// <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		public void Sign(ISecret secret, bool assumeP2Sh)
		{
			Sign(secret.PrivateKey, assumeP2Sh);
		}

		/// <summary>
		/// Sign the transaction with a private key
		/// <para>ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)</para>
		/// <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		public void Sign(Key key, bool assumeP2Sh)
		{
			var builder = new TransactionBuilder();
			builder.AddKeys(key);
			for(var i = 0 ; i < Inputs.Count ; i++)
			{
				var txin = Inputs[i];
				if(Script.IsNullOrEmpty(txin.ScriptSig))
					throw new InvalidOperationException("ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)");
				if(assumeP2Sh)
				{
					var p2ShSig = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txin.ScriptSig);
					if(p2ShSig == null)
					{
						builder.AddCoins(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = txin.ScriptSig.PaymentScript,
						}, txin.ScriptSig));
					}
					else
					{
						builder.AddCoins(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = p2ShSig.RedeemScript.PaymentScript
						}, p2ShSig.RedeemScript));
					}
				}
				else
				{
					builder.AddCoins(new Coin(txin.PrevOut, new TxOut()
					{
						ScriptPubKey = txin.ScriptSig
					}));
				}

			}
			builder.SignTransactionInPlace(this);
		}

		public TxPayload CreatePayload()
		{
			return new TxPayload(this.Clone());
		}


		public static Transaction Parse(string tx, RawFormat format, Network network = null)
		{
			return GetFormatter(format, network).Parse(tx);
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(this.ToBytes());
		}

		public override string ToString()
		{
			return ToString(RawFormat.BlockExplorer);
		}

		public string ToString(RawFormat rawFormat, Network network = null)
		{
			var formatter = GetFormatter(rawFormat, network);
			return ToString(formatter);
		}

		static private RawFormatter GetFormatter(RawFormat rawFormat, Network network)
		{
			RawFormatter formatter = null;
			switch(rawFormat)
			{
				case RawFormat.Satoshi:
					formatter = new SatoshiFormatter();
					break;
				case RawFormat.BlockExplorer:
					formatter = new BlockExplorerFormatter();
					break;
				default:
					throw new NotSupportedException(rawFormat.ToString());
			}
			formatter.Network = network ?? formatter.Network;
			return formatter;
		}

		internal string ToString(RawFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");
			return formatter.ToString(this);
		}
	}

}
