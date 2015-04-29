using System;
using ChainUtils.BouncyCastle.Math;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public class TransactionSignature
	{
		static readonly TransactionSignature _Empty = new TransactionSignature(new EcdsaSignature(BigInteger.ValueOf(0), BigInteger.ValueOf(0)), SigHash.All);
		public static TransactionSignature Empty
		{
			get
			{
				return _Empty;
			}
		}
		public TransactionSignature(EcdsaSignature signature, SigHash sigHash)
		{
			if(sigHash == SigHash.Undefined)
				throw new ArgumentException("sigHash should not be Undefined");
			_sigHash = sigHash;
			_signature = signature.MakeCanonical();
		}
		public TransactionSignature(EcdsaSignature signature)
			: this(signature, SigHash.All)
		{

		}
		public TransactionSignature(byte[] sigSigHash)
		{
			_signature = EcdsaSignature.FromDer(sigSigHash).MakeCanonical();
			_sigHash = (SigHash)sigSigHash[sigSigHash.Length - 1];
		}
		public TransactionSignature(byte[] sig, SigHash sigHash)
		{
			_signature = EcdsaSignature.FromDer(sig).MakeCanonical();
			_sigHash = sigHash;
		}

		private readonly EcdsaSignature _signature;
		public EcdsaSignature Signature
		{
			get
			{
				return _signature;
			}
		}
		private readonly SigHash _sigHash;
		public SigHash SigHash
		{
			get
			{
				return _sigHash;
			}
		}

		public byte[] ToBytes()
		{
			var sig = _signature.ToDer();
			var result = new byte[sig.Length + 1];
			Array.Copy(sig, 0, result, 0, sig.Length);
			result[result.Length - 1] = (byte)_sigHash;
			return result;
		}

		public static bool ValidLength(int length)
		{
			return (67 <= length && length <= 80) || length == 9; //9 = Empty signature
		}

		public bool Check(PubKey pubKey, Script scriptPubKey, IndexedTxIn txIn, ScriptVerify verify = ScriptVerify.Standard)
		{
			return Check(pubKey, scriptPubKey, txIn.Transaction, txIn.N, verify);
		}

		public bool Check(PubKey pubKey, Script scriptPubKey, Transaction tx, uint nIndex, ScriptVerify verify = ScriptVerify.Standard)
		{
			return new ScriptEvaluationContext()
			{
				ScriptVerify = verify,
				SigHash = SigHash
			}.CheckSig(this, pubKey, scriptPubKey, tx, nIndex);
		}

		string _id;
		private string Id
		{
			get
			{
				if(_id == null)
					_id = Encoders.Hex.EncodeData(ToBytes());
				return _id;
			}
		}

		public override bool Equals(object obj)
		{
			var item = obj as TransactionSignature;
			if(item == null)
				return false;
			return Id.Equals(item.Id);
		}
		public static bool operator ==(TransactionSignature a, TransactionSignature b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.Id == b.Id;
		}

		public static bool operator !=(TransactionSignature a, TransactionSignature b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}
