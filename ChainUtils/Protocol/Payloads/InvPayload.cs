using System.Collections.Generic;
using System.Linq;

namespace ChainUtils.Protocol
{
	[Payload("inv")]
	public class InvPayload : Payload, IBitcoinSerializable
	{
		public InvPayload()
		{

		}
		public InvPayload(params Transaction[] transactions)
			: this(transactions.Select(tx => new InventoryVector(InventoryType.MsgTx, tx.GetHash())).ToArray())
		{

		}
		public InvPayload(params Block[] blocks)
			: this(blocks.Select(b => new InventoryVector(InventoryType.MsgBlock, b.GetHash())).ToArray())
		{

		}
		public InvPayload(InventoryType type, params Uint256[] hashes)
			: this(hashes.Select(h => new InventoryVector(type, h)).ToArray())
		{

		}
		public InvPayload(params InventoryVector[] invs)
		{
			_inventory.AddRange(invs);
		}
		List<InventoryVector> _inventory = new List<InventoryVector>();
		public List<InventoryVector> Inventory
		{
			get
			{
				return _inventory;
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			var old = stream.MaxArraySize;
			stream.MaxArraySize = 5000;
			stream.ReadWrite(ref _inventory);
			stream.MaxArraySize = old;
		}

		#endregion

		public override string ToString()
		{
			return "Count: " + Inventory.Count.ToString();
		}
	}
}
