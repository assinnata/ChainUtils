using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainUtils
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId);
	}
	
}
