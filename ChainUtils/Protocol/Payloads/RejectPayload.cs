using ChainUtils.DataEncoders;

namespace ChainUtils.Protocol
{

	public enum RejectCode : byte
	{
		Malformed = 0x01,
		Invalid = 0x10,
		Obsolete = 0x11,
		Duplicate = 0x12,
		Nonstandard = 0x40,
		Dust = 0x41,
		Insufficientfee = 0x42,
		Checkpoint = 0x43
	}
	public enum RejectCodeType
	{
		Common,
		Version,
		Transaction,
		Block
	}
#if !PORTABLE
	[Payload("reject")]
	public class RejectPayload : Payload
	{
		VarString _message = new VarString();
		public string Message
		{
			get
			{
				return Encoders.ASCII.EncodeData(_message.GetString(true));
			}
			set
			{
				_message = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}
		byte _code;
		public RejectCode Code
		{
			get
			{
				return (RejectCode)_code;
			}
			set
			{
				_code = (byte)value;
			}
		}

		public RejectCodeType CodeType
		{
			get
			{
				switch(Code)
				{
					case RejectCode.Malformed:
						return RejectCodeType.Common;
					case RejectCode.Obsolete:
						if(Message == "block")
							return RejectCodeType.Block;
						else
							return RejectCodeType.Version;
					case RejectCode.Duplicate:
						if(Message == "tx")
							return RejectCodeType.Transaction;
						else
							return RejectCodeType.Version;
					case RejectCode.Nonstandard:
					case RejectCode.Dust:
					case RejectCode.Insufficientfee:
						return RejectCodeType.Transaction;
					case RejectCode.Checkpoint:
						return RejectCodeType.Block;
					case RejectCode.Invalid:
						if(Message == "tx")
							return RejectCodeType.Transaction;
						else
							return RejectCodeType.Block;
					default:
						return RejectCodeType.Common;
				}
			}
		}

		VarString _reason = new VarString();
		public string Reason
		{
			get
			{
				return Encoders.ASCII.EncodeData(_reason.GetString(true));
			}
			set
			{
				_reason = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		Uint256 _hash;
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

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _message);
			stream.ReadWrite(ref _code);
			stream.ReadWrite(ref _reason);
			if(Message == "tx" || Message == "block")
				stream.ReadWrite(ref _hash);
		}
	}
#endif
}
