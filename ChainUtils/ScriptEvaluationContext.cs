using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ChainUtils.Crypto;

namespace ChainUtils
{
	public enum ScriptError
	{
		Ok = 0,
		UnknownError,
		EvalFalse,
		OpReturn,

		/* Max sizes */
		ScriptSize,
		PushSize,
		OpCount,
		StackSize,
		SigCount,
		PubkeyCount,

		/* Failed verify operations */
		Verify,
		EqualVerify,
		CheckMultiSigVerify,
		CheckSigVerify,
		NumEqualVerify,

		/* Logical/Format/Canonical errors */
		BadOpCode,
		DisabledOpCode,
		InvalidStackOperation,
		InvalidAltStackOperation,
		UnbalancedConditional,

		/* BIP62 */
		SigHashType,
		SigDer,
		MinimalData,
		SigPushOnly,
		SigHighS,
		SigNullDummy,
		PubKeyType,
		CleanStack,

		/* softfork safeness */
		DiscourageUpgradableNops,
	}
	public class ScriptEvaluationContext
	{
		class CScriptNum
		{
			const long NMaxNumSize = 4;
			/**
			 * Numeric opcodes (OP_1ADD, etc) are restricted to operating on 4-byte integers.
			 * The semantics are subtle, though: operands must be in the range [-2^31 +1...2^31 -1],
			 * but results may overflow (and are valid as long as they are not used in a subsequent
			 * numeric operation). CScriptNum enforces those semantics by storing results as
			 * an int64 and allowing out-of-range values to be returned as a vector of bytes but
			 * throwing an exception if arithmetic is done or the result is interpreted as an integer.
			 */

			public CScriptNum(long n)
			{
				_mValue = n;
			}
			private long _mValue;

			public CScriptNum(byte[] vch, bool fRequireMinimal)
			{
				if(vch.Length > NMaxNumSize)
				{
					throw new ArgumentException("script number overflow", "vch");
				}
				if(fRequireMinimal && vch.Length > 0)
				{
					// Check that the number is encoded with the minimum possible
					// number of bytes.
					//
					// If the most-significant-byte - excluding the sign bit - is zero
					// then we're not minimal. Note how this test also rejects the
					// negative-zero encoding, 0x80.
					if((vch[vch.Length - 1] & 0x7f) == 0)
					{
						// One exception: if there's more than one byte and the most
						// significant bit of the second-most-significant-byte is set
						// it would conflict with the sign bit. An example of this case
						// is +-255, which encode to 0xff00 and 0xff80 respectively.
						// (big-endian).
						if(vch.Length <= 1 || (vch[vch.Length - 2] & 0x80) == 0)
						{
							throw new ArgumentException("non-minimally encoded script number", "vch");
						}
					}
				}
				_mValue = set_vch(vch);
			}

			public override int GetHashCode()
			{
				return Getint();
			}
			public override bool Equals(object obj)
			{
				if(obj == null || !(obj is CScriptNum))
					return false;
				var item = (CScriptNum)obj;
				return _mValue == item._mValue;
			}
			public static bool operator ==(CScriptNum num, long rhs)
			{
				return num._mValue == rhs;
			}
			public static bool operator !=(CScriptNum num, long rhs)
			{
				return num._mValue != rhs;
			}
			public static bool operator <=(CScriptNum num, long rhs)
			{
				return num._mValue <= rhs;
			}
			public static bool operator <(CScriptNum num, long rhs)
			{
				return num._mValue < rhs;
			}
			public static bool operator >=(CScriptNum num, long rhs)
			{
				return num._mValue >= rhs;
			}
			public static bool operator >(CScriptNum num, long rhs)
			{
				return num._mValue > rhs;
			}

			public static bool operator ==(CScriptNum a, CScriptNum b)
			{
				return a._mValue == b._mValue;
			}
			public static bool operator !=(CScriptNum a, CScriptNum b)
			{
				return a._mValue != b._mValue;
			}
			public static bool operator <=(CScriptNum a, CScriptNum b)
			{
				return a._mValue <= b._mValue;
			}
			public static bool operator <(CScriptNum a, CScriptNum b)
			{
				return a._mValue < b._mValue;
			}
			public static bool operator >=(CScriptNum a, CScriptNum b)
			{
				return a._mValue >= b._mValue;
			}
			public static bool operator >(CScriptNum a, CScriptNum b)
			{
				return a._mValue > b._mValue;
			}

			public static CScriptNum operator +(CScriptNum num, long rhs)
			{
				return new CScriptNum(num._mValue + rhs);
			}
			public static CScriptNum operator -(CScriptNum num, long rhs)
			{
				return new CScriptNum(num._mValue - rhs);
			}
			public static CScriptNum operator +(CScriptNum a, CScriptNum b)
			{
				return new CScriptNum(a._mValue + b._mValue);
			}
			public static CScriptNum operator -(CScriptNum a, CScriptNum b)
			{
				return new CScriptNum(a._mValue - b._mValue);
			}


			public static CScriptNum operator -(CScriptNum num)
			{
				Assert(num._mValue != Int64.MinValue);
				return new CScriptNum(-num._mValue);
			}

			private static void Assert(bool result)
			{
				if(!result)
					throw new InvalidOperationException("Assertion fail for CScriptNum");
			}

			public static implicit operator CScriptNum(long rhs)
			{
				return new CScriptNum(rhs);
			}




			public int Getint()
			{
				if(_mValue > int.MaxValue)
					return int.MaxValue;
				else if(_mValue < int.MinValue)
					return int.MinValue;
				return (int)_mValue;
			}

			public byte[] Getvch()
			{
				return Serialize(_mValue);
			}

			static byte[] Serialize(long value)
			{
				if(value == 0)
					return new byte[0];

				var result = new List<byte>();
				var neg = value < 0;
				var absvalue = neg ? -value : value;

				while(absvalue != 0)
				{
					result.Add((byte)(absvalue & 0xff));
					absvalue >>= 8;
				}

				//    - If the most significant byte is >= 0x80 and the value is positive, push a
				//    new zero-byte to make the significant byte < 0x80 again.

				//    - If the most significant byte is >= 0x80 and the value is negative, push a
				//    new 0x80 byte that will be popped off when converting to an integral.

				//    - If the most significant byte is < 0x80 and the value is negative, add
				//    0x80 to it, since it will be subtracted and interpreted as a negative when
				//    converting to an integral.

				if((result[result.Count - 1] & 0x80) != 0)
					result.Add((byte)(neg ? 0x80 : 0));
				else if(neg)
					result[result.Count - 1] |= 0x80;

				return result.ToArray();
			}

			static long set_vch(byte[] vch)
			{
				if(vch.Length == 0)
					return 0;

				long result = 0;
				for(var i = 0 ; i != vch.Length ; ++i)
					result |= ((long)(vch[i])) << 8 * i;

				// If the input vector's most significant byte is 0x80, remove it from
				// the result's msb and return a negative.
				if((vch[vch.Length - 1] & 0x80) != 0)
				{
					var temp = ~(0x80UL << (8 * (vch.Length - 1)));
					return -((long)((ulong)result & temp));
				}

				return result;
			}

		}
		Stack<byte[]> _stack = new Stack<byte[]>();
		public Stack<byte[]> Stack
		{
			get
			{
				return _stack;
			}
		}

		public ScriptEvaluationContext()
		{
			ScriptVerify = ScriptVerify.Standard;
			SigHash = SigHash.Undefined;
			Error = ScriptError.UnknownError;
		}
		public ScriptVerify ScriptVerify
		{
			get;
			set;
		}
		public SigHash SigHash
		{
			get;
			set;
		}

		public bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction txTo, int nIn)
		{
			SetError(ScriptError.UnknownError);
			if((ScriptVerify & ScriptVerify.SigPushOnly) != 0 && !scriptSig.IsPushOnly)
			{
				return SetError(ScriptError.SigPushOnly);
			}

			ScriptEvaluationContext evaluationCopy = null;

			if(!EvalScript(scriptSig, txTo, nIn))
				return false;
			if((ScriptVerify & ScriptVerify.P2Sh) != 0)
			{
				evaluationCopy = Clone();
			}
			if(!EvalScript(scriptPubKey, txTo, nIn))
				return false;

			if(Result == null || Result.Value == false)
				return SetError(ScriptError.EvalFalse);

			// Additional validation for spend-to-script-hash transactions:
			if(((ScriptVerify & ScriptVerify.P2Sh) != 0) && scriptPubKey.IsPayToScriptHash)
			{
				Load(evaluationCopy);
				evaluationCopy = this;
				if(!scriptSig.IsPushOnly)
					return SetError(ScriptError.SigPushOnly);

				// stackCopy cannot be empty here, because if it was the
				// P2SH  HASH <> EQUAL  scriptPubKey would be evaluated with
				// an empty stack and the EvalScript above would return false.
				if(evaluationCopy.Stack.Count == 0)
					throw new InvalidOperationException("stackCopy cannot be empty here");

				var redeem = new Script(evaluationCopy.Stack.Pop());

				if(!evaluationCopy.EvalScript(redeem, txTo, nIn))
					return false;

				if(evaluationCopy.Result == null)
					return SetError(ScriptError.EvalFalse);
				if(!evaluationCopy.Result.Value)
					return SetError(ScriptError.EvalFalse);
			}

			// The CLEANSTACK check is only performed after potential P2SH evaluation,
			// as the non-P2SH evaluation of a P2SH script will obviously not result in
			// a clean stack (the P2SH inputs remain).
			if((ScriptVerify & ScriptVerify.CleanStack) != 0)
			{
				// Disallow CLEANSTACK without P2SH, as otherwise a switch CLEANSTACK->P2SH+CLEANSTACK
				// would be possible, which is not a softfork (and P2SH should be one).
				if((ScriptVerify & ScriptVerify.P2Sh) == 0)
					throw new InvalidOperationException("ScriptVerify : CleanStack without P2SH is not allowed");
				if(Stack.Count != 1)
				{
					return SetError(ScriptError.CleanStack);
				}
			}


			return true;
		}


		static readonly byte[] VchFalse = new byte[] { 0 };
		static readonly byte[] VchZero = new byte[] { 0 };
		static readonly byte[] VchTrue = new byte[] { 1 };

		public bool EvalScript(Script s, Transaction txTo, int nIn)
		{
			var script = s.CreateReader();
			var pend = (int)script.Inner.Length;

			var pbegincodehash = 0;
			var vfExec = new Stack<bool>();
			var altstack = new Stack<byte[]>();
			SetError(ScriptError.UnknownError);
			Op opcode = null;
			if(s.Length > 10000)
				return SetError(ScriptError.ScriptSize);
			var nOpCount = 0;
			var fRequireMinimal = (ScriptVerify & ScriptVerify.MinimalData) != 0;

			try
			{
				while((opcode = script.Read()) != null)
				{
					var fExec = vfExec.All(o => o); //!count(vfExec.begin(), vfExec.end(), false);

					//
					// Read instruction
					//

					if(opcode.PushData != null && opcode.PushData.Length > 520)
						return SetError(ScriptError.PushSize);

					// Note how OP_RESERVED does not count towards the opcode limit.
					if(opcode.Code > OpcodeType.Op16 && ++nOpCount > 201)
						return SetError(ScriptError.OpCount);

					if(opcode.Code == OpcodeType.OpCat ||
						opcode.Code == OpcodeType.OpSubstr ||
						opcode.Code == OpcodeType.OpLeft ||
						opcode.Code == OpcodeType.OpRight ||
						opcode.Code == OpcodeType.OpInvert ||
						opcode.Code == OpcodeType.OpAnd ||
						opcode.Code == OpcodeType.OpOr ||
						opcode.Code == OpcodeType.OpXor ||
						opcode.Code == OpcodeType.Op_2Mul ||
						opcode.Code == OpcodeType.Op_2Div ||
						opcode.Code == OpcodeType.OpMul ||
						opcode.Code == OpcodeType.OpDiv ||
						opcode.Code == OpcodeType.OpMod ||
						opcode.Code == OpcodeType.OpLshift ||
						opcode.Code == OpcodeType.OpRshift)
						return SetError(ScriptError.DisabledOpCode); // Disabled opcodes.

					if(fExec && 0 <= (int)opcode.Code && (int)opcode.Code <= (int)OpcodeType.OpPushdata4)
					{
						if(fRequireMinimal && !CheckMinimalPush(opcode.PushData, opcode.Code))
						{
							return SetError(ScriptError.MinimalData);
						}
						_stack.Push(opcode.PushData);
					}
					//if(fExec && opcode.PushData != null)
					//	_Stack.Push(opcode.PushData);
					else if(fExec || (OpcodeType.OpIf <= opcode.Code && opcode.Code <= OpcodeType.OpEndif))
						switch(opcode.Code)
						{
							//
							// Push value
							//
							case OpcodeType.Op_1Negate:
							case OpcodeType.Op1:
							case OpcodeType.Op2:
							case OpcodeType.Op3:
							case OpcodeType.Op4:
							case OpcodeType.Op5:
							case OpcodeType.Op6:
							case OpcodeType.Op7:
							case OpcodeType.Op8:
							case OpcodeType.Op9:
							case OpcodeType.Op10:
							case OpcodeType.Op11:
							case OpcodeType.Op12:
							case OpcodeType.Op13:
							case OpcodeType.Op14:
							case OpcodeType.Op15:
							case OpcodeType.Op16:
								{
									// ( -- value)
									var bn = new CScriptNum((int)opcode.Code - (int)(OpcodeType.Op1 - 1));
									_stack.Push(bn.Getvch());
								}
								break;


							//
							// Control
							//
							case OpcodeType.OpNop:
								break;
							case OpcodeType.OpNop1:
							case OpcodeType.OpNop2:
							case OpcodeType.OpNop3:
							case OpcodeType.OpNop4:
							case OpcodeType.OpNop5:
							case OpcodeType.OpNop6:
							case OpcodeType.OpNop7:
							case OpcodeType.OpNop8:
							case OpcodeType.OpNop9:
							case OpcodeType.OpNop10:
								{
									if((ScriptVerify & ScriptVerify.DiscourageUpgradableNops) != 0)
										return SetError(ScriptError.DiscourageUpgradableNops);
								}
								break;

							case OpcodeType.OpIf:
							case OpcodeType.OpNotif:
								{
									// <expression> if [statements] [else [statements]] endif
									var fValue = false;
									if(fExec)
									{
										if(_stack.Count < 1)
											return SetError(ScriptError.UnbalancedConditional);
										var vch = Top(_stack, -1);
										fValue = CastToBool(vch);
										if(opcode.Code == OpcodeType.OpNotif)
											fValue = !fValue;
										_stack.Pop();
									}
									vfExec.Push(fValue);
								}
								break;

							case OpcodeType.OpElse:
								{
									if(vfExec.Count == 0)
										return SetError(ScriptError.UnbalancedConditional);
									var v = vfExec.Pop();
									vfExec.Push(!v);
									//vfExec.Peek() = !vfExec.Peek();
								}
								break;

							case OpcodeType.OpEndif:
								{
									if(vfExec.Count == 0)
										return SetError(ScriptError.UnbalancedConditional);
									vfExec.Pop();
								}
								break;

							case OpcodeType.OpVerify:
								{
									// (true -- ) or
									// (false -- false) and return
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var fValue = CastToBool(Top(_stack, -1));
									if(fValue)
										_stack.Pop();
									else
										return SetError(ScriptError.Verify);
								}
								break;

							case OpcodeType.OpReturn:
								{
									return SetError(ScriptError.OpReturn);
								}


							//
							// Stack ops
							//
							case OpcodeType.OpToaltstack:
								{
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidAltStackOperation);
									altstack.Push(Top(_stack, -1));
									_stack.Pop();
								}
								break;

							case OpcodeType.OpFromaltstack:
								{
									if(altstack.Count < 1)
										return SetError(ScriptError.InvalidAltStackOperation);
									_stack.Push(Top(altstack, -1));
									altstack.Pop();
								}
								break;

							case OpcodeType.Op_2Drop:
								{
									// (x1 x2 -- )
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									_stack.Pop();
									_stack.Pop();
								}
								break;

							case OpcodeType.Op_2Dup:
								{
									// (x1 x2 -- x1 x2 x1 x2)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var vch1 = Top(_stack, -2);
									var vch2 = Top(_stack, -1);
									_stack.Push(vch1);
									_stack.Push(vch2);
								}
								break;

							case OpcodeType.Op_3Dup:
								{
									// (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
									if(_stack.Count < 3)
										return SetError(ScriptError.InvalidStackOperation);
									var vch1 = Top(_stack, -3);
									var vch2 = Top(_stack, -2);
									var vch3 = Top(_stack, -1);
									_stack.Push(vch1);
									_stack.Push(vch2);
									_stack.Push(vch3);
								}
								break;

							case OpcodeType.Op_2Over:
								{
									// (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
									if(_stack.Count < 4)
										return SetError(ScriptError.InvalidStackOperation);
									var vch1 = Top(_stack, -4);
									var vch2 = Top(_stack, -3);
									_stack.Push(vch1);
									_stack.Push(vch2);
								}
								break;

							case OpcodeType.Op_2Rot:
								{
									// (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
									if(_stack.Count < 6)
										return SetError(ScriptError.InvalidStackOperation);
									var vch1 = Top(_stack, -6);
									var vch2 = Top(_stack, -5);
									erase(ref _stack, _stack.Count - 6, _stack.Count - 4);
									_stack.Push(vch1);
									_stack.Push(vch2);
								}
								break;

							case OpcodeType.Op_2Swap:
								{
									// (x1 x2 x3 x4 -- x3 x4 x1 x2)
									if(_stack.Count < 4)
										return SetError(ScriptError.InvalidStackOperation);
									Swap(ref _stack, -4, -2);
									Swap(ref _stack, -3, -1);
								}
								break;

							case OpcodeType.OpIfdup:
								{
									// (x - 0 | x x)
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -1);
									if(CastToBool(vch))
										_stack.Push(vch);
								}
								break;

							case OpcodeType.OpDepth:
								{
									// -- stacksize
									var bn = new CScriptNum(_stack.Count);
									_stack.Push(bn.Getvch());
								}
								break;

							case OpcodeType.OpDrop:
								{
									// (x -- )
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									_stack.Pop();
								}
								break;

							case OpcodeType.OpDup:
								{
									// (x -- x x)
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -1);
									_stack.Push(vch);
								}
								break;

							case OpcodeType.OpNip:
								{
									// (x1 x2 -- x2)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									erase(ref _stack, _stack.Count - 2);
								}
								break;

							case OpcodeType.OpOver:
								{
									// (x1 x2 -- x1 x2 x1)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -2);
									_stack.Push(vch);
								}
								break;

							case OpcodeType.OpPick:
							case OpcodeType.OpRoll:
								{
									// (xn ... x2 x1 x0 n - xn ... x2 x1 x0 xn)
									// (xn ... x2 x1 x0 n - ... x2 x1 x0 xn)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var n = new CScriptNum(Top(_stack, -1), fRequireMinimal).Getint();
									_stack.Pop();
									if(n < 0 || n >= _stack.Count)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -n - 1);
									if(opcode.Code == OpcodeType.OpRoll)
										erase(ref _stack, _stack.Count - n - 1);
									_stack.Push(vch);
								}
								break;

							case OpcodeType.OpRot:
								{
									// (x1 x2 x3 -- x2 x3 x1)
									//  x2 x1 x3  after first swap
									//  x2 x3 x1  after second swap
									if(_stack.Count < 3)
										return SetError(ScriptError.InvalidStackOperation);
									Swap(ref _stack, -3, -2);
									Swap(ref _stack, -2, -1);
								}
								break;

							case OpcodeType.OpSwap:
								{
									// (x1 x2 -- x2 x1)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									Swap(ref _stack, -2, -1);
								}
								break;

							case OpcodeType.OpTuck:
								{
									// (x1 x2 -- x2 x1 x2)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -1);
									Insert(ref _stack, _stack.Count - 2, vch);
								}
								break;


							case OpcodeType.OpSize:
								{
									// (in -- in size)
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var bn = new CScriptNum(Top(_stack, -1).Length);
									_stack.Push(bn.Getvch());
								}
								break;


							//
							// Bitwise logic
							//
							case OpcodeType.OpEqual:
							case OpcodeType.OpEqualverify:
								//case OpcodeType.OP_NOTEQUAL: // use OpcodeType.OP_NUMNOTEQUAL
								{
									// (x1 x2 - bool)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var vch1 = Top(_stack, -2);
									var vch2 = Top(_stack, -1);
									var fEqual = Utils.ArrayEqual(vch1, vch2);
									// OpcodeType.OP_NOTEQUAL is disabled because it would be too easy to say
									// something like n != 1 and have some wiseguy pass in 1 with extra
									// zero bytes after it (numerically, 0x01 == 0x0001 == 0x000001)
									//if (opcode == OpcodeType.OP_NOTEQUAL)
									//    fEqual = !fEqual;
									_stack.Pop();
									_stack.Pop();
									_stack.Push(fEqual ? VchTrue : VchFalse);
									if(opcode.Code == OpcodeType.OpEqualverify)
									{
										if(fEqual)
											_stack.Pop();
										else
											return SetError(ScriptError.EqualVerify);
									}
								}
								break;


							//
							// Numeric
							//
							case OpcodeType.Op_1Add:
							case OpcodeType.Op_1Sub:
							case OpcodeType.OpNegate:
							case OpcodeType.OpAbs:
							case OpcodeType.OpNot:
							case OpcodeType.Op_0Notequal:
								{
									// (in -- out)
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var bn = new CScriptNum(Top(_stack, -1), fRequireMinimal);
									switch(opcode.Code)
									{
										case OpcodeType.Op_1Add:
											bn += 1;
											break;
										case OpcodeType.Op_1Sub:
											bn -= 1;
											break;
										case OpcodeType.OpNegate:
											bn = -bn;
											break;
										case OpcodeType.OpAbs:
											if(bn < 0)
												bn = -bn;
											break;
										case OpcodeType.OpNot:
											bn = bn == 0 ? 1 : 0;
											break;
										case OpcodeType.Op_0Notequal:
											bn = bn != 0 ? 1 : 0;
											break;
										default:
											throw new NotSupportedException("invalid opcode");
									}
									_stack.Pop();
									_stack.Push(bn.Getvch());
								}
								break;

							case OpcodeType.OpAdd:
							case OpcodeType.OpSub:
							case OpcodeType.OpBooland:
							case OpcodeType.OpBoolor:
							case OpcodeType.OpNumequal:
							case OpcodeType.OpNumequalverify:
							case OpcodeType.OpNumnotequal:
							case OpcodeType.OpLessthan:
							case OpcodeType.OpGreaterthan:
							case OpcodeType.OpLessthanorequal:
							case OpcodeType.OpGreaterthanorequal:
							case OpcodeType.OpMin:
							case OpcodeType.OpMax:
								{
									// (x1 x2 -- out)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);
									var bn1 = new CScriptNum(Top(_stack, -2), fRequireMinimal);
									var bn2 = new CScriptNum(Top(_stack, -1), fRequireMinimal);
									var bn = new CScriptNum(0);
									switch(opcode.Code)
									{
										case OpcodeType.OpAdd:
											bn = bn1 + bn2;
											break;

										case OpcodeType.OpSub:
											bn = bn1 - bn2;
											break;

										case OpcodeType.OpBooland:
											bn = bn1 != 0 && bn2 != 0 ? 1 : 0;
											break;
										case OpcodeType.OpBoolor:
											bn = bn1 != 0 || bn2 != 0 ? 1 : 0;
											break;
										case OpcodeType.OpNumequal:
											bn = (bn1 == bn2) ? 1 : 0;
											break;
										case OpcodeType.OpNumequalverify:
											bn = (bn1 == bn2) ? 1 : 0;
											break;
										case OpcodeType.OpNumnotequal:
											bn = (bn1 != bn2) ? 1 : 0;
											break;
										case OpcodeType.OpLessthan:
											bn = (bn1 < bn2) ? 1 : 0;
											break;
										case OpcodeType.OpGreaterthan:
											bn = (bn1 > bn2) ? 1 : 0;
											break;
										case OpcodeType.OpLessthanorequal:
											bn = (bn1 <= bn2) ? 1 : 0;
											break;
										case OpcodeType.OpGreaterthanorequal:
											bn = (bn1 >= bn2) ? 1 : 0;
											break;
										case OpcodeType.OpMin:
											bn = (bn1 < bn2 ? bn1 : bn2);
											break;
										case OpcodeType.OpMax:
											bn = (bn1 > bn2 ? bn1 : bn2);
											break;
										default:
											throw new NotSupportedException("invalid opcode");
									}
									_stack.Pop();
									_stack.Pop();
									_stack.Push(bn.Getvch());

									if(opcode.Code == OpcodeType.OpNumequalverify)
									{
										if(CastToBool(Top(_stack, -1)))
											_stack.Pop();
										else
											return SetError(ScriptError.NumEqualVerify);
									}
								}
								break;

							case OpcodeType.OpWithin:
								{
									// (x min max -- out)
									if(_stack.Count < 3)
										return SetError(ScriptError.InvalidStackOperation);
									var bn1 = new CScriptNum(Top(_stack, -3), fRequireMinimal);
									var bn2 = new CScriptNum(Top(_stack, -2), fRequireMinimal);
									var bn3 = new CScriptNum(Top(_stack, -1), fRequireMinimal);
									var fValue = (bn2 <= bn1 && bn1 < bn3);
									_stack.Pop();
									_stack.Pop();
									_stack.Pop();
									_stack.Push(fValue ? VchTrue : VchFalse);
								}
								break;


							//
							// Crypto
							//
							case OpcodeType.OpRipemd160:
							case OpcodeType.OpSha1:
							case OpcodeType.OpSha256:
							case OpcodeType.OpHash160:
							case OpcodeType.OpHash256:
								{
									// (in -- hash)
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									var vch = Top(_stack, -1);
									byte[] vchHash = null;//((opcode == OpcodeType.OP_RIPEMD160 || opcode == OpcodeType.OP_SHA1 || opcode == OpcodeType.OP_HASH160) ? 20 : 32);
									if(opcode.Code == OpcodeType.OpRipemd160)
										vchHash = Hashes.RIPEMD160(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OpSha1)
										vchHash = Hashes.Sha1(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OpSha256)
										vchHash = Hashes.SHA256(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OpHash160)
										vchHash = Hashes.Hash160(vch, vch.Length).ToBytes();
									else if(opcode.Code == OpcodeType.OpHash256)
										vchHash = Hashes.Hash256(vch, vch.Length).ToBytes();
									_stack.Pop();
									_stack.Push(vchHash);
								}
								break;

							case OpcodeType.OpCodeseparator:
								{
									// Hash starts after the code separator
									pbegincodehash = (int)script.Inner.Position;
								}
								break;

							case OpcodeType.OpChecksig:
							case OpcodeType.OpChecksigverify:
								{
									// (sig pubkey -- bool)
									if(_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									var vchSig = Top(_stack, -2);
									var vchPubKey = Top(_stack, -1);

									////// debug print
									//PrintHex(vchSig.begin(), vchSig.end(), "sig: %s\n");
									//PrintHex(vchPubKey.begin(), vchPubKey.end(), "pubkey: %s\n");

									// Subset of script starting at the most recent codeseparator
									var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
									// Drop the signature, since there's no way for a signature to sign itself
									scriptCode.FindAndDelete(vchSig);

									if(!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey))
									{
										//serror is set
										return false;
									}

									var fSuccess = CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

									_stack.Pop();
									_stack.Pop();
									_stack.Push(fSuccess ? VchTrue : VchFalse);
									if(opcode.Code == OpcodeType.OpChecksigverify)
									{
										if(fSuccess)
											_stack.Pop();
										else
											return SetError(ScriptError.CheckSigVerify);
									}
								}
								break;

							case OpcodeType.OpCheckmultisig:
							case OpcodeType.OpCheckmultisigverify:
								{
									// ([sig ...] num_of_signatures [pubkey ...] num_of_pubkeys -- bool)

									var i = 1;
									if((int)_stack.Count < i)
										return SetError(ScriptError.InvalidStackOperation);

									var nKeysCount = new CScriptNum(Top(_stack, -i), fRequireMinimal).Getint();
									if(nKeysCount < 0 || nKeysCount > 20)
										return SetError(ScriptError.PubkeyCount);
									nOpCount += nKeysCount;
									if(nOpCount > 201)
										return SetError(ScriptError.OpCount);
									var ikey = ++i;
									i += nKeysCount;
									if((int)_stack.Count < i)
										return SetError(ScriptError.InvalidStackOperation);

									var nSigsCount = new CScriptNum(Top(_stack, -i), fRequireMinimal).Getint();
									if(nSigsCount < 0 || nSigsCount > nKeysCount)
										return SetError(ScriptError.SigCount);
									var isig = ++i;
									i += nSigsCount;
									if((int)_stack.Count < i)
										return SetError(ScriptError.InvalidStackOperation);

									// Subset of script starting at the most recent codeseparator
									var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
									// Drop the signatures, since there's no way for a signature to sign itself
									for(var k = 0 ; k < nSigsCount ; k++)
									{
										var vchSig = Top(_stack, -isig - k);
										scriptCode.FindAndDelete(vchSig);
									}

									var fSuccess = true;
									while(fSuccess && nSigsCount > 0)
									{
										var vchSig = Top(_stack, -isig);
										var vchPubKey = Top(_stack, -ikey);


										// Note how this makes the exact order of pubkey/signature evaluation
										// distinguishable by CHECKMULTISIG NOT if the STRICTENC flag is set.
										// See the script_(in)valid tests for details.
										if(!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey))
										{
											// serror is set
											return false;
										}

										var fOk = CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

										if(fOk)
										{
											isig++;
											nSigsCount--;
										}
										ikey++;
										nKeysCount--;

										// If there are more signatures left than keys left,
										// then too many signatures have failed
										if(nSigsCount > nKeysCount)
											fSuccess = false;
									}

									while(i-- > 1)
										_stack.Pop();

									// A bug causes CHECKMULTISIG to consume one extra argument
									// whose contents were not checked in any way.
									//
									// Unfortunately this is a potential source of mutability,
									// so optionally verify it is exactly equal to zero prior
									// to removing it from the stack.
									if(_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);
									if(((ScriptVerify & ScriptVerify.NullDummy) != 0) && Top(_stack, -1).Length != 0)
										return SetError(ScriptError.SigNullDummy);
									_stack.Pop();

									_stack.Push(fSuccess ? VchTrue : VchFalse);

									if(opcode.Code == OpcodeType.OpCheckmultisigverify)
									{
										if(fSuccess)
											_stack.Pop();
										else
											return SetError(ScriptError.CheckMultiSigVerify);
									}
								}
								break;

							default:
								return SetError(ScriptError.BadOpCode);
						}

					// Size limits
					if(_stack.Count + altstack.Count > 1000)
						return SetError(ScriptError.StackSize);

				}
			}
			catch(Exception ex)
			{
				ThrownException = ex;
				return SetError(ScriptError.UnknownError);
			}


			if(vfExec.Count != 0)
				return SetError(ScriptError.UnbalancedConditional);

			return SetSuccess(ScriptError.Ok);
		}

		private bool SetSuccess(ScriptError scriptError)
		{
			Error = ScriptError.Ok;
			return true;
		}

		private bool IsCompressedOrUncompressedPubKey(byte[] vchPubKey)
		{
			if(vchPubKey.Length < 33)
			{
				//  Non-canonical public key: too short
				return false;
			}
			if(vchPubKey[0] == 0x04)
			{
				if(vchPubKey.Length != 65)
				{
					//  Non-canonical public key: invalid length for uncompressed key
					return false;
				}
			}
			else if(vchPubKey[0] == 0x02 || vchPubKey[0] == 0x03)
			{
				if(vchPubKey.Length != 33)
				{
					//  Non-canonical public key: invalid length for compressed key
					return false;
				}
			}
			else
			{
				//  Non-canonical public key: neither compressed nor uncompressed
				return false;
			}
			return true;
		}

		private bool CheckSignatureEncoding(byte[] vchSig)
		{
			// Empty signature. Not strictly DER encoded, but allowed to provide a
			// compact way to provide an invalid signature for use with CHECK(MULTI)SIG
			if(vchSig.Length == 0)
			{
				return true;
			}
			if((ScriptVerify & (ScriptVerify.DerSig | ScriptVerify.LowS | ScriptVerify.StrictEnc)) != 0 && !IsDerSignature(vchSig))
			{
				return SetError(ScriptError.SigDer);
			}
			else if((ScriptVerify & ScriptVerify.LowS) != 0 && !IsLowDerSignature(vchSig))
			{
				// serror is set
				return false;
			}
			else if((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsDefinedHashtypeSignature(vchSig))
			{
				return SetError(ScriptError.SigHashType);
			}
			return true;
		}

		private bool CheckPubKeyEncoding(byte[] vchPubKey)
		{
			if((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsCompressedOrUncompressedPubKey(vchPubKey))
			{
				return SetError(ScriptError.PubKeyType);
			}
			return true;
		}

		private bool IsDefinedHashtypeSignature(byte[] vchSig)
		{
			if(vchSig.Length == 0)
			{
				return false;
			}

			var temp = ~(SigHash.AnyoneCanPay);
			var nHashType = (byte)(vchSig[vchSig.Length - 1] & (byte)temp);
			if(nHashType < (byte)SigHash.All || nHashType > (byte)SigHash.Single)
				return false;

			return true;
		}

		private bool IsLowDerSignature(byte[] vchSig)
		{
			if(!IsDerSignature(vchSig))
			{
				return SetError(ScriptError.SigDer);
			}
			int nLenR = vchSig[3];
			int nLenS = vchSig[5 + nLenR];
			var s = 6 + nLenR;
			// If the S value is above the order of the curve divided by two, its
			// complement modulo the order could have been used instead, which is
			// one byte shorter when encoded correctly.
			if(!CheckSignatureElement(vchSig, s, nLenS, true))
				return SetError(ScriptError.SigHighS);

			return true;
		}

		public ScriptError Error
		{
			get;
			set;
		}

		private bool SetError(ScriptError scriptError)
		{
			Error = scriptError;
			return false;
		}

		static byte[] _vchMaxModOrder = new byte[]{
0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
 0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x40
};

		static byte[] _vchMaxModHalfOrder = new byte[]{
 0x7F,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0x5D,0x57,0x6E,0x73,0x57,0xA4,0x50,0x1D,
0xDF,0xE9,0x2F,0x46,0x68,0x1B,0x20,0xA0
};

		private bool CheckSignatureElement(byte[] vchSig, int i, int len, bool half)
		{
			return vchSig != null
						&&
						 CompareBigEndian(vchSig, i, len, VchZero, 0) > 0 &&
						 CompareBigEndian(vchSig, i, len, half ? _vchMaxModHalfOrder : _vchMaxModOrder, 32) <= 0;
		}

		private int CompareBigEndian(byte[] c1, int ic1, int c1Len, byte[] c2, int c2Len)
		{
			var ic2 = 0;
			while(c1Len > c2Len)
			{
				if(c1[ic1] != 0)
					return 1;
				ic1++;
				c1Len--;
			}
			while(c2Len > c1Len)
			{
				if(c2[ic2] != 0)
					return -1;
				ic2++;
				c2Len--;
			}
			while(c1Len > 0)
			{
				if(c1[ic1] > c2[ic2])
					return 1;
				if(c2[ic2] > c1[ic1])
					return -1;
				ic1++;
				ic2++;
				c1Len--;
			}
			return 0;
		}



		private bool IsDerSignature(byte[] vchSig)
		{
			if(vchSig.Length < 9)
			{
				//  Non-canonical signature: too short
				return false;
			}
			if(vchSig.Length > 73)
			{
				// Non-canonical signature: too long
				return false;
			}
			if(vchSig[0] != 0x30)
			{
				//  Non-canonical signature: wrong type
				return false;
			}
			if(vchSig[1] != vchSig.Length - 3)
			{
				//  Non-canonical signature: wrong length marker
				return false;
			}
			uint nLenR = vchSig[3];
			if(5 + nLenR >= vchSig.Length)
			{
				//  Non-canonical signature: S length misplaced
				return false;
			}
			uint nLenS = vchSig[5 + nLenR];
			if((ulong)(nLenR + nLenS + 7) != (ulong)vchSig.Length)
			{
				//  Non-canonical signature: R+S length mismatch
				return false;
			}

			var r = 4;
			if(vchSig[r + -2] != 0x02)
			{
				//  Non-canonical signature: R value type mismatch
				return false;
			}
			if(nLenR == 0)
			{
				//  Non-canonical signature: R length is zero
				return false;
			}
			if((vchSig[r] & 0x80) != 0)
			{
				//  Non-canonical signature: R value negative
				return false;
			}
			if(nLenR > 1 && (vchSig[r] == 0x00) && !((vchSig[r + 1] & 0x80) != 0))
			{
				//  Non-canonical signature: R value excessively padded
				return false;
			}

			var s = 6 + nLenR;
			if(vchSig[s + -2] != 0x02)
			{
				//  Non-canonical signature: S value type mismatch
				return false;
			}
			if(nLenS == 0)
			{
				//  Non-canonical signature: S length is zero
				return false;
			}
			if((vchSig[s] & 0x80) != 0)
			{
				//  Non-canonical signature: S value negative
				return false;
			}
			if(nLenS > 1 && (vchSig[s] == 0x00) && !((vchSig[s + 1] & 0x80) != 0))
			{
				//  Non-canonical signature: S value excessively padded
				return false;
			}
			return true;
		}

		private void Insert(ref Stack<byte[]> stack, int i, byte[] vch)
		{
			var newStack = new Stack<byte[]>();
			var count = stack.Count;
			stack = new Stack<byte[]>(stack); //Reverse the stack
			for(var y = 0 ; y < count + 1 ; y++)
			{
				if(y == i)
					newStack.Push(vch);
				else
					newStack.Push(stack.Pop());
			}
			stack = newStack;
		}

		bool CheckMinimalPush(byte[] data, OpcodeType opcode)
		{
			if(data.Length == 0)
			{
				// Could have used OP_0.
				return opcode == OpcodeType.Op0;
			}
			else if(data.Length == 1 && data[0] >= 1 && data[0] <= 16)
			{
				// Could have used OP_1 .. OP_16.
				return (int)opcode == ((int)OpcodeType.Op1) + (data[0] - 1);
			}
			else if(data.Length == 1 && data[0] == 0x81)
			{
				// Could have used OP_1NEGATE.
				return opcode == OpcodeType.Op_1Negate;
			}
			else if(data.Length <= 75)
			{
				// Could have used a direct push (opcode indicating number of bytes pushed + those bytes).
				return (int)opcode == data.Length;
			}
			else if(data.Length <= 255)
			{
				// Could have used OP_PUSHDATA.
				return opcode == OpcodeType.OpPushdata1;
			}
			else if(data.Length <= 65535)
			{
				// Could have used OP_PUSHDATA2.
				return opcode == OpcodeType.OpPushdata2;
			}
			return true;
		}

		private BigInteger CastToBigNum(bool v)
		{
			return new BigInteger(v ? 1 : 0);
		}

		private static bool CastToBool(byte[] vch)
		{
			for(uint i = 0 ; i < vch.Length ; i++)
			{
				if(vch[i] != 0)
				{

					if(i == vch.Length - 1 && vch[i] == 0x80)
						return false;
					return true;
				}
			}
			return false;
		}

		static void Swap<T>(ref Stack<T> stack, int i, int i2)
		{
			var values = stack.ToArray();
			Array.Reverse(values);
			var temp = values[values.Length + i];
			values[values.Length + i] = values[values.Length + i2];
			values[values.Length + i2] = temp;
			stack = new Stack<T>(values);
		}

		private void erase(ref Stack<byte[]> stack, int from, int to)
		{
			var values = stack.ToArray();
			Array.Reverse(values);
			stack = new Stack<byte[]>();
			for(var i = 0 ; i < values.Length ; i++)
			{
				if(from <= i && i < to)
					continue;
				stack.Push(values[i]);
			}
		}
		private void erase(ref Stack<byte[]> stack, int i)
		{
			erase(ref stack, i, i + 1);
		}
		static T Top<T>(Stack<T> stack, int i)
		{
			var array = stack.ToArray();
			Array.Reverse(array);
			return array[stack.Count + i];
			//stacktop(i)  (altstack.at(altstack.size()+(i)))
		}


		public bool CheckSig(TransactionSignature signature, PubKey pubKey, Script scriptPubKey, IndexedTxIn txIn)
		{
			return CheckSig(signature, pubKey, scriptPubKey, txIn.Transaction, txIn.N);
		}
		public bool CheckSig(TransactionSignature signature, PubKey pubKey, Script scriptPubKey, Transaction txTo, uint nIn)
		{
			return CheckSig(signature.ToBytes(), pubKey.ToBytes(), scriptPubKey, txTo, (int)nIn);
		}

		public bool CheckSig(byte[] vchSig, byte[] vchPubKey, Script scriptCode, Transaction txTo, int nIn)
		{
			PubKey pubkey = null;
			try
			{
				pubkey = new PubKey(vchPubKey);
			}
			catch(Exception)
			{
				return false;
			}


			// Hash type is one byte tacked on to the end of the signature
			if(vchSig.Length == 0)
				return false;

			TransactionSignature scriptSig = null;
			try
			{
				scriptSig = new TransactionSignature(vchSig);
			}
			catch(Exception)
			{
				if((ScriptVerify.DerSig & ScriptVerify) != 0)
					throw;
				return false;
			}

			if(!IsAllowedSignature(scriptSig.SigHash))
				return false;

			var sighash = scriptCode.SignatureHash(txTo, nIn, scriptSig.SigHash);

			if(!pubkey.Verify(sighash, scriptSig.Signature))
			{
				if((ScriptVerify & ScriptVerify.StrictEnc) != 0)
					return false;

				//Replicate OpenSSL bug on 23b397edccd3740a74adb603c9756370fafcde9bcc4483eb271ecad09a94dd63 (http://r6.ca/blog/20111119T211504Z.html)
				var nLenR = vchSig[3];
				var nLenS = vchSig[5 + nLenR];
				var r = 4;
				var s = 6 + nLenR;
				var newS = new BouncyCastle.Math.BigInteger(1, vchSig, s, nLenS);
				var newR = new BouncyCastle.Math.BigInteger(1, vchSig, r, nLenR);
				var sig2 = new EcdsaSignature(newR, newS);
				if(sig2.R != scriptSig.Signature.R || sig2.S != scriptSig.Signature.S)
				{
					if(!pubkey.Verify(sighash, sig2))
						return false;
				}
			}

			return true;
		}


		public bool IsAllowedSignature(SigHash sigHash)
		{
			if(SigHash == SigHash.Undefined)
				return true;
			else
				return SigHash == sigHash;
		}


		private void Load(ScriptEvaluationContext other)
		{
			_stack = Clone(other._stack);
			ScriptVerify = other.ScriptVerify;
			SigHash = other.SigHash;
		}

		public ScriptEvaluationContext Clone()
		{
			return new ScriptEvaluationContext()
			{
				_stack = Clone(_stack),
				ScriptVerify = ScriptVerify,
				SigHash = SigHash
			};
		}

		private Stack<byte[]> Clone(Stack<byte[]> stack)
		{
			var elements = stack.ToArray();
			Array.Reverse(elements);
			return new Stack<byte[]>(elements.Select(s => s.ToArray()));
		}

		public bool? Result
		{
			get
			{
				if(Stack.Count == 0)
					return null;
				return CastToBool(Stack.Peek());
			}
		}

		public Exception ThrownException
		{
			get;
			set;
		}
	}
}
