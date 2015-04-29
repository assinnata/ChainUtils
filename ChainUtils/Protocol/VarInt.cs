namespace ChainUtils.Protocol
{
	public class CompactVarInt : IBitcoinSerializable
	{
		private ulong _value = 0;
		private int _size;
		public CompactVarInt(int size)
		{
			_size = size;
		}
		public CompactVarInt(ulong value, int size)
		{
			_value = value;
			_size = size;
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var n = _value;
				var tmp = new byte[(_size * 8 + 6) / 7];
				var len = 0;
				while(true)
				{
					var a = (byte)(n & 0x7F);
					var b = (byte)(len != 0 ? 0x80 : 0x00);
					tmp[len] = (byte)(a | b);
					if(n <= 0x7F)
						break;
					n = (n >> 7) - 1;
					len++;
				}
				do
				{
					var b = tmp[len];
					stream.ReadWrite(ref b);
				} while(len-- != 0);
			}
			else
			{
				ulong n = 0;
				while(true)
				{
					byte chData = 0;
					stream.ReadWrite(ref chData);
					var a = (n << 7);
					var b = (byte)(chData & 0x7F);
					n = (a | b);
					if((chData & 0x80) != 0)
						n++;
					else
						break;
				}
				_value = n;
			}
		}

		#endregion

		public ulong ToLong()
		{
			return _value;
		}
	}


	//https://en.bitcoin.it/wiki/Protocol_specification#Variable_length_integer
	public class VarInt : IBitcoinSerializable
	{
		private byte _prefixByte = 0;
		private ulong _value = 0;

		public VarInt()
			: this(0)
		{

		}
		public VarInt(ulong value)
		{
			_value = value;
			if(_value < 0xFD)
				_prefixByte = (byte)(int)_value;
			else if(_value <= 0xffff)
				_prefixByte = 0xFD;
			else if(_value <= 0xffffffff)
				_prefixByte = 0xFE;
			else
				_prefixByte = 0xFF;
		}

		public ulong ToLong()
		{
			return _value;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _prefixByte);
			if(_prefixByte < 0xFD)
			{
				_value = _prefixByte;
			}
			else if(_prefixByte == 0xFD)
			{
				var value = (ushort)_value;
				stream.ReadWrite(ref value);
				_value = value;
			}
			else if(_prefixByte == 0xFE)
			{
				var value = (uint)_value;
				stream.ReadWrite(ref value);
				_value = value;
			}
			else
			{
				var value = (ulong)_value;
				stream.ReadWrite(ref value);
				_value = value;
			}
		}

		#endregion


	}
}
