namespace ChainUtils
{
	public abstract class BitcoinExtKeyBase : Base58Data, IDestination
	{
	    protected BitcoinExtKeyBase(IBitcoinSerializable key, Network network)
			: base(key.ToBytes(), network)
		{
		}

	    protected BitcoinExtKeyBase(string base58, Network network)
			: base(base58, network)
		{
		}


		#region IDestination Members

		public abstract Script ScriptPubKey
		{
			get;
		}

		#endregion
	}

	public class BitcoinExtKey : BitcoinExtKeyBase, ISecret
	{
		public BitcoinExtKey(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{

		}
		public BitcoinExtKey(ExtKey key, Network network)
			: base(key, network)
		{

		}

		protected override bool IsValid
		{
			get
			{
				return VchData.Length == 74;
			}
		}

		ExtKey _key;
		public ExtKey ExtKey
		{
			get
			{
				if(_key == null)
				{
					_key = new ExtKey();
					_key.ReadWrite(VchData);
				}
				return _key;
			}
		}


		public override Base58Type Type
		{
			get
			{
				return Base58Type.ExtSecretKey;
			}
		}

		public override Script ScriptPubKey
		{
			get
			{
				return ExtKey.ScriptPubKey;
			}
		}

		#region ISecret Members

		public Key PrivateKey
		{
			get
			{
				return ExtKey.PrivateKey;
			}
		}

		#endregion
	}
	public class BitcoinExtPubKey : BitcoinExtKeyBase
	{
		public BitcoinExtPubKey(ExtPubKey key, Network network)
			: base(key, network)
		{

		}

		public BitcoinExtPubKey(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		ExtPubKey _pubKey;
		public ExtPubKey ExtPubKey
		{
			get
			{
				if(_pubKey == null)
				{
					_pubKey = new ExtPubKey();
					_pubKey.ReadWrite(VchData);
				}
				return _pubKey;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ExtPublicKey;
			}
		}

		public override Script ScriptPubKey
		{
			get
			{
				return ExtPubKey.ScriptPubKey;
			}
		}
	}
}
