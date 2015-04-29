
using System;
using System.IO;
using System.Linq;
using ChainUtils.DataEncoders;
using ChainUtils.Protocol;

namespace ChainUtils
{
	public class Uint256 :  IBitcoinSerializable
	{

		public Uint256()
		{
			for(var i = 0 ; i < Width ; i++)
				Pn[i] = 0;
		}

		public Uint256(Uint256 b)
		{
			for(var i = 0 ; i < Width ; i++)
				Pn[i] = b.Pn[i];
		}

		protected const int Width = 256 / 32;
		protected const int WidthByte = 256 / 8;
		protected internal UInt32[] Pn = new UInt32[Width];

		internal void SetHex(string str)
		{
			Array.Clear(Pn, 0, Pn.Length);
			str = str.TrimStart();

			var i = 0;
			if(str.Length >= 2)
				if(str[0] == '0' && char.ToLower(str[1]) == 'x')
					i += 2;

			var pBegin = i;
			while(i < str.Length && HexEncoder.IsDigit(str[i]) != -1)
				i++;

			i--;

			var p1 = 0;
			var pend = p1 + Width * 4;
			while(i >= pBegin && p1 < pend)
			{
				SetByte(p1, (byte)HexEncoder.IsDigit(str[i]));
				i--;
				if(i >= pBegin)
				{
					var n = (byte)HexEncoder.IsDigit(str[i]);
					n = (byte)(n << 4);
					SetByte(p1, (byte)(GetByte(p1) | n));
					i--;
					p1++;
				}
			}
		}
		internal void SetByte(int index, byte value)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);

			var currentValue = Pn[uintIndex];
			var mask = ((uint)0xFF << (byteIndex * 8));
			currentValue = currentValue & ~mask;
			var shiftedValue = (uint)value << (byteIndex * 8);
			currentValue |= shiftedValue;
			Pn[uintIndex] = currentValue;
		}

		private static readonly uint[] Lookup32 = CreateLookup32();
		private static uint[] CreateLookup32()
		{
			var result = new uint[256];
			for(var i = 0 ; i < 256 ; i++)
			{
				var s = i.ToString("x2");
				result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
			}
			return result;
		}
		internal string GetHex()
		{
			var lookup32 = Lookup32;
			var result = new char[WidthByte * 2];
			for(var i = 0 ; i < WidthByte ; i++)
			{
				var val = lookup32[GetByte(WidthByte - i - 1)];
				result[2 * i] = (char)val;
				result[2 * i + 1] = (char)(val >> 16);
			}
			return new string(result);
		}
		
		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			var value = Pn[uintIndex];
			return (byte)(value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			return GetHex();
		}


		public Uint256(ulong b)
		{
			Pn[0] = (uint)b;
			Pn[1] = (uint)(b >> 32);
			for (var i = 2; i < Width; i++)
				Pn[i] = 0;
		}
		public Uint256(byte[] vch, bool lendian = true)
		{
			if(!lendian)
				vch = vch.Reverse().ToArray();
			if(vch.Length == Pn.Length * 4)
			{
				for(int i = 0, y = 0 ; i < Pn.Length && y < vch.Length ; i++, y += 4)
				{
					Pn[i] = BitConverter.ToUInt32(vch, y);
				}
			}
			else
				throw new FormatException("the byte array should be 256 byte long");
		}

		public Uint256(string str)
		{
			SetHex(str);
		}

		public Uint256(byte[] vch)
		{
			if(vch.Length == Pn.Length * 4)
			{
				for(int i = 0, y = 0 ; i < Pn.Length && y < vch.Length ; i++, y += 4)
				{
					Pn[i] = BitConverter.ToUInt32(vch, y);
				}
			}
			else
				throw new FormatException("the byte array should be 256 byte long");
		}

		public override bool Equals(object obj)
		{
			var item = obj as Uint256;
			if(item == null)
				return false;
			return AreEquals(Pn, item.Pn);
		}
		public static bool operator ==(Uint256 a, Uint256 b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return AreEquals(a.Pn, b.Pn);
		}

		private static bool AreEquals(uint[] ar1, uint[] ar2)
		{
			if(ar1.Length != ar2.Length)
				return false;
			for(var i = 0 ; i < ar1.Length ; i++)
			{
				if(ar1[i] != ar2[i])
					return false;
			}
			return true;
		}

		public static bool operator <(Uint256 a, Uint256 b)
		{

			return Comparison(a, b) < 0;

		}

		public static bool operator >(Uint256 a, Uint256 b)
		{

			return Comparison(a, b) > 0;

		}

		public static bool operator <=(Uint256 a, Uint256 b)
		{

			return Comparison(a, b) <= 0;

		}

		public static bool operator >=(Uint256 a, Uint256 b)
		{

			return Comparison(a, b) >= 0;

		}

		private static int Comparison(Uint256 a, Uint256 b)
		{
			 for (var i = Width-1; i >= 0; i--)
			{
				if (a.Pn[i] < b.Pn[i])
					return -1;
				else if (a.Pn[i] > b.Pn[i])
					return 1;
			}
			return 0;
		}

		public static bool operator !=(Uint256 a, Uint256 b)
		{
			return !(a == b);
		}

		public static bool operator ==(Uint256 a, ulong b)
		{
			return (a == new Uint256(b));
		}
		public static bool operator !=(Uint256 a, ulong b)
		{
			return !(a == new Uint256(b));
		}
		public static Uint256 operator ^(Uint256 a, Uint256 b)
		{
			var c = new Uint256();
			c.Pn = new uint[a.Pn.Length];
			for(var i = 0 ; i < c.Pn.Length ; i++)
			{
				c.Pn[i] = a.Pn[i] ^ b.Pn[i];
			}
			return c;
		}

		public static bool operator!(Uint256 a)
	    {
	     for (var i = 0; i < Width; i++)
	         if (a.Pn[i] != 0)
	             return false;
	     return true;
	   }

	    public static Uint256 operator-(Uint256 a, Uint256 b)
    {
		return a + (-b);
    }

	   public static Uint256 operator+(Uint256 a, Uint256 b)
    {
		var result = new Uint256();
        ulong carry = 0;
        for (var i = 0; i < Width; i++)
        {
            var n = carry + a.Pn[i] + b.Pn[i];
            result.Pn[i] = (uint)(n & 0xffffffff);
            carry = n >> 32;
        }
        return result;
    }

	public static Uint256 operator+(Uint256 a, ulong b)
    {
		return a + new Uint256(b);
    }

	

	public static implicit operator Uint256(ulong value)
	{
		return new Uint256(value);
	}

		public static Uint256 operator &(Uint256 a, Uint256 b)
		{
			var n = new Uint256(a);
			for(var i = 0 ; i < Width ; i++)
				n.Pn[i] &= b.Pn[i];
			return n;
		}
		public static Uint256 operator |(Uint256 a, Uint256 b)
		{
			var n = new Uint256(a);
			for(var i = 0 ; i < Width ; i++)
				n.Pn[i] |= b.Pn[i];
			return n;
		}
		public static Uint256 operator <<(Uint256 a, int shift)
		{
			var result = new Uint256();
			var k = shift / 32;
			shift = shift % 32;
			for(var i = 0 ; i < Width ; i++)
			{
				if(i + k + 1 < Width && shift != 0)
					result.Pn[i + k + 1] |= (a.Pn[i] >> (32 - shift));
				if(i + k < Width)
					result.Pn[i + k] |= (a.Pn[i] << shift);
			}
			return result;
		}

		public static Uint256 operator >>(Uint256 a, int shift)
		{
			var result = new Uint256();
			var k = shift / 32;
			shift = shift % 32;
			for(var i = 0 ; i < Width ; i++)
			{
				if(i - k - 1 >= 0 && shift != 0)
					result.Pn[i - k - 1] |= (a.Pn[i] << (32 - shift));
				if(i - k >= 0)
					result.Pn[i - k] |= (a.Pn[i] >> shift);
			}
			return result;
		}

		
		public static Uint256 operator ~(Uint256 a)
		{
			var b = new Uint256();
			for(var i = 0 ; i < b.Pn.Length ; i++)
			{
				b.Pn[i] = ~a.Pn[i];
			}
			return b;
		}
		public static Uint256 operator -(Uint256 a)
		{
			var b = new Uint256();
			for(var i = 0 ; i < b.Pn.Length ; i++)
			{
				b.Pn[i] = ~a.Pn[i];
			}
			b++;
			return b;
		}

		 public static Uint256 operator ++(Uint256 a)
		{
			var ret = new Uint256(a);
			return a + new Uint256(1);
		}
		public static Uint256 operator --(Uint256 a)
		{
			return a - 1;
		}
		
		public byte[] ToBytes(bool lendian = true)
{
	var copy = new byte[WidthByte];
	for(var i = 0 ; i < WidthByte ; i++)
	{
		copy[i] = GetByte(i);
	}
	if(!lendian)
		Array.Reverse(copy);
	return copy;
}

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var b = ToBytes();
				stream.ReadWrite(ref b);
			}
			else
			{
				var b = new byte[WidthByte];
				stream.ReadWrite(ref b);
				Pn = new Uint256(b).Pn;
			}
		}

		public void Serialize(Stream stream, int nType = 0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			for(var i = 0 ; i < WidthByte ; i++)
			{
				stream.WriteByte(GetByte(i));
			}
		}

		public void Unserialize(Stream stream, int nType = 0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			for(var i = 0 ; i < WidthByte ; i++)
			{
				var b = stream.ReadByte();
				if(b != -1)
				{
					SetByte(i,(byte)b);
				}
			}
		}

		public int GetSerializeSize(int nType=0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			return WidthByte;
		}
		public int Size
		{
			get
			{
				return WidthByte;
			}
		}

		public ulong GetLow64()
		{
			return Pn[0] | (ulong)Pn[1] << 32;
		}
		public uint GetLow32()
		{
			return Pn[0];
		}
		//public double GetDouble()
		//{
		//	double ret = 0.0;
		//	double fact = 1.0;
		//	for (int i = 0; i < WIDTH; i++) {
		//		ret += fact * pn[i];
		//		fact *= 4294967296.0;
		//	}
		//	return ret;
		//}
		public override int GetHashCode()
		{
			unchecked
			{
				if(Pn == null)
				{
					return 0;
				}
				var hash = 17;
				foreach(var element in Pn)
				{
					hash = hash * 31 + element.GetHashCode();
				}
				return hash;
			}
		}
	}
	public class Uint160 :  IBitcoinSerializable
	{

		public Uint160()
		{
			for(var i = 0 ; i < Width ; i++)
				Pn[i] = 0;
		}

		public Uint160(Uint160 b)
		{
			for(var i = 0 ; i < Width ; i++)
				Pn[i] = b.Pn[i];
		}

		protected const int Width = 160 / 32;
		protected const int WidthByte = 160 / 8;
		protected internal UInt32[] Pn = new UInt32[Width];

		internal void SetHex(string str)
		{
			Array.Clear(Pn, 0, Pn.Length);
			str = str.TrimStart();

			var i = 0;
			if(str.Length >= 2)
				if(str[0] == '0' && char.ToLower(str[1]) == 'x')
					i += 2;

			var pBegin = i;
			while(i < str.Length && HexEncoder.IsDigit(str[i]) != -1)
				i++;

			i--;

			var p1 = 0;
			var pend = p1 + Width * 4;
			while(i >= pBegin && p1 < pend)
			{
				SetByte(p1, (byte)HexEncoder.IsDigit(str[i]));
				i--;
				if(i >= pBegin)
				{
					var n = (byte)HexEncoder.IsDigit(str[i]);
					n = (byte)(n << 4);
					SetByte(p1, (byte)(GetByte(p1) | n));
					i--;
					p1++;
				}
			}
		}
		internal void SetByte(int index, byte value)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);

			var currentValue = Pn[uintIndex];
			var mask = ((uint)0xFF << (byteIndex * 8));
			currentValue = currentValue & ~mask;
			var shiftedValue = (uint)value << (byteIndex * 8);
			currentValue |= shiftedValue;
			Pn[uintIndex] = currentValue;
		}

		private static readonly uint[] Lookup32 = CreateLookup32();
		private static uint[] CreateLookup32()
		{
			var result = new uint[256];
			for(var i = 0 ; i < 256 ; i++)
			{
				var s = i.ToString("x2");
				result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
			}
			return result;
		}
		internal string GetHex()
		{
			var lookup32 = Lookup32;
			var result = new char[WidthByte * 2];
			for(var i = 0 ; i < WidthByte ; i++)
			{
				var val = lookup32[GetByte(WidthByte - i - 1)];
				result[2 * i] = (char)val;
				result[2 * i + 1] = (char)(val >> 16);
			}
			return new string(result);
		}
		
		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			var value = Pn[uintIndex];
			return (byte)(value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			return GetHex();
		}


		public Uint160(ulong b)
		{
			Pn[0] = (uint)b;
			Pn[1] = (uint)(b >> 32);
			for (var i = 2; i < Width; i++)
				Pn[i] = 0;
		}
		public Uint160(byte[] vch, bool lendian = true)
		{
			if(!lendian)
				vch = vch.Reverse().ToArray();
			if(vch.Length == Pn.Length * 4)
			{
				for(int i = 0, y = 0 ; i < Pn.Length && y < vch.Length ; i++, y += 4)
				{
					Pn[i] = BitConverter.ToUInt32(vch, y);
				}
			}
			else
				throw new FormatException("the byte array should be 160 byte long");
		}

		public Uint160(string str)
		{
			SetHex(str);
		}

		public Uint160(byte[] vch)
		{
			if(vch.Length == Pn.Length * 4)
			{
				for(int i = 0, y = 0 ; i < Pn.Length && y < vch.Length ; i++, y += 4)
				{
					Pn[i] = BitConverter.ToUInt32(vch, y);
				}
			}
			else
				throw new FormatException("the byte array should be 160 byte long");
		}

		public override bool Equals(object obj)
		{
			var item = obj as Uint160;
			if(item == null)
				return false;
			return AreEquals(Pn, item.Pn);
		}
		public static bool operator ==(Uint160 a, Uint160 b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return AreEquals(a.Pn, b.Pn);
		}

		private static bool AreEquals(uint[] ar1, uint[] ar2)
		{
			if(ar1.Length != ar2.Length)
				return false;
			for(var i = 0 ; i < ar1.Length ; i++)
			{
				if(ar1[i] != ar2[i])
					return false;
			}
			return true;
		}

		public static bool operator <(Uint160 a, Uint160 b)
		{

			return Comparison(a, b) < 0;

		}

		public static bool operator >(Uint160 a, Uint160 b)
		{

			return Comparison(a, b) > 0;

		}

		public static bool operator <=(Uint160 a, Uint160 b)
		{

			return Comparison(a, b) <= 0;

		}

		public static bool operator >=(Uint160 a, Uint160 b)
		{

			return Comparison(a, b) >= 0;

		}

		private static int Comparison(Uint160 a, Uint160 b)
		{
			 for (var i = Width-1; i >= 0; i--)
			{
				if (a.Pn[i] < b.Pn[i])
					return -1;
				else if (a.Pn[i] > b.Pn[i])
					return 1;
			}
			return 0;
		}

		public static bool operator !=(Uint160 a, Uint160 b)
		{
			return !(a == b);
		}

		public static bool operator ==(Uint160 a, ulong b)
		{
			return (a == new Uint160(b));
		}
		public static bool operator !=(Uint160 a, ulong b)
		{
			return !(a == new Uint160(b));
		}
		public static Uint160 operator ^(Uint160 a, Uint160 b)
		{
			var c = new Uint160();
			c.Pn = new uint[a.Pn.Length];
			for(var i = 0 ; i < c.Pn.Length ; i++)
			{
				c.Pn[i] = a.Pn[i] ^ b.Pn[i];
			}
			return c;
		}

		public static bool operator!(Uint160 a)
	    {
	     for (var i = 0; i < Width; i++)
	         if (a.Pn[i] != 0)
	             return false;
	     return true;
	   }

	    public static Uint160 operator-(Uint160 a, Uint160 b)
    {
		return a + (-b);
    }

	   public static Uint160 operator+(Uint160 a, Uint160 b)
    {
		var result = new Uint160();
        ulong carry = 0;
        for (var i = 0; i < Width; i++)
        {
            var n = carry + a.Pn[i] + b.Pn[i];
            result.Pn[i] = (uint)(n & 0xffffffff);
            carry = n >> 32;
        }
        return result;
    }

	public static Uint160 operator+(Uint160 a, ulong b)
    {
		return a + new Uint160(b);
    }

	

	public static implicit operator Uint160(ulong value)
	{
		return new Uint160(value);
	}

		public static Uint160 operator &(Uint160 a, Uint160 b)
		{
			var n = new Uint160(a);
			for(var i = 0 ; i < Width ; i++)
				n.Pn[i] &= b.Pn[i];
			return n;
		}
		public static Uint160 operator |(Uint160 a, Uint160 b)
		{
			var n = new Uint160(a);
			for(var i = 0 ; i < Width ; i++)
				n.Pn[i] |= b.Pn[i];
			return n;
		}
		public static Uint160 operator <<(Uint160 a, int shift)
		{
			var result = new Uint160();
			var k = shift / 32;
			shift = shift % 32;
			for(var i = 0 ; i < Width ; i++)
			{
				if(i + k + 1 < Width && shift != 0)
					result.Pn[i + k + 1] |= (a.Pn[i] >> (32 - shift));
				if(i + k < Width)
					result.Pn[i + k] |= (a.Pn[i] << shift);
			}
			return result;
		}

		public static Uint160 operator >>(Uint160 a, int shift)
		{
			var result = new Uint160();
			var k = shift / 32;
			shift = shift % 32;
			for(var i = 0 ; i < Width ; i++)
			{
				if(i - k - 1 >= 0 && shift != 0)
					result.Pn[i - k - 1] |= (a.Pn[i] << (32 - shift));
				if(i - k >= 0)
					result.Pn[i - k] |= (a.Pn[i] >> shift);
			}
			return result;
		}

		
		public static Uint160 operator ~(Uint160 a)
		{
			var b = new Uint160();
			for(var i = 0 ; i < b.Pn.Length ; i++)
			{
				b.Pn[i] = ~a.Pn[i];
			}
			return b;
		}
		public static Uint160 operator -(Uint160 a)
		{
			var b = new Uint160();
			for(var i = 0 ; i < b.Pn.Length ; i++)
			{
				b.Pn[i] = ~a.Pn[i];
			}
			b++;
			return b;
		}

		 public static Uint160 operator ++(Uint160 a)
		{
			var ret = new Uint160(a);
			return a + new Uint160(1);
		}
		public static Uint160 operator --(Uint160 a)
		{
			return a - 1;
		}
		
		public byte[] ToBytes(bool lendian = true)
{
	var copy = new byte[WidthByte];
	for(var i = 0 ; i < WidthByte ; i++)
	{
		copy[i] = GetByte(i);
	}
	if(!lendian)
		Array.Reverse(copy);
	return copy;
}

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var b = ToBytes();
				stream.ReadWrite(ref b);
			}
			else
			{
				var b = new byte[WidthByte];
				stream.ReadWrite(ref b);
				Pn = new Uint160(b).Pn;
			}
		}

		public void Serialize(Stream stream, int nType = 0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			for(var i = 0 ; i < WidthByte ; i++)
			{
				stream.WriteByte(GetByte(i));
			}
		}

		public void Unserialize(Stream stream, int nType = 0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			for(var i = 0 ; i < WidthByte ; i++)
			{
				var b = stream.ReadByte();
				if(b != -1)
				{
					SetByte(i,(byte)b);
				}
			}
		}

		public int GetSerializeSize(int nType=0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			return WidthByte;
		}
		public int Size
		{
			get
			{
				return WidthByte;
			}
		}

		public ulong GetLow64()
		{
			return Pn[0] | (ulong)Pn[1] << 32;
		}
		public uint GetLow32()
		{
			return Pn[0];
		}
		//public double GetDouble()
		//{
		//	double ret = 0.0;
		//	double fact = 1.0;
		//	for (int i = 0; i < WIDTH; i++) {
		//		ret += fact * pn[i];
		//		fact *= 4294967296.0;
		//	}
		//	return ret;
		//}
		public override int GetHashCode()
		{
			unchecked
			{
				if(Pn == null)
				{
					return 0;
				}
				var hash = 17;
				foreach(var element in Pn)
				{
					hash = hash * 31 + element.GetHashCode();
				}
				return hash;
			}
		}
	}
}