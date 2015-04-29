using System.Collections.Generic;

namespace ChainUtils.Protocol
{
	[Payload("getdata")]
	public class GetDataPayload : Payload
	{
		public GetDataPayload()
		{
		}
		public GetDataPayload(params InventoryVector[] vectors)
		{
			_inventory.AddRange(vectors);
		}
		List<InventoryVector> _inventory = new List<InventoryVector>();

		public List<InventoryVector> Inventory
		{
			set
			{
				_inventory = value;
			}
			get
			{
				return _inventory;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _inventory);
		}
	}
}

