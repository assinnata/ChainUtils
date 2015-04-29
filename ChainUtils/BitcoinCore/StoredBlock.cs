using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ChainUtils.BitcoinCore
{
	public class DiskBlockPosRange
	{
		private static DiskBlockPosRange _all = new DiskBlockPosRange(DiskBlockPos.Begin, DiskBlockPos.End);

		public static DiskBlockPosRange All
		{
			get
			{
				return _all;
			}
		}

		/// <summary>
		/// Represent a disk block range
		/// </summary>
		/// <param name="begin">Beginning of the range (included)</param>
		/// <param name="end">End of the range (excluded)</param>
		public DiskBlockPosRange(DiskBlockPos begin = null, DiskBlockPos end = null)
		{
			if(begin == null)
				begin = DiskBlockPos.Begin;
			if(end == null)
				end = DiskBlockPos.End;
			_begin = begin;
			_end = end;
			if(end <= begin)
				throw new ArgumentException("End should be more than begin");
		}
		private readonly DiskBlockPos _begin;
		public DiskBlockPos Begin
		{
			get
			{
				return _begin;
			}
		}
		private readonly DiskBlockPos _end;
		public DiskBlockPos End
		{
			get
			{
				return _end;
			}
		}

		public bool InRange(DiskBlockPos pos)
		{
			return Begin <= pos && pos < End;
		}
		public override string ToString()
		{
			return Begin + " <= x < " + End;
		}
	}
	public class DiskBlockPos : IBitcoinSerializable
	{
		private static DiskBlockPos _begin = new DiskBlockPos(0, 0);

		public static DiskBlockPos Begin
		{
			get
			{
				return _begin;
			}
		}

		private static DiskBlockPos _end = new DiskBlockPos(uint.MaxValue, uint.MaxValue);
		public static DiskBlockPos End
		{
			get
			{
				return _end;
			}
		}

		public DiskBlockPos()
		{

		}
		public DiskBlockPos(uint file, uint position)
		{
			_file = file;
			_position = position;
			UpdateHash();
		}
		private uint _file;
		public uint File
		{
			get
			{
				return _file;
			}
		}
		private uint _position;
		public uint Position
		{
			get
			{
				return _position;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsCompactVarInt(ref _file);
			stream.ReadWriteAsCompactVarInt(ref _position);
			if(!stream.Serializing)
				UpdateHash();
		}

		private void UpdateHash()
		{
			_hash = ToString().GetHashCode();
		}

		int _hash;

		#endregion

		public override bool Equals(object obj)
		{
			var item = obj as DiskBlockPos;
			if(item == null)
				return false;
			return _hash.Equals(item._hash);
		}
		public static bool operator ==(DiskBlockPos a, DiskBlockPos b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._hash == b._hash;
		}

		public static bool operator !=(DiskBlockPos a, DiskBlockPos b)
		{
			return !(a == b);
		}

		public static bool operator <(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File < b.File)
				return true;
			if(a.File == b.File && a.Position < b.Position)
				return true;
			return false;
		}
		public static bool operator <=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a < b;
		}
		public static bool operator >(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File > b.File)
				return true;
			if(a.File == b.File && a.Position > b.Position)
				return true;
			return false;
		}
		public static bool operator >=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a > b;
		}
		public override int GetHashCode()
		{
			return _hash.GetHashCode();
		}


		public DiskBlockPos OfFile(uint file)
		{
			return new DiskBlockPos(file, Position);
		}

		public override string ToString()
		{
			return "f:" + File + "p:" + Position;
		}

		static readonly Regex Reg = new Regex("f:([0-9]*)p:([0-9]*)"
#if !PORTABLE
			,RegexOptions.Compiled
#endif			
			);
		public static DiskBlockPos Parse(string data)
		{
			var match = Reg.Match(data);
			if(!match.Success)
				throw new FormatException("Invalid position string : " + data);
			return new DiskBlockPos(uint.Parse(match.Groups[1].Value), uint.Parse(match.Groups[2].Value));
		}
	}
	public class StoredBlock : StoredItem<Block>
	{
		public bool ParseSkipBlockContent
		{
			get;
			set;
		}

		public StoredBlock(Network expectedNetwork, DiskBlockPos position)
			: base(expectedNetwork, position)
		{
		}
		public StoredBlock(uint magic, Block block, DiskBlockPos blockPosition)
			: base(magic, block, blockPosition)
		{
		}


		#region IBitcoinSerializable Members


		static byte[] _unused = new byte[1024 * 4];
		protected override void ReadWriteItem(BitcoinStream stream, ref Block item)
		{
			if(!ParseSkipBlockContent)
				stream.ReadWrite(ref item);
			else
			{
				var beforeReading = stream.Inner.Position;
				var header = item == null ? null : item.Header;
				stream.ReadWrite(ref header);
				if(!stream.Serializing)
					item = new Block(header);

				var headerSize = stream.Inner.Position - beforeReading;
				var bodySize = Header.ItemSize - headerSize;
				if(bodySize > 1024 * 4)
				{
					stream.Inner.Position = beforeReading + Header.ItemSize;
				}
				else //Does not refill internal buffer, thus quicker than Seek
				{
					stream.Inner.Read(_unused, 0, (int)bodySize);
				}
			}
		}


		#endregion
#if !NOFILEIO
		public static IEnumerable<StoredBlock> EnumerateFile(string file, uint fileIndex = 0, DiskBlockPosRange range = null)
		{
			return new BlockStore(Path.GetDirectoryName(file), Network.Main).EnumerateFile(file, fileIndex, range);
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(string folder, DiskBlockPosRange range = null)
		{
			return new BlockStore(folder, Network.Main).EnumerateFolder(range);
		}
#endif
	}
}
