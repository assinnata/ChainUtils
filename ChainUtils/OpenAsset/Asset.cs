using System;

namespace ChainUtils.OpenAsset
{
	public class Asset : IBitcoinSerializable
	{
		ulong _quantity;
		public ulong Quantity
		{
			get
			{
				return _quantity;
			}
			set
			{
				_quantity = value;
			}
		}

		AssetId _id = new AssetId(0);

		public Asset(AssetId id, ulong quantity)
		{
			if(id == null)
				throw new ArgumentNullException("id");
			Quantity = quantity;
			Id = id;
		}
		public Asset(BitcoinAssetId id, ulong quantity)
		{
			if(id == null)
				throw new ArgumentNullException("id");
			Quantity = quantity;
			Id = new AssetId(id);
		}

		public Asset(IDestination issuer, ulong quantity)
			: this(new AssetId(issuer), quantity)
		{
		}

		public Asset()
		{

		}
		public AssetId Id
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			var assetId = _id.ToBytes();
			stream.ReadWrite(ref assetId);
			if(!stream.Serializing)
				_id = new AssetId(assetId);
			stream.ReadWrite(ref _quantity);
		}

		#endregion

		public override string ToString()
		{
			return Quantity + "-" + Id;
		}
	}
}
