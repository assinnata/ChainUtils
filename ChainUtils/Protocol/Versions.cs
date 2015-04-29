namespace ChainUtils.Protocol
{
	//
	// network protocol versioning
	//
	public enum ProtocolVersion : uint
	{
		PROTOCOL_VERSION = 70002,

		// intial proto version, to be increased after version/verack negotiation
		InitProtoVersion = 209,

		// disconnect from peers older than this proto version
		MinPeerProtoVersion = 209,

		// nTime field added to CAddress, starting with this version;
		// if possible, avoid requesting addresses nodes older than this
		CaddrTimeVersion = 31402,

		// only request blocks from nodes outside this range of versions
		NoblksVersionStart = 32000,
		NoblksVersionEnd = 32400,

		// BIP 0031, pong message, is enabled for all versions AFTER this one
		Bip0031Version = 60000,

		// "mempool" command, enhanced "getdata" behavior starts with this version:
		MempoolGdVersion = 60002
	}
}
