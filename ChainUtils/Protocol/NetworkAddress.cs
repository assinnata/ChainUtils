#if !NOSOCKET
using System;
using System.Net;

namespace ChainUtils.Protocol
{
	public class NetworkAddress : IBitcoinSerializable
	{
		uint _time;
		ulong _service = 1;
		byte[] _ip = new byte[16];
		ushort _port;

		public TimeSpan Ago
		{
			get
			{
				return DateTimeOffset.UtcNow - Time;
			}
			set
			{
				Time = DateTimeOffset.UtcNow - value;
			}
		}

		public void Adjust()
		{
			var nNow = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow);
			if(_time <= 100000000 || _time > nNow + 10 * 60)
				_time = nNow - 5 * 24 * 60 * 60;
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return new IPEndPoint(new IPAddress(_ip), _port);
			}
			set
			{
				_port = (ushort)value.Port;
				var ipBytes = value.Address.GetAddressBytes();
				if(ipBytes.Length == 16)
				{
					_ip = ipBytes;
				}
				else if(ipBytes.Length == 4)
				{
					//Convert to ipv4 mapped to ipv6
					//In these addresses, the first 80 bits are zero, the next 16 bits are one, and the remaining 32 bits are the IPv4 address
					_ip = new byte[16];
					Array.Copy(ipBytes, 0, _ip, 12, 4);
					Array.Copy(new byte[] { 0xFF, 0xFF }, 0, _ip, 10, 2);
				}
				else
					throw new NotSupportedException("Invalid IP address type");
			}
		}

		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(_time);
			}
			set
			{
				_time = Utils.DateTimeToUnixTime(value);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.ProtocolVersion >= ProtocolVersion.CaddrTimeVersion)
				stream.ReadWrite(ref _time);
			stream.ReadWrite(ref _service);
			stream.ReadWrite(ref _ip);
			using(stream.BigEndianScope())
			{
				stream.ReadWrite(ref _port);
			}
		}

		#endregion

		public void ZeroTime()
		{
			_time = 0;
		}
	}
}
#endif