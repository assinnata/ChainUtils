using System.Linq;
using ChainUtils.DataEncoders;

namespace ChainUtils
{
	public class BitcoinColoredAddress : Base58Data, IDestination
	{
		public BitcoinColoredAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinColoredAddress(BitcoinAddress address)
			:base(Build(address),address.Network)
		{
			
		}

		private static byte[] Build(BitcoinAddress address)
		{
			var version = address.Network.GetVersionBytes(address.Type);
			var data = address.ToBytes();
			return version.Concat(data).ToArray();
		}

		protected override bool IsValid
		{
			get
			{
				return Address != null;
			}
		}

		BitcoinAddress _address;
		public BitcoinAddress Address
		{
			get
			{
				if(_address == null)
				{
					var base58 = Encoders.Base58Check.EncodeData(VchData);
					_address = BitcoinAddress.Create(base58, Network);
				}
				return _address;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ColoredAddress;
			}
		}

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return Address.ScriptPubKey;
			}
		}

		#endregion

		public static string GetWrappedBase58(string base58, Network network)
		{
			var coloredVersion = network.GetVersionBytes(Base58Type.ColoredAddress);
			var inner = Encoders.Base58Check.DecodeData(base58);
			inner = inner.Skip(coloredVersion.Length).ToArray();
			return Encoders.Base58Check.EncodeData(inner);
		}
	}
}
