#if !NOFILEIO
using System.IO;

namespace ChainUtils.BitcoinCore
{
	public class DataDirectory
	{
		private readonly string _folder;
		public string Folder
		{
			get
			{
				return _folder;
			}
		}

		private readonly Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}
		public DataDirectory(string dataFolder, Network network)
		{
			EnsureExist(dataFolder);
			_folder = dataFolder;
			_network = network;
		}

		private void EnsureExist(string folder)
		{
			if(!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
		}

		public IndexedBlockUndoStore GetIndexedBlockUndoStore()
		{
			var path = Path.Combine(Folder, "blocks");
			EnsureExist(path);
			return new IndexedBlockUndoStore(new SqLiteNoSqlRepository(Path.Combine(path, "undoindex")),
										 new BlockUndoStore(path, Network));
		}

		public IndexedBlockStore GetIndexedBlockStore()
		{
			var path = Path.Combine(Folder, "blocks");
			EnsureExist(path);
			return new IndexedBlockStore(new SqLiteNoSqlRepository(Path.Combine(path, "blockindex")), 
										 new BlockStore(path, Network));
		}

		public CoinsView GetCoinsView()
		{
			var path = Path.Combine(Folder, "coins");
			EnsureExist(path);
			return new CoinsView(new SqLiteNoSqlRepository(Path.Combine(path, "coinsIndex"))); 
		}
	}
}
#endif