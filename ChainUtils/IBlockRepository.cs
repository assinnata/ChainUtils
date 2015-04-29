using System.Threading.Tasks;

namespace ChainUtils
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(Uint256 blockId);
	}
	
}
