#if !NOSOCKET
using System;
using System.Net;
using ChainUtils.DataEncoders;

namespace ChainUtils.Protocol
{
	[Payload("version")]
	public class VersionPayload : Payload, IBitcoinSerializable
	{
		static string _nUserAgent;
		public static string GetChainUtilsUserAgent()
		{
			if(_nUserAgent == null)
			{
				var version = typeof(VersionPayload).Assembly.GetName().Version;
				_nUserAgent = "/ChainUtils:" + version.Major + "." + version.MajorRevision + "." + version.Minor + "/";
			}
			return _nUserAgent;
		}
		uint _version;

		public ProtocolVersion Version
		{
			get
			{
				if(_version == 10300) //A version number of 10300 is converted to 300 before being processed
					return (ProtocolVersion)(300);  //https://en.bitcoin.it/wiki/Version_Handshake
				return (ProtocolVersion)_version;
			}
			set
			{
				if(value == (ProtocolVersion)10300)
					value = (ProtocolVersion)300;
				_version = (uint)value;
			}
		}
		ulong _services;
		long _timestamp;

		public DateTimeOffset Timestamp
		{
			get
			{
				return Utils.UnixTimeToDateTime((uint)_timestamp);
			}
			set
			{
				_timestamp = Utils.DateTimeToUnixTime(value);
			}
		}

		NetworkAddress _addrRecv = new NetworkAddress();
		public IPEndPoint AddressReceiver
		{
			get
			{
				return _addrRecv.Endpoint;
			}
			set
			{
				_addrRecv.Endpoint = value;
			}
		}
		NetworkAddress _addrFrom = new NetworkAddress();
		public IPEndPoint AddressFrom
		{
			get
			{
				return _addrFrom.Endpoint;
			}
			set
			{
				_addrFrom.Endpoint = value;
			}
		}

		ulong _nonce;
		public ulong Nonce
		{
			get
			{
				return _nonce;
			}
			set
			{
				_nonce = value;
			}
		}
		int _startHeight;

		public int StartHeight
		{
			get
			{
				return _startHeight;
			}
			set
			{
				_startHeight = value;
			}
		}

		bool _relay;
		public bool Relay
		{
			get
			{
				return _relay;
			}
			set
			{
				_relay = value;
			}
		}

		VarString _userAgent;
		public string UserAgent
		{
			get
			{
				return Encoders.ASCII.EncodeData(_userAgent.GetString());
			}
			set
			{
				_userAgent = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _version);
			using(stream.ProtocolVersionScope((ProtocolVersion)_version))
			{
				stream.ReadWrite(ref _services);
				stream.ReadWrite(ref _timestamp);
				using(stream.ProtocolVersionScope(ProtocolVersion.CaddrTimeVersion - 1)) //No time field in version message
				{
					stream.ReadWrite(ref _addrRecv);
				}
				if(_version >= 106)
				{
					using(stream.ProtocolVersionScope(ProtocolVersion.CaddrTimeVersion - 1)) //No time field in version message
					{
						stream.ReadWrite(ref _addrFrom);
					}
					stream.ReadWrite(ref _nonce);
					stream.ReadWrite(ref _userAgent);
					if(_version < 60002)
						if(_userAgent.Length != 0)
							throw new FormatException("Should not find user agent for current version " + _version);
					stream.ReadWrite(ref _startHeight);
					if(_version >= 70001)
						stream.ReadWrite(ref _relay);
				}
			}
		}

		#endregion


		public override string ToString()
		{
			return Version.ToString();
		}
	}
}
#endif