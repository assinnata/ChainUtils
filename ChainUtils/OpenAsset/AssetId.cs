using System;
using ChainUtils.DataEncoders;

namespace ChainUtils.OpenAsset
{
	public class AssetId
	{
		internal byte[] Bytes;

		public AssetId()
		{
			Bytes = new byte[] { 0 };
		}

		public AssetId(IDestination assetScriptPubKey)
			: this(assetScriptPubKey.ScriptPubKey)
		{
		}

		public AssetId(BitcoinAssetId assetId)
		{
			if(assetId == null)
				throw new ArgumentNullException("assetId");
			Bytes = assetId.AssetId.Bytes;
		}

		public AssetId(Script assetScriptPubKey)
			: this(assetScriptPubKey.Hash)
		{
		}

		public AssetId(ScriptId scriptId)
		{
			Bytes = scriptId.ToBytes(true);
		}
		public AssetId(byte[] value)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			Bytes = value;
		}
		public AssetId(Uint160 value)
			: this(value.ToBytes())
		{
		}

		public AssetId(string value)
		{
			Bytes = Encoders.Hex.DecodeData(value);
			_str = value;
		}

		public BitcoinAssetId GetWif(Network network)
		{
			return new BitcoinAssetId(this, network);
		}

		public byte[] ToBytes()
		{
			return ToBytes(false);
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return Bytes;
			var array = new byte[Bytes.Length];
			Array.Copy(Bytes, array, Bytes.Length);
			return array;
		}

		public override bool Equals(object obj)
		{
			var item = obj as AssetId;
			if(item == null)
				return false;
			return Utils.ArrayEqual(Bytes, item.Bytes);
		}
		public static bool operator ==(AssetId a, AssetId b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a.Bytes, b.Bytes);
		}

		public static bool operator !=(AssetId a, AssetId b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(Bytes);
		}

		string _str;
		public override string ToString()
		{
			if(_str == null)
				_str = Encoders.Hex.EncodeData(Bytes);
			return _str;
		}
	}
}
