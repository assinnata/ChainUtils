using System;
using ChainUtils.Crypto;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public class TxDestination : IDestination
	{
		byte[] _destBytes;

		public TxDestination()
		{
			_destBytes = new byte[] { 0 };
		}

		public TxDestination(byte[] value)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			_destBytes = value;
		}
		public TxDestination(Uint160 value)
			: this(value.ToBytes())
		{
		}

		public TxDestination(string value)
		{
			_destBytes = Encoders.Hex.DecodeData(value);
			_str = value;
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return BitcoinAddress.Create(this, network);
		}

		[Obsolete("Use ScriptPubKey instead")]
		public Script CreateScriptPubKey()
		{
			return ScriptPubKey;
		}

		#region IDestination Members

		public virtual Script ScriptPubKey
		{
			get
			{
				return null;
			}
		}

		#endregion


		public byte[] ToBytes()
		{
			return ToBytes(false);
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return _destBytes;
			var array = new byte[_destBytes.Length];
			Array.Copy(_destBytes, array, _destBytes.Length);
			return array;
		}

		public override bool Equals(object obj)
		{
			var item = obj as TxDestination;
			if(item == null)
				return false;
			return Utils.ArrayEqual(_destBytes, item._destBytes);
		}
		public static bool operator ==(TxDestination a, TxDestination b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a._destBytes, b._destBytes);
		}

		public static bool operator !=(TxDestination a, TxDestination b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_destBytes);
		}

		string _str;
		public override string ToString()
		{
			if(_str == null)
				_str = Encoders.Hex.EncodeData(_destBytes);
			return _str;
		}
	}
	public class KeyId : TxDestination
	{
		public KeyId()
			: base(0)
		{

		}

		public KeyId(byte[] value)
			: base(value)
		{

		}
		public KeyId(Uint160 value)
			: base(value)
		{

		}

		public KeyId(string value)
			: base(value)
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}
	}
	public class ScriptId : TxDestination
	{

		public ScriptId()
			: base(0)
		{

		}

		public ScriptId(byte[] value)
			: base(value)
		{

		}
		public ScriptId(Uint160 value)
			: base(value)
		{

		}

		public ScriptId(string value)
			: base(value)
		{
		}

		public ScriptId(Script script)
			: this(Hashes.Hash160(script._Script))
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}
	}
}
