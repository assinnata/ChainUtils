#if !NOSOCKET
using System.Linq;

namespace ChainUtils.Protocol
{
	[Payload("addr")]
	public class AddrPayload : Payload, IBitcoinSerializable
	{
		NetworkAddress[] _addrList = new NetworkAddress[0];
		public NetworkAddress[] Addresses
		{
			get
			{
				return _addrList;
			}
		}

		public AddrPayload()
		{

		}
		public AddrPayload(NetworkAddress address)
		{
			_addrList = new[] { address };
		}
		public AddrPayload(NetworkAddress[] addresses)
		{
			_addrList = addresses.ToArray();
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _addrList);
		}

		#endregion

		public override string ToString()
		{
			return Addresses.Length + " address(es)";
		}
	}
}
#endif