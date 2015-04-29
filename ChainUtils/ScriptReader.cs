using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ChainUtils.DataEncoders;
using ooo = ChainUtils.BouncyCastle.Math;

namespace ChainUtils
{
	public class Op
	{
		//Copied from satoshi's code
		public static string GetOpName(OpcodeType opcode)
		{
			switch(opcode)
			{
				// push value
				case OpcodeType.Op0:
					return "0";
				case OpcodeType.OpPushdata1:
					return "OP_PUSHDATA1";
				case OpcodeType.OpPushdata2:
					return "OP_PUSHDATA2";
				case OpcodeType.OpPushdata4:
					return "OP_PUSHDATA4";
				case OpcodeType.Op_1Negate:
					return "-1";
				case OpcodeType.OpReserved:
					return "OP_RESERVED";
				case OpcodeType.Op1:
					return "1";
				case OpcodeType.Op2:
					return "2";
				case OpcodeType.Op3:
					return "3";
				case OpcodeType.Op4:
					return "4";
				case OpcodeType.Op5:
					return "5";
				case OpcodeType.Op6:
					return "6";
				case OpcodeType.Op7:
					return "7";
				case OpcodeType.Op8:
					return "8";
				case OpcodeType.Op9:
					return "9";
				case OpcodeType.Op10:
					return "10";
				case OpcodeType.Op11:
					return "11";
				case OpcodeType.Op12:
					return "12";
				case OpcodeType.Op13:
					return "13";
				case OpcodeType.Op14:
					return "14";
				case OpcodeType.Op15:
					return "15";
				case OpcodeType.Op16:
					return "16";

				// control
				case OpcodeType.OpNop:
					return "OP_NOP";
				case OpcodeType.OpVer:
					return "OP_VER";
				case OpcodeType.OpIf:
					return "OP_IF";
				case OpcodeType.OpNotif:
					return "OP_NOTIF";
				case OpcodeType.OpVerif:
					return "OP_VERIF";
				case OpcodeType.OpVernotif:
					return "OP_VERNOTIF";
				case OpcodeType.OpElse:
					return "OP_ELSE";
				case OpcodeType.OpEndif:
					return "OP_ENDIF";
				case OpcodeType.OpVerify:
					return "OP_VERIFY";
				case OpcodeType.OpReturn:
					return "OP_RETURN";

				// stack ops
				case OpcodeType.OpToaltstack:
					return "OP_TOALTSTACK";
				case OpcodeType.OpFromaltstack:
					return "OP_FROMALTSTACK";
				case OpcodeType.Op_2Drop:
					return "OP_2DROP";
				case OpcodeType.Op_2Dup:
					return "OP_2DUP";
				case OpcodeType.Op_3Dup:
					return "OP_3DUP";
				case OpcodeType.Op_2Over:
					return "OP_2OVER";
				case OpcodeType.Op_2Rot:
					return "OP_2ROT";
				case OpcodeType.Op_2Swap:
					return "OP_2SWAP";
				case OpcodeType.OpIfdup:
					return "OP_IFDUP";
				case OpcodeType.OpDepth:
					return "OP_DEPTH";
				case OpcodeType.OpDrop:
					return "OP_DROP";
				case OpcodeType.OpDup:
					return "OP_DUP";
				case OpcodeType.OpNip:
					return "OP_NIP";
				case OpcodeType.OpOver:
					return "OP_OVER";
				case OpcodeType.OpPick:
					return "OP_PICK";
				case OpcodeType.OpRoll:
					return "OP_ROLL";
				case OpcodeType.OpRot:
					return "OP_ROT";
				case OpcodeType.OpSwap:
					return "OP_SWAP";
				case OpcodeType.OpTuck:
					return "OP_TUCK";

				// splice ops
				case OpcodeType.OpCat:
					return "OP_CAT";
				case OpcodeType.OpSubstr:
					return "OP_SUBSTR";
				case OpcodeType.OpLeft:
					return "OP_LEFT";
				case OpcodeType.OpRight:
					return "OP_RIGHT";
				case OpcodeType.OpSize:
					return "OP_SIZE";

				// bit logic
				case OpcodeType.OpInvert:
					return "OP_INVERT";
				case OpcodeType.OpAnd:
					return "OP_AND";
				case OpcodeType.OpOr:
					return "OP_OR";
				case OpcodeType.OpXor:
					return "OP_XOR";
				case OpcodeType.OpEqual:
					return "OP_EQUAL";
				case OpcodeType.OpEqualverify:
					return "OP_EQUALVERIFY";
				case OpcodeType.OpReserved1:
					return "OP_RESERVED1";
				case OpcodeType.OpReserved2:
					return "OP_RESERVED2";

				// numeric
				case OpcodeType.Op_1Add:
					return "OP_1ADD";
				case OpcodeType.Op_1Sub:
					return "OP_1SUB";
				case OpcodeType.Op_2Mul:
					return "OP_2MUL";
				case OpcodeType.Op_2Div:
					return "OP_2DIV";
				case OpcodeType.OpNegate:
					return "OP_NEGATE";
				case OpcodeType.OpAbs:
					return "OP_ABS";
				case OpcodeType.OpNot:
					return "OP_NOT";
				case OpcodeType.Op_0Notequal:
					return "OP_0NOTEQUAL";
				case OpcodeType.OpAdd:
					return "OP_ADD";
				case OpcodeType.OpSub:
					return "OP_SUB";
				case OpcodeType.OpMul:
					return "OP_MUL";
				case OpcodeType.OpDiv:
					return "OP_DIV";
				case OpcodeType.OpMod:
					return "OP_MOD";
				case OpcodeType.OpLshift:
					return "OP_LSHIFT";
				case OpcodeType.OpRshift:
					return "OP_RSHIFT";
				case OpcodeType.OpBooland:
					return "OP_BOOLAND";
				case OpcodeType.OpBoolor:
					return "OP_BOOLOR";
				case OpcodeType.OpNumequal:
					return "OP_NUMEQUAL";
				case OpcodeType.OpNumequalverify:
					return "OP_NUMEQUALVERIFY";
				case OpcodeType.OpNumnotequal:
					return "OP_NUMNOTEQUAL";
				case OpcodeType.OpLessthan:
					return "OP_LESSTHAN";
				case OpcodeType.OpGreaterthan:
					return "OP_GREATERTHAN";
				case OpcodeType.OpLessthanorequal:
					return "OP_LESSTHANOREQUAL";
				case OpcodeType.OpGreaterthanorequal:
					return "OP_GREATERTHANOREQUAL";
				case OpcodeType.OpMin:
					return "OP_MIN";
				case OpcodeType.OpMax:
					return "OP_MAX";
				case OpcodeType.OpWithin:
					return "OP_WITHIN";

				// crypto
				case OpcodeType.OpRipemd160:
					return "OP_RIPEMD160";
				case OpcodeType.OpSha1:
					return "OP_SHA1";
				case OpcodeType.OpSha256:
					return "OP_SHA256";
				case OpcodeType.OpHash160:
					return "OP_HASH160";
				case OpcodeType.OpHash256:
					return "OP_HASH256";
				case OpcodeType.OpCodeseparator:
					return "OP_CODESEPARATOR";
				case OpcodeType.OpChecksig:
					return "OP_CHECKSIG";
				case OpcodeType.OpChecksigverify:
					return "OP_CHECKSIGVERIFY";
				case OpcodeType.OpCheckmultisig:
					return "OP_CHECKMULTISIG";
				case OpcodeType.OpCheckmultisigverify:
					return "OP_CHECKMULTISIGVERIFY";

				// expanson
				case OpcodeType.OpNop1:
					return "OP_NOP1";
				case OpcodeType.OpNop2:
					return "OP_NOP2";
				case OpcodeType.OpNop3:
					return "OP_NOP3";
				case OpcodeType.OpNop4:
					return "OP_NOP4";
				case OpcodeType.OpNop5:
					return "OP_NOP5";
				case OpcodeType.OpNop6:
					return "OP_NOP6";
				case OpcodeType.OpNop7:
					return "OP_NOP7";
				case OpcodeType.OpNop8:
					return "OP_NOP8";
				case OpcodeType.OpNop9:
					return "OP_NOP9";
				case OpcodeType.OpNop10:
					return "OP_NOP10";



				// template matching params
				case OpcodeType.OpPubkeyhash:
					return "OP_PUBKEYHASH";
				case OpcodeType.OpPubkey:
					return "OP_PUBKEY";
				case OpcodeType.OpSmalldata:
					return "OP_SMALLDATA";

				case OpcodeType.OpInvalidopcode:
					return "OP_INVALIDOPCODE";
				default:
					return "OP_UNKNOWN";
			}
		}
		internal static bool IsPushCode(OpcodeType opcode)
		{
			return 0 <= opcode && opcode <= OpcodeType.Op16 && opcode != OpcodeType.OpReserved;
		}

		static Dictionary<string, OpcodeType> _opcodeByName;
		static Op()
		{
			_opcodeByName = new Dictionary<string, OpcodeType>();
			foreach(var code in Enum.GetValues(typeof(OpcodeType)).Cast<OpcodeType>().Distinct())
			{
				var name = GetOpName(code);
				if(name != "OP_UNKNOWN")
					_opcodeByName.Add(name, code);
			}
		}
		public static OpcodeType GetOpCode(string name)
		{
			OpcodeType code;
			if(_opcodeByName.TryGetValue(name, out code))
				return code;
			else
				return OpcodeType.OpInvalidopcode;
		}

#if !NOBIGINT
		public static Op GetPushOp(BigInteger data)
#else
		internal static Op GetPushOp(BigInteger data)
#endif
		{
			return GetPushOp(Utils.BigIntegerToBytes(data));
		}
		public static Op GetPushOp(byte[] data)
		{
			var op = new Op();
			op.PushData = data;
			if(data.Length == 0)
				op.Code = OpcodeType.Op0;
			else if(data.Length == 1 && (byte)1 <= data[0] && data[0] <= (byte)16)
				op.Code = (OpcodeType)(data[0] + (byte)OpcodeType.Op1 - 1);
			else if(data.Length == 1 && (byte)0x81 == data[0])
				op.Code = OpcodeType.Op_1Negate;
			else if(0x01 <= data.Length && data.Length <= 0x4b)
				op.Code = (OpcodeType)(byte)data.Length;
			else if(data.Length <= 0xFF)
				op.Code = OpcodeType.OpPushdata1;
#if !PORTABLE
			else if(data.LongLength <= 0xFFFF)
				op.Code = OpcodeType.OpPushdata2;
			else if(data.LongLength <= 0xFFFFFFFF)
				op.Code = OpcodeType.OpPushdata4;
#else
			else if(data.Length <= 0xFFFF)
				op.Code = OpcodeType.OP_PUSHDATA2;
#endif
			else
				throw new NotSupportedException("Data length should not be bigger than 0xFFFFFFFF");
			return op;
		}

		internal Op()
		{

		}
		string _name;
		public string Name
		{
			get
			{
				if(_name == null)
					_name = GetOpName(Code);
				return _name;
			}
		}
		public OpcodeType Code
		{
			get;
			set;
		}
		public byte[] PushData
		{
			get;
			set;
		}

		private void PushDataToStream(byte[] data, Stream result)
		{
			var bitStream = new BitcoinStream(result, true);

			if(Code == OpcodeType.Op0)
			{
				//OP_0 already pushed
				return;
			}

			if(OpcodeType.Op1 <= Code && Code <= OpcodeType.Op16)
			{
				//OP_1 to OP_16 already pushed
				return;
			}
			if(Code == OpcodeType.Op_1Negate)
			{
				//OP_1Negate already pushed
				return;
			}

			if(0x01 <= (byte)Code && (byte)Code <= 0x4b)
			{
				//Data length already pushed
			}
			else if(Code == OpcodeType.OpPushdata1)
			{
				bitStream.ReadWrite((byte)data.Length);
			}
			else if(Code == OpcodeType.OpPushdata2)
			{
				bitStream.ReadWrite((ushort)data.Length);
			}
			else if(Code == OpcodeType.OpPushdata4)
			{
				bitStream.ReadWrite((uint)data.Length);
			}
			else
				throw new NotSupportedException("Data length should not be bigger than 0xFFFFFFFF");
			result.Write(data, 0, data.Length);
		}
		internal static byte[] ReadData(Op op, Stream stream, bool ignoreWrongPush = false)
		{
			var opcode = op.Code;
			uint len = 0;
			var bitStream = new BitcoinStream(stream, false);
			if(opcode == 0)
				return new byte[0];

			if((byte)OpcodeType.Op1 <= (byte)opcode && (byte)opcode <= (byte)OpcodeType.Op16)
			{
				return new[] { (byte)(opcode - OpcodeType.Op1 + 1) };
			}

			if(opcode == OpcodeType.Op_1Negate)
			{
				return new byte[] { 0x81 };
			}

			try
			{
				if(0x01 <= (byte)opcode && (byte)opcode <= 0x4b)
					len = (uint)opcode;
				else if(opcode == OpcodeType.OpPushdata1)
					len = bitStream.ReadWrite((byte)0);
				else if(opcode == OpcodeType.OpPushdata2)
					len = bitStream.ReadWrite((ushort)0);
				else if(opcode == OpcodeType.OpPushdata4)
					len = bitStream.ReadWrite((uint)0);
				else
					throw new FormatException("Invalid opcode for pushing data : " + opcode);
			}
			catch(EndOfStreamException)
			{
				if(!ignoreWrongPush)
					throw new FormatException("Incomplete script");
				op.IncompleteData = true;
				return new byte[0];
			}

			if(stream.CanSeek && stream.Length - stream.Position < len)
			{
				len = (uint)(stream.Length - stream.Position);
				if(!ignoreWrongPush)
					throw new FormatException("Not enough bytes pushed with " + opcode.ToString() + " expected " + len + " but got " + len);
				op.IncompleteData = true;
			}
			var data = new byte[len];
			var readen = stream.Read(data, 0, data.Length);
			if(readen != data.Length && !ignoreWrongPush)
				throw new FormatException("Not enough bytes pushed with " + opcode.ToString() + " expected " + len + " but got " + readen);
			else if(readen != data.Length)
			{
				op.IncompleteData = true;
				Array.Resize(ref data, readen);
			}
			return data;
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public override string ToString()
		{
			if(PushData != null)
			{
				if(PushData.Length == 0)
					return "0";
				var result = Encoders.Hex.EncodeData(PushData);
				return result.Length == 2 && result[0] == '0' ? result.Substring(1) : result;
			}
			else if(Name == "OP_UNKNOWN")
			{
				return Name + "(" + string.Format("0x{0:x2}", (byte)Code) + ")";
			}
			else
			{
				return Name;
			}
		}

		public void WriteTo(Stream stream)
		{
			stream.WriteByte((byte)Code);
			if(PushData != null)
			{
				PushDataToStream(PushData, stream);
			}
		}

		static string _unknown = "OP_UNKNOWN(0x";
		internal static Op Read(TextReader textReader)
		{
			var ms = new MemoryStream();
			var opname = ReadWord(textReader);
			var opcode = GetOpCode(opname);

			if(
				(opcode == OpcodeType.OpInvalidopcode || IsPushCode(opcode))
				&& !opname.StartsWith(_unknown)
				&& opname != "OP_INVALIDOPCODE")
			{
				if(opcode == OpcodeType.Op0)
					return GetPushOp(new byte[0]);
				return GetPushOp(Encoders.Hex.DecodeData(opname.Length == 1 ? "0" + opname : opname));
			}
			else if(opname.StartsWith(_unknown))
			{
				try
				{
					if(opname.StartsWith(_unknown))
					{
						opcode = (OpcodeType)(Encoders.Hex.DecodeData(opname.Substring(_unknown.Length, 2))[0]);
					}
				}
				catch(Exception ex)
				{
					throw new FormatException("Invalid unknown opcode", ex);
				}
			}

			return new Op()
			{
				Code = opcode
			};
		}

		public static implicit operator Op(OpcodeType codeType)
		{
			if(!IsPushCode(codeType))
				return new Op()
				{
					Code = codeType,
				};
			else
			{
				if(OpcodeType.Op1 <= codeType && codeType <= OpcodeType.Op16)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new[] { (byte)((byte)codeType - (byte)OpcodeType.Op1 + 1) }
					};
				}
				else if(codeType == OpcodeType.Op0)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new byte[0]
					};
				}
				else if(codeType == OpcodeType.Op_1Negate)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new byte[] { 0x81 }
					};
				}
				else
				{
					throw new InvalidOperationException("Push OP without any data provided detected, Op.PushData instead");
				}
			}
		}

		private static string ReadWord(TextReader textReader)
		{
			var builder = new StringBuilder();
			int r;
			while((r = textReader.Read()) != -1)
			{
				var ch = (char)r;
				var isSpace = DataEncoder.IsSpace(ch);
				if(isSpace && builder.Length == 0)
					continue;
				if(isSpace && builder.Length != 0)
					break;
				builder.Append((char)r);
			}
			return builder.ToString();
		}

		public bool IncompleteData
		{
			get;
			set;
		}

		public bool IsSmallUInt
		{
			get
			{
				return Code == OpcodeType.Op0 ||
						OpcodeType.Op1 <= Code && Code <= OpcodeType.Op16;
			}
		}
		public bool IsSmallInt
		{
			get
			{
				return IsSmallUInt || Code == OpcodeType.Op_1Negate;
			}
		}
#if !NOBIGINT
		public BigInteger? GetValue()
#else
		internal BigInteger? GetValue()
#endif
		{
			if(PushData == null)
				return null;
			return Utils.BytesToBigInteger(PushData);
		}
	}
	public class ScriptReader
	{
		public bool IgnoreIncoherentPushData
		{
			get;
			set;
		}
		private readonly Stream _inner;
		public Stream Inner
		{
			get
			{
				return _inner;
			}
		}
		public ScriptReader(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException("stream");
			_inner = stream;
		}
		public ScriptReader(byte[] data)
			: this(new MemoryStream(data))
		{

		}


		public Op Read()
		{
			var b = Inner.ReadByte();
			if(b == -1)
				return null;
			var opcode = (OpcodeType)b;
			if(Op.IsPushCode(opcode))
			{
				var op = new Op();
				op.Code = opcode;
				op.PushData = Op.ReadData(op, Inner, IgnoreIncoherentPushData);
				if(op.IncompleteData == true)
					return null;
				return op;
			}
			return new Op()
			{
				Code = opcode
			};
		}



		public IEnumerable<Op> ToEnumerable()
		{
			Op code;
			while((code = Read()) != null)
			{
				yield return code;
			}
		}
	}
}
