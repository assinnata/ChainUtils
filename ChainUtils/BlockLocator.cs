using System.Collections.Generic;

namespace ChainUtils
{
	public class BlockLocator : IBitcoinSerializable
	{
		public BlockLocator()
		{

		}
		public BlockLocator(List<Uint256> hashes)
		{
			_vHave = hashes;
		}

		List<Uint256> _vHave = new List<Uint256>();
		public List<Uint256> Blocks
		{
			get
			{
				return _vHave;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _vHave);
		}

		#endregion
	}
}
