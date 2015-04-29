using System;
using System.Linq;

namespace ChainUtils.Protocol
{
	public class VarString : IBitcoinSerializable
	{
		public VarString()
		{

		}
		byte[] _bytes = new byte[0];
		public int Length
		{
			get
			{
				return _bytes.Length;
			}
		}
		public VarString(byte[] bytes)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");
			_bytes = bytes;
		}
		public byte[] GetString()
		{
			return GetString(false);
		}
		public byte[] GetString(bool @unsafe)
		{
			if(@unsafe)
				return _bytes;
			return _bytes.ToArray();
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			 var len = new VarInt((ulong)_bytes.Length);
			 stream.ReadWrite(ref len);
			if(!stream.Serializing)
				_bytes = new byte[len.ToLong()];
			stream.ReadWrite(ref _bytes);
		}

		#endregion
	}
}
