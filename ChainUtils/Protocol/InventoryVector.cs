namespace ChainUtils.Protocol
{
	public enum InventoryType : uint
	{
		Error = 0,
		MsgTx = 1,
		MsgBlock = 2,
	}
	public class InventoryVector : Payload, IBitcoinSerializable
	{
		uint _type;
		Uint256 _hash = new Uint256(0);

		public InventoryVector()
		{

		}
		public InventoryVector(InventoryType type, Uint256 hash)
		{
			Type = type;
			Hash = hash;
		}
		public InventoryType Type
		{
			get
			{
				return (InventoryType)_type;
			}
			set
			{
				_type = (uint)value;
			}
		}
		public Uint256 Hash
		{
			get
			{
				return _hash;
			}
			set
			{
				_hash = value;
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _type);
			stream.ReadWrite(ref _hash);
		}

		#endregion

		public override string ToString()
		{
			return Type.ToString();
		}
	}
}
