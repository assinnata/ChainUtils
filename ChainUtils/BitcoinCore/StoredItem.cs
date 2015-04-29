using System.IO;
using System.Threading;

namespace ChainUtils.BitcoinCore
{
	public class StoredHeader : IBitcoinSerializable
	{
		public StoredHeader()
		{

		}
		private readonly Network _expectedNetwork;
		public Network ExpectedNetwork
		{
			get
			{
				return _expectedNetwork;
			}
		}
		public StoredHeader(Network expectedNetwork)
		{
			_expectedNetwork = expectedNetwork;
		}
		uint _magic;
		public uint Magic
		{
			get
			{
				return _magic;
			}
			set
			{
				_magic = value;
			}
		}

		uint _size;
		public uint ItemSize
		{
			get
			{
				return _size;
			}
			set
			{
				_size = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(_expectedNetwork == null || stream.Serializing)
			{
				stream.ReadWrite(ref _magic);
			}
			else
			{
				if(!_expectedNetwork.ReadMagic(stream.Inner, default(CancellationToken)))
					return;
				_magic = ExpectedNetwork.Magic;
			}
			if(_magic == 0)
				return;
			stream.ReadWrite(ref _size);
		}


		#endregion
	}
	public class StoredItem<T> : IBitcoinSerializable where T : IBitcoinSerializable, new()
	{
		public StoredItem(Network expectedNetwork, DiskBlockPos position)
		{
			_header = new StoredHeader(expectedNetwork);
			_blockPosition = position;
		}
		public StoredItem(uint magic, T item, DiskBlockPos position)
		{
			_blockPosition = position;
			_item = item;
			_header.Magic = magic;
			_header.ItemSize = (uint)item.GetSerializedSize();
		}
		public bool ParseSkipItem
		{
			get;
			set;
		}

		private readonly DiskBlockPos _blockPosition;
		public DiskBlockPos BlockPosition
		{
			get
			{
				return _blockPosition;
			}
		}



		private StoredHeader _header = new StoredHeader();
		public StoredHeader Header
		{
			get
			{
				return _header;
			}
		}

		private T _item = new T();
		public T Item
		{
			get
			{
				return _item;
			}
		}

		public bool HasChecksum
		{
			get;
			set;
		}

		private Uint256 _checksum = new Uint256(0);
		public Uint256 Checksum
		{
			get
			{
				return _checksum;
			}
			set
			{
				_checksum = value;
			}
		}

		public uint GetStorageSize()
		{
			var ms = new MemoryStream();
			var stream = new BitcoinStream(ms, true);
			stream.ReadWrite(ref _header);
			return _header.ItemSize + (uint)stream.Inner.Length + (HasChecksum ? (uint)(256 / 8) : 0);
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _header);
			if(_header.Magic == 0)
				return;

			if(ParseSkipItem)
				stream.Inner.Position += _header.ItemSize;
			else
				ReadWriteItem(stream, ref _item);
			if(HasChecksum)
				stream.ReadWrite(ref _checksum);
		}

		protected virtual void ReadWriteItem(BitcoinStream stream, ref T item)
		{
			stream.ReadWrite(ref item);
		}

	}
}
