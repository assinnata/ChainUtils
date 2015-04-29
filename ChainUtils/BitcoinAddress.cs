using System;

namespace ChainUtils
{
	public class BitcoinScriptAddress : BitcoinAddress
	{

		public BitcoinScriptAddress(string address, Network expectedNetwork)
			: base(address, expectedNetwork)
		{
		}

		public BitcoinScriptAddress(ScriptId scriptId, Network network)
			: base(scriptId, network)
		{
		}

		[Obsolete("Use Hash instead")]
		public override TxDestination Id
		{
			get
			{
				return Hash;
			}
		}

		public override TxDestination Hash
		{
			get
			{
				return new ScriptId(VchData);
			}
		}


		public override Base58Type Type
		{
			get
			{
				return Base58Type.ScriptAddress;
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToScriptHashTemplate.Instance.GenerateScriptPubKey((ScriptId)Hash);
		}
	}
	public class BitcoinAddress : Base58Data, IDestination
	{
		public static BitcoinAddress Create(string base58, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinAddress>(base58, expectedNetwork);
		}

		public BitcoinAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinAddress(KeyId keyId, Network network)
			: base(keyId.ToBytes(), network)
		{
		}

		protected BitcoinAddress(TxDestination dest, Network network)
			: base(dest.ToBytes(), network)
		{
		}

		protected override bool IsValid
		{
			get
			{
				return VchData.Length <= 20;
			}
		}

		[Obsolete("Use Hash instead")]
		public virtual TxDestination Id
		{
			get
			{
				return new KeyId(VchData);
			}
		}
		public virtual TxDestination Hash
		{
			get
			{
				return new KeyId(VchData);
			}
		}

		[Obsolete("Use ScriptPubKey instead")]
		public Script PaymentScript
		{
			get
			{
				return ScriptPubKey;
			}
		}

		Script _scriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				if(_scriptPubKey == null)
				{
					_scriptPubKey = GeneratePaymentScript();
				}
				return _scriptPubKey;
			}
		}


		public BitcoinScriptAddress GetScriptAddress()
		{
			if(this is BitcoinScriptAddress)
				return (BitcoinScriptAddress)this;
			var redeem = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.Hash, Network);
		}



		protected virtual Script GeneratePaymentScript()
		{
			return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey((KeyId)Hash);
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.PubkeyAddress;
			}
		}

		public static BitcoinAddress Create(TxDestination id, Network network)
		{
			if(id == null)
				throw new ArgumentNullException("id");
			if(network == null)
				throw new ArgumentNullException("network");
			if(id is KeyId)
				return new BitcoinAddress(id, network);
			else if(id is ScriptId)
				return new BitcoinScriptAddress((ScriptId)id, network);
			else
				throw new NotSupportedException();
		}

		public BitcoinColoredAddress ToColoredAddress()
		{
			return new BitcoinColoredAddress(this);
		}
	}
}
