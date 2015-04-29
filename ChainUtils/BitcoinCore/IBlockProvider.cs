using System.Collections.Generic;

namespace ChainUtils.BitcoinCore
{
	public interface IBlockProvider
	{
		Block GetBlock(Uint256 id, List<byte[]> searchedData);
	}
}
