namespace ChainUtils.OpenAsset
{
	public class BitcoinAssetId : Base58Data
	{
		public BitcoinAssetId(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}
		public BitcoinAssetId(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinAssetId(AssetId assetId, Network network)
			: this(assetId.Bytes, network)
		{
		}

		AssetId _assetId;
		public AssetId AssetId
		{
			get
			{
				if(_assetId == null)
					_assetId = new AssetId(VchData);
				return _assetId;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return VchData.Length == 20;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.AssetId;
			}
		}
	}
}
