using System;
using System.Threading.Tasks;

namespace ChainUtils
{
	public class NoSqlBlockRepository : IBlockRepository
	{
		NoSqlRepository _repository;
		public NoSqlBlockRepository(NoSqlRepository repository)
		{
			if(repository == null)
				throw new ArgumentNullException("repository");
			_repository = repository;
		}
		public NoSqlBlockRepository()
			: this(new InMemoryNoSqlRepository())
		{

		}

		#region IBlockRepository Members

		public Task<Block> GetBlockAsync(Uint256 blockId)
		{
			return _repository.GetAsync<Block>(blockId.ToString());
		}

		#endregion

		public Task PutAsync(Block block)
		{
			return PutAsync(block.GetHash(), block);
		}
		public Task PutAsync(Uint256 blockId, Block block)
		{
			return _repository.PutAsync(blockId.ToString(), block);
		}
	}
}
