#if !NOSOCKET
using System.Net;
using System.Net.Sockets;

namespace ChainUtils
{
	public static class IpExtensions
	{
		public static bool IsRfc1918(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return address.IsIPv4() && (
				bytes[15 - 3] == 10 ||
				(bytes[15 - 3] == 192 && bytes[15 - 2] == 168) ||
				(bytes[15 - 3] == 172 && (bytes[15 - 2] >= 16 && bytes[15 - 2] <= 31)));
		}


		public static bool IsIPv4(this IPAddress address)
		{
			return address.AddressFamily == AddressFamily.InterNetwork ||
#if WIN				
				address.IsIPv4MappedToIPv6;
#else
				address.IsIPv4MappedToIPv6();
#endif

		}

		public static bool IsRfc3927(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return address.IsIPv4() && (bytes[15 - 3] == 169 && bytes[15 - 2] == 254);
		}

		public static bool IsRfc3849(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x0D && bytes[15 - 12] == 0xB8;
		}

		public static bool IsRfc3964(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x02);
		}

		public static bool IsRfc6052(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			var pchRfc6052 = new byte[] { 0, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0 };
			return (Memcmp(bytes, pchRfc6052, pchRfc6052.Length) == 0);
		}

		public static bool IsRfc4380(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0 && bytes[15 - 12] == 0);
		}

		public static bool IsRfc4862(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			var pchRfc4862 = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0 };
			return (Memcmp(bytes, pchRfc4862, pchRfc4862.Length) == 0);
		}

		public static bool IsRfc4193(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((bytes[15 - 15] & 0xFE) == 0xFC);
		}

		public static bool IsRfc6145(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			var pchRfc6145 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0 };
			return (Memcmp(bytes, pchRfc6145, pchRfc6145.Length) == 0);
		}

		public static bool IsRfc4843(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x00 && (bytes[15 - 12] & 0xF0) == 0x10);
		}

		static byte[] _pchOnionCat = new byte[] { 0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43 };
		public static bool IsTor(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (Memcmp(bytes, _pchOnionCat, _pchOnionCat.Length) == 0);
		}

		public static bool IsLocal(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			// IPv4 loopback
			if(address.IsIPv4() && (bytes[15 - 3] == 127 || bytes[15 - 3] == 0))
				return true;

			// IPv6 loopback (::1/128)
			var pchLocal = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			if(Memcmp(bytes, pchLocal, 16) == 0)
				return true;

			return false;
		}

		private static int Memcmp(byte[] a, byte[] b, int n)
		{
			return Utils.ArrayEqual(a, 0, b, 0, n) ? 0 : 1;
		}

		public static bool IsMulticast(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (address.IsIPv4() && (bytes[15 - 3] & 0xF0) == 0xE0)
				   || (bytes[15 - 15] == 0xFF);
		}



		public static bool IsRoutable(this IPAddress address, bool allowLocal)
		{
			return address.IsValid() && !(
											(!allowLocal && address.IsRfc1918()) ||
											address.IsRfc3927() ||
											address.IsRfc4862() ||
											(address.IsRfc4193() && !address.IsTor()) ||
											address.IsRfc4843() || (!allowLocal && address.IsLocal())
											);
		}
		public static bool IsValid(this IPAddress address)
		{
			var ip = address.GetAddressBytes();
			// unspecified IPv6 address (::/128)
			var ipNone = new byte[16];
			if(Memcmp(ip, ipNone, 16) == 0)
				return false;

			// documentation IPv6 address
			if(address.IsRfc3849())
				return false;

			if(address.IsIPv4())
			{
				//// INADDR_NONE
				if(Utils.ArrayEqual(ip, 12, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4))
					return false;

				//// 0
				if(Utils.ArrayEqual(ip, 12, new byte[] { 0x0, 0x0, 0x0, 0x0 }, 0, 4))
					return false;
			}

			return true;
		}
	}
}
#endif