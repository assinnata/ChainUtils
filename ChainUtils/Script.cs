using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	/** Script verification flags */
	[Flags]
	public enum ScriptVerify : uint
	{
		None = 0,

		// Evaluate P2SH subscripts (softfork safe, BIP16).
		P2Sh = (1U << 0),

		// Passing a non-strict-DER signature or one with undefined hashtype to a checksig operation causes script failure.
		// Passing a pubkey that is not (0x04 + 64 bytes) or (0x02 or 0x03 + 32 bytes) to checksig causes that pubkey to be
		// skipped (not softfork safe: this flag can widen the validity of OP_CHECKSIG OP_NOT).
		StrictEnc = (1U << 1),

		// Passing a non-strict-DER signature to a checksig operation causes script failure (softfork safe, BIP62 rule 1)
		DerSig = (1U << 2),

		// Passing a non-strict-DER signature or one with S > order/2 to a checksig operation causes script failure
		// (softfork safe, BIP62 rule 5).
		LowS = (1U << 3),

		// verify dummy stack item consumed by CHECKMULTISIG is of zero-length (softfork safe, BIP62 rule 7).
		NullDummy = (1U << 4),

		// Using a non-push operator in the scriptSig causes script failure (softfork safe, BIP62 rule 2).
		SigPushOnly = (1U << 5),

		// Require minimal encodings for all push operations (OP_0... OP_16, OP_1NEGATE where possible, direct
		// pushes up to 75 bytes, OP_PUSHDATA up to 255 bytes, OP_PUSHDATA2 for anything larger). Evaluating
		// any other push causes the script to fail (BIP62 rule 3).
		// In addition, whenever a stack element is interpreted as a number, it must be of minimal length (BIP62 rule 4).
		// (softfork safe)
		MinimalData = (1U << 6),

		// Discourage use of NOPs reserved for upgrades (NOP1-10)
		//
		// Provided so that nodes can avoid accepting or mining transactions
		// containing executed NOP's whose meaning may change after a soft-fork,
		// thus rendering the script invalid; with this flag set executing
		// discouraged NOPs fails the script. This verification flag will never be
		// a mandatory flag applied to scripts in a block. NOPs that are not
		// executed, e.g.  within an unexecuted IF ENDIF block, are *not* rejected.
		DiscourageUpgradableNops = (1U << 7),

		// Require that only a single stack element remains after evaluation. This changes the success criterion from
		// "At least one stack element must remain, and when interpreted as a boolean, it must be true" to
		// "Exactly one stack element must remain, and when interpreted as a boolean, it must be true".
		// (softfork safe, BIP62 rule 6)
		// Note: CLEANSTACK should never be used without P2SH.
		CleanStack = (1U << 8),

		/**
 * Mandatory script verification flags that all new blocks must comply with for
 * them to be valid. (but old blocks may not comply with) Currently just P2SH,
 * but in the future other flags may be added, such as a soft-fork to enforce
 * strict DER encoding.
 * 
 * Failing one of these tests may trigger a DoS ban - see CheckInputs() for
 * details.
 */
		Mandatory = P2Sh,
		/**
 * Standard script verification flags that standard transactions will comply
 * with. However scripts violating these flags may still be present in valid
 * blocks and we must accept those blocks.
 */
		Standard =
			Mandatory |
			DerSig |
			StrictEnc |
			MinimalData |
			NullDummy |
			DiscourageUpgradableNops |
			CleanStack,
	}

	/** Signature hash types/flags */
	public enum SigHash : uint
	{
		Undefined = 0,
		All = 1,
		None = 2,
		Single = 3,
		AnyoneCanPay = 0x80,
	};

	/** Script opcodes */
	public enum OpcodeType : byte
	{
		// push value
		Op0 = 0x00,
		OpFalse = Op0,
		OpPushdata1 = 0x4c,
		OpPushdata2 = 0x4d,
		OpPushdata4 = 0x4e,
		Op_1Negate = 0x4f,
		OpReserved = 0x50,
		Op1 = 0x51,
		OpTrue = Op1,
		Op2 = 0x52,
		Op3 = 0x53,
		Op4 = 0x54,
		Op5 = 0x55,
		Op6 = 0x56,
		Op7 = 0x57,
		Op8 = 0x58,
		Op9 = 0x59,
		Op10 = 0x5a,
		Op11 = 0x5b,
		Op12 = 0x5c,
		Op13 = 0x5d,
		Op14 = 0x5e,
		Op15 = 0x5f,
		Op16 = 0x60,

		// control
		OpNop = 0x61,
		OpVer = 0x62,
		OpIf = 0x63,
		OpNotif = 0x64,
		OpVerif = 0x65,
		OpVernotif = 0x66,
		OpElse = 0x67,
		OpEndif = 0x68,
		OpVerify = 0x69,
		OpReturn = 0x6a,

		// stack ops
		OpToaltstack = 0x6b,
		OpFromaltstack = 0x6c,
		Op_2Drop = 0x6d,
		Op_2Dup = 0x6e,
		Op_3Dup = 0x6f,
		Op_2Over = 0x70,
		Op_2Rot = 0x71,
		Op_2Swap = 0x72,
		OpIfdup = 0x73,
		OpDepth = 0x74,
		OpDrop = 0x75,
		OpDup = 0x76,
		OpNip = 0x77,
		OpOver = 0x78,
		OpPick = 0x79,
		OpRoll = 0x7a,
		OpRot = 0x7b,
		OpSwap = 0x7c,
		OpTuck = 0x7d,

		// splice ops
		OpCat = 0x7e,
		OpSubstr = 0x7f,
		OpLeft = 0x80,
		OpRight = 0x81,
		OpSize = 0x82,

		// bit logic
		OpInvert = 0x83,
		OpAnd = 0x84,
		OpOr = 0x85,
		OpXor = 0x86,
		OpEqual = 0x87,
		OpEqualverify = 0x88,
		OpReserved1 = 0x89,
		OpReserved2 = 0x8a,

		// numeric
		Op_1Add = 0x8b,
		Op_1Sub = 0x8c,
		Op_2Mul = 0x8d,
		Op_2Div = 0x8e,
		OpNegate = 0x8f,
		OpAbs = 0x90,
		OpNot = 0x91,
		Op_0Notequal = 0x92,

		OpAdd = 0x93,
		OpSub = 0x94,
		OpMul = 0x95,
		OpDiv = 0x96,
		OpMod = 0x97,
		OpLshift = 0x98,
		OpRshift = 0x99,

		OpBooland = 0x9a,
		OpBoolor = 0x9b,
		OpNumequal = 0x9c,
		OpNumequalverify = 0x9d,
		OpNumnotequal = 0x9e,
		OpLessthan = 0x9f,
		OpGreaterthan = 0xa0,
		OpLessthanorequal = 0xa1,
		OpGreaterthanorequal = 0xa2,
		OpMin = 0xa3,
		OpMax = 0xa4,

		OpWithin = 0xa5,

		// crypto
		OpRipemd160 = 0xa6,
		OpSha1 = 0xa7,
		OpSha256 = 0xa8,
		OpHash160 = 0xa9,
		OpHash256 = 0xaa,
		OpCodeseparator = 0xab,
		OpChecksig = 0xac,
		OpChecksigverify = 0xad,
		OpCheckmultisig = 0xae,
		OpCheckmultisigverify = 0xaf,

		// expansion
		OpNop1 = 0xb0,
		OpNop2 = 0xb1,
		OpNop3 = 0xb2,
		OpNop4 = 0xb3,
		OpNop5 = 0xb4,
		OpNop6 = 0xb5,
		OpNop7 = 0xb6,
		OpNop8 = 0xb7,
		OpNop9 = 0xb8,
		OpNop10 = 0xb9,



		// template matching params
		OpSmalldata = 0xf9,
		OpSmallinteger = 0xfa,
		OpPubkeys = 0xfb,
		OpPubkeyhash = 0xfd,
		OpPubkey = 0xfe,

		OpInvalidopcode = 0xff,
	};

	public class Script
	{
		static readonly Script _Empty = new Script();
		public static Script Empty
		{
			get
			{
				return _Empty;
			}
		}

		internal byte[] _Script = new byte[0];
		public Script()
		{

		}
		public Script(params Op[] ops)
			: this((IEnumerable<Op>)ops)
		{
		}

		public Script(IEnumerable<Op> ops)
		{
			var ms = new MemoryStream();
			foreach(var op in ops)
			{
				op.WriteTo(ms);
			}
			_Script = ms.ToArray();
		}

		public Script(string script)
		{
			_Script = Parse(script);
		}

		private static byte[] Parse(string script)
		{
			var reader = new StringReader(script);
			var result = new MemoryStream();
			while(reader.Peek() != -1)
			{
				Op.Read(reader).WriteTo(result);
			}
			return result.ToArray();
		}

		public static Script FromBytesUnsafe(byte[] data)
		{
			return new Script(data, true, true);
		}

		public Script(byte[] data)
			: this((IEnumerable<byte>)data)
		{
		}


		private Script(byte[] data, bool @unsafe, bool unused)
		{
			_Script = @unsafe ? data : data.ToArray();
		}

		public Script(IEnumerable<byte> data)
		{
			_Script = data.ToArray();
		}

		public Script(byte[] data, bool compressed)
		{
			if(!compressed)
				_Script = data.ToArray();
			else
			{
				var compressor = new ScriptCompressor();
				compressor.ReadWrite(data);
				_Script = compressor.GetScript()._Script;
			}
		}

		public int Length
		{
			get
			{
				return _Script.Length;
			}
		}




		public ScriptReader CreateReader(bool ignoreErrors = false)
		{
			return new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = ignoreErrors
			};
		}


		internal int FindAndDelete(OpcodeType op)
		{
			return FindAndDelete(new Op()
			{
				Code = op
			});
		}
		internal int FindAndDelete(Op op)
		{
			return op == null ? 0 : FindAndDelete(o => o.Code == op.Code && Utils.ArrayEqual(o.PushData, op.PushData));
		}

		internal int FindAndDelete(byte[] pushedData)
		{
			if(pushedData.Length == 0)
				return 0;
			var standardOp = Op.GetPushOp(pushedData);
			return FindAndDelete(op =>
							op.Code == standardOp.Code &&
							op.PushData != null && Utils.ArrayEqual(op.PushData, pushedData));
		}
		internal int FindAndDelete(Func<Op, bool> predicate)
		{
			var nFound = 0;
			var operations = new List<Op>();
			foreach(var op in ToOps())
			{
				var shouldDelete = predicate(op);
				if(!shouldDelete)
				{
					operations.Add(op);
				}
				else
					nFound++;
			}
			if(nFound == 0)
				return 0;
			_Script = new Script(operations)._Script;
			return nFound;
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}

		Script _paymentScript;
		public Script PaymentScript
		{
			get
			{
				return _paymentScript ?? (_paymentScript = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(Hash));
			}
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			var reader = new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = true
			};

			Op op;
			while((op = reader.Read()) != null)
			{
				builder.Append(" ");
				builder.Append(op);
			}

			return builder.ToString().Trim();
		}

		public bool IsPushOnly
		{
			get
			{
				foreach(var script in CreateReader(true).ToEnumerable())
				{
					if(script.PushData == null)
						return false;
				}
				return true;
			}
		}

		public bool HasCanonicalPushes
		{
			get
			{
				foreach(var op in CreateReader(true).ToEnumerable())
				{
					if(op.IncompleteData)
						return false;
					if(op.Code > OpcodeType.Op16)
						continue;
					if(op.Code < OpcodeType.OpPushdata1 && op.Code > OpcodeType.Op0 && (op.PushData.Length == 1 && op.PushData[0] <= 16))
						// Could have used an OP_n code, rather than a 1-byte push.
						return false;
					if(op.Code == OpcodeType.OpPushdata1 && op.PushData.Length < (byte)OpcodeType.OpPushdata1)
						// Could have used a normal n-byte push, rather than OP_PUSHDATA1.
						return false;
					if(op.Code == OpcodeType.OpPushdata2 && op.PushData.Length <= 0xFF)
						// Could have used an OP_PUSHDATA1.
						return false;
					if(op.Code == OpcodeType.OpPushdata4 && op.PushData.Length <= 0xFFFF)
						// Could have used an OP_PUSHDATA2.
						return false;
				}
				return true;
			}
		}


		//https://en.bitcoin.it/wiki/OP_CHECKSIG
		public Uint256 SignatureHash(Transaction txTo, int nIn, SigHash nHashType)
		{
			if(nIn >= txTo.Inputs.Count)
			{
				Utils.Log("ERROR: SignatureHash() : nIn=" + nIn + " out of range\n");
				return 1;
			}

			// Check for invalid use of SIGHASH_SINGLE
			if(nHashType == SigHash.Single)
			{
				if(nIn >= txTo.Outputs.Count)
				{
					Utils.Log("ERROR: SignatureHash() : nOut=" + nIn + " out of range\n");
					return 1;
				}
			}

			var scriptCopy = new Script(_Script);
			scriptCopy.FindAndDelete(OpcodeType.OpCodeseparator);

			var txCopy = new Transaction(txTo.ToBytes());
			//Set all TxIn script to empty string
			foreach(var txin in txCopy.Inputs)
			{
				txin.ScriptSig = new Script();
			}
			//Copy subscript into the txin script you are checking
			txCopy.Inputs[nIn].ScriptSig = scriptCopy;

			if(((int)nHashType & 31) == (int)SigHash.None)
			{
				//The output of txCopy is set to a vector of zero size.
				txCopy.Outputs.Clear();
				//All other inputs aside from the current input in txCopy have their nSequence index set to zero
				for(var i = 0 ; i < txCopy.Inputs.Count ; i++)
				{
					if(i == nIn)
						continue;
					txCopy.Inputs[i].Sequence = 0;
				}
			}

			if(((int)nHashType & 31) == (int)SigHash.Single)
			{
				//The output of txCopy is resized to the size of the current input index+1.
				var remainingOut = txCopy.Outputs.Take(nIn + 1).ToArray();
				txCopy.Outputs.Clear();
				txCopy.Outputs.AddRange(remainingOut);
				//All other txCopy outputs aside from the output that is the same as the current input index are set to a blank script and a value of (long) -1.
				for(var i = 0 ; i < txCopy.Outputs.Count ; i++)
				{
					if(i == nIn)
						continue;
					txCopy.Outputs[i] = new TxOut();
				}
				for(var i = 0 ; i < txCopy.Inputs.Count ; i++)
				{
					//All other txCopy inputs aside from the current input are set to have an nSequence index of zero.
					if(i == nIn)
						continue;
					txCopy.Inputs[i].Sequence = 0;
				}
			}

			if(((int)nHashType & (int)SigHash.AnyoneCanPay) != 0)
			{
				//The txCopy input vector is resized to a length of one.
				var script = txCopy.Inputs[nIn];
				txCopy.Inputs.Clear();
				txCopy.Inputs.Add(script);
				//The subScript (lead in by its length as a var-integer encoded!) is set as the first and only member of this vector.
				txCopy.Inputs[0].ScriptSig = scriptCopy;
			}


			//Serialize TxCopy, append 4 byte hashtypecode
			var ms = new MemoryStream();
			var bitcoinStream = new BitcoinStream(ms, true);
			txCopy.ReadWrite(bitcoinStream);
			bitcoinStream.ReadWrite((uint)nHashType);

			var hashed = ms.ToArray();
			return Hashes.Hash256(hashed);
		}

		public static Script operator +(Script a, int value)
		{
			return a + Utils.BigIntegerToBytes(value);
		}

		public static Script operator +(Script a, IEnumerable<byte> bytes)
		{
			if(a == null)
				return new Script(Op.GetPushOp(bytes.ToArray()));
			return a + Op.GetPushOp(bytes.ToArray());
		}
		public static Script operator +(Script a, Op op)
		{
			return a == null ? new Script(op) : new Script(a._Script.Concat(op.ToBytes()));
		}

		public static Script operator +(Script a, IEnumerable<Op> ops)
		{
			return a == null ? new Script(ops) : new Script(a._Script.Concat(new Script(ops)._Script));
		}

		public IEnumerable<Op> ToOps()
		{
			var reader = new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = true
			};
			return reader.ToEnumerable();
		}

		public uint GetSigOpCount(bool fAccurate)
		{
			uint n = 0;
			Op lastOpcode = null;
			foreach(var op in ToOps())
			{
				if(op.Code == OpcodeType.OpChecksig || op.Code == OpcodeType.OpChecksigverify)
					n++;
				else if(op.Code == OpcodeType.OpCheckmultisig || op.Code == OpcodeType.OpCheckmultisigverify)
				{
					if(fAccurate && lastOpcode != null && lastOpcode.Code >= OpcodeType.Op1 && lastOpcode.Code <= OpcodeType.Op16)
						n += (lastOpcode.PushData == null || lastOpcode.PushData.Length == 0) ? 0U : (uint)lastOpcode.PushData[0];
					else
						n += 20;
				}
				lastOpcode = op;
			}
			return n;
		}

		ScriptId _id;

		[Obsolete("Use Hash instead")]
		public ScriptId Id
		{
			get
			{
				return _id ?? (_id = new ScriptId(Hashes.Hash160(_Script)));
			}
		}

		public ScriptId Hash
		{
			get
			{
				return _id ?? (_id = new ScriptId(this));
			}
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			return new BitcoinScriptAddress(Hash, network);
		}

		public bool IsPayToScriptHash
		{
			get
			{
				return PayToScriptHashTemplate.Instance.CheckScriptPubKey(this);
			}
		}
		public uint GetSigOpCount(Script scriptSig)
		{
			if(!IsPayToScriptHash)
				return GetSigOpCount(true);
			// This is a pay-to-script-hash scriptPubKey;
			// get the last item that the scriptSig
			// pushes onto the stack:
			var validSig = new PayToScriptHashTemplate().CheckScriptSig(scriptSig, this);
			return !validSig ? 0 : new Script(scriptSig.ToOps().Last().PushData).GetSigOpCount(true);
			// ... and return its opcount:
		}

		public ScriptTemplate FindTemplate()
		{
			return StandardScripts.GetTemplateFromScriptPubKey(this);
		}

		/// <summary>
		/// Extract P2SH or P2PH address from scriptSig
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinAddress GetSignerAddress(Network network)
		{
			var sig = GetSigner();
			return sig == null ? null : BitcoinAddress.Create(sig, network);
		}

		/// <summary>
		/// Extract P2SH or P2PH id from scriptSig
		/// </summary>
		/// <returns></returns>
		public TxDestination GetSigner()
		{
			var pubKey = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(this);
			if(pubKey != null)
			{
				return pubKey.PublicKey.Hash;
			}
			var p2Sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(this);
			return p2Sh != null ? p2Sh.RedeemScript.Hash : null;
		}

		/// <summary>
		/// Extract P2SH or P2PH address from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinAddress GetDestinationAddress(Network network)
		{
			var dest = GetDestination();
			return dest == null ? null : BitcoinAddress.Create(dest, network);
		}

		/// <summary>
		/// Extract P2SH or P2PH id from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public TxDestination GetDestination()
		{
			var pubKeyHashParams = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if(pubKeyHashParams != null)
				return pubKeyHashParams;
			var scriptHashParams = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			return scriptHashParams;
		}

		/// <summary>
		/// Extract public keys if this script is a multi sig or pay to pub key scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public PubKey[] GetDestinationPublicKeys()
		{
			var result = new List<PubKey>();
			var single = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if(single != null)
			{
				result.Add(single);
			}
			else
			{
				var multiSig = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(this);
				if(multiSig != null)
				{
					result.AddRange(multiSig.PubKeys);
				}
			}
			return result.ToArray();
		}

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <returns></returns>
		[Obsolete("Use ToBytes instead")]
		public byte[] ToRawScript()
		{
			return ToBytes(false);
		}

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <returns></returns>
		public byte[] ToBytes()
		{
			return ToBytes(false);
		}

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <param name="unsafe">if false, returns a copy of the internal byte array</param>
		/// <returns></returns>
		[Obsolete("Use ToBytes instead")]
		public byte[] ToRawScript(bool @unsafe)
		{
			return @unsafe ? _Script : _Script.ToArray();
		}

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <param name="unsafe">if false, returns a copy of the internal byte array</param>
		/// <returns></returns>
		public byte[] ToBytes(bool @unsafe)
		{
			return @unsafe ? _Script : _Script.ToArray();
		}

		public byte[] ToCompressedBytes()
		{
			var compressor = new ScriptCompressor(this);
			return compressor.ToBytes();
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, ScriptVerify scriptVerify = ScriptVerify.Standard, SigHash sigHash = SigHash.Undefined)
		{
			ScriptError unused;
			return VerifyScript(scriptSig, scriptPubKey, tx, i, scriptVerify, sigHash, out unused);
		}
		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, out ScriptError error)
		{
			return VerifyScript(scriptSig, scriptPubKey, tx, i, ScriptVerify.Standard, SigHash.Undefined, out error);
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, ScriptVerify scriptVerify, SigHash sigHash, out ScriptError error)
		{
			var eval = new ScriptEvaluationContext
			{
				SigHash = sigHash,
				ScriptVerify = scriptVerify
			};
			var result = eval.VerifyScript(scriptSig, scriptPubKey, tx, i);
			error = eval.Error;
			return result;
		}

		public bool IsUnspendable
		{
			get
			{
				return _Script.Length > 0 && _Script[0] == (byte)OpcodeType.OpReturn;
			}
		}

		/// <summary>
		/// Create scriptPubKey from destination id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static Script CreateFromDestination(TxDestination id)
		{
			if(id is ScriptId)
				return PayToScriptHashTemplate.Instance.GenerateScriptPubKey((ScriptId)id);
			if(id is KeyId)
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey((KeyId)id);
			throw new NotSupportedException();
		}

		/// <summary>
		/// Create scriptPubKey from destination address
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static Script CreateFromDestinationAddress(BitcoinAddress address)
		{
			return CreateFromDestination(address.Hash);
		}

		public static bool IsNullOrEmpty(Script script)
		{
			return script == null || script._Script.Length == 0;
		}

		public override bool Equals(object obj)
		{
			var item = obj as Script;
			return item != null && Utils.ArrayEqual(item._Script, _Script);
		}
		public static bool operator ==(Script a, Script b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a._Script, b._Script);
		}

		public static bool operator !=(Script a, Script b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Encoders.Hex.EncodeData(_Script).GetHashCode();
		}

		public Script Clone()
		{
			return new Script(_Script);
		}

		public static Script CombineSignatures(Script scriptPubKey, Transaction transaction, int n, Script scriptSig1, Script scriptSig2)
		{
			if(scriptPubKey == null)
				scriptPubKey = new Script();
			var context = new ScriptEvaluationContext();
			context.ScriptVerify = ScriptVerify.StrictEnc;
			context.EvalScript(scriptSig1, transaction, n);

			var stack1 = context.Stack.Reverse().ToArray();
			context = new ScriptEvaluationContext();
			context.ScriptVerify = ScriptVerify.StrictEnc;
			context.EvalScript(scriptSig2, transaction, n);

			var stack2 = context.Stack.Reverse().ToArray();

			return CombineSignatures(scriptPubKey, transaction, n, stack1, stack2);
		}

		private static Script CombineSignatures(Script scriptPubKey, Transaction transaction, int n, byte[][] sigs1, byte[][] sigs2)
		{
			var template = StandardScripts.GetTemplateFromScriptPubKey(scriptPubKey);
			if(template == null || template is TxNullDataTemplate)
				return PushAll(Max(sigs1, sigs2));

			if(template is PayToPubkeyTemplate || template is PayToPubkeyHashTemplate)
				if(sigs1.Length == 0 || sigs1[0].Length == 0)
					return PushAll(sigs2);
				else
					return PushAll(sigs1);

			if(template is PayToScriptHashTemplate)
			{
				if(sigs1.Length == 0 || sigs1[sigs1.Length - 1].Length == 0)
					return PushAll(sigs2);

				if(sigs2.Length == 0 || sigs2[sigs2.Length - 1].Length == 0)
					return PushAll(sigs1);

				var redeemBytes = sigs1[sigs1.Length - 1];
				var redeem = new Script(redeemBytes);
				sigs1 = sigs1.Take(sigs1.Length - 1).ToArray();
				sigs2 = sigs2.Take(sigs2.Length - 1).ToArray();
				var result = CombineSignatures(redeem, transaction, n, sigs1, sigs2);
				result += Op.GetPushOp(redeemBytes);
				return result;
			}

			if(template is PayToMultiSigTemplate)
			{
				return CombineMultisig(scriptPubKey, transaction, n, sigs1, sigs2);
			}

			throw new NotSupportedException("An impossible thing happen !");
		}

		private static Script CombineMultisig(Script scriptPubKey, Transaction transaction, int n, byte[][] sigs1, byte[][] sigs2)
		{
			// Combine all the signatures we've got:
			var allsigs = new List<TransactionSignature>();
			foreach(var v in sigs1)
			{
				try
				{
					allsigs.Add(new TransactionSignature(v));
				}
				catch(FormatException)
				{
				}
			}


			foreach(var v in sigs2)
			{
				try
				{
					allsigs.Add(new TransactionSignature(v));
				}
				catch(FormatException)
				{
				}
			}

			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if(multiSigParams == null)
				throw new InvalidOperationException("The scriptPubKey is not a valid multi sig");

			var sigs = new Dictionary<PubKey, TransactionSignature>();

			foreach(var sig in allsigs)
			{
				foreach(var pubkey in multiSigParams.PubKeys)
				{
					if(sigs.ContainsKey(pubkey))
						continue; // Already got a sig for this pubkey

					var eval = new ScriptEvaluationContext();
					if(eval.CheckSig(sig.ToBytes(), pubkey.ToBytes(), scriptPubKey, transaction, n))
					{
						sigs.AddOrReplace(pubkey, sig);
					}
				}
			}


			// Now build a merged CScript:
			var nSigsHave = 0;
			var result = new Script(OpcodeType.Op0); // pop-one-too-many workaround
			foreach(var pubkey in multiSigParams.PubKeys)
			{
				if(sigs.ContainsKey(pubkey))
				{
					result += Op.GetPushOp(sigs[pubkey].ToBytes());
					nSigsHave++;
				}
				if(nSigsHave >= multiSigParams.SignatureCount)
					break;
			}

			// Fill any missing with OP_0:
			for(var i = nSigsHave ; i < multiSigParams.SignatureCount ; i++)
				result += OpcodeType.Op0;

			return result;
		}

		private static Script PushAll(byte[][] stack)
		{
			var s = new Script();
			foreach(var push in stack)
			{
				s += Op.GetPushOp(push);
			}
			return s;
		}

		private static byte[][] Max(byte[][] scriptSig1, byte[][] scriptSig2)
		{
			return scriptSig1.Length >= scriptSig2.Length ? scriptSig1 : scriptSig2;
		}
	}
}
