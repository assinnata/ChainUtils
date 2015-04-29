using System;

namespace ChainUtils.RPC
{
	public enum RpcErrorCode
	{
		// Standard JSON-RPC 2.0 errors
		RpcInvalidRequest = -32600,
		RpcMethodNotFound = -32601,
		RpcInvalidParams = -32602,
		RpcInternalError = -32603,
		RpcParseError = -32700,

		// General application defined errors
		RpcMiscError = -1, // std::exception thrown in command handling
		RpcForbiddenBySafeMode = -2, // Server is in safe mode, and command is not allowed in safe mode
		RpcTypeError = -3, // Unexpected type was passed as parameter
		RpcInvalidAddressOrKey = -5, // Invalid address or key
		RpcOutOfMemory = -7, // Ran out of memory during operation
		RpcInvalidParameter = -8, // Invalid, missing or duplicate parameter
		RpcDatabaseError = -20, // Database error
		RpcDeserializationError = -22, // Error parsing or validating structure in raw format
		RpcTransactionError = -25, // General error during transaction submission
		RpcTransactionRejected = -26, // Transaction was rejected by network rules
		RpcTransactionAlreadyInChain = -27, // Transaction already in chain

		// P2P client errors
		RpcClientNotConnected = -9, // Bitcoin is not connected
		RpcClientInInitialDownload = -10, // Still downloading initial blocks
		RpcClientNodeAlreadyAdded = -23, // Node is already added
		RpcClientNodeNotAdded = -24, // Node has not been added before

		// Wallet errors
		RpcWalletError = -4, // Unspecified problem with wallet (key not found etc.)
		RpcWalletInsufficientFunds = -6, // Not enough funds in wallet or account
		RpcWalletInvalidAccountName = -11, // Invalid account name
		RpcWalletKeypoolRanOut = -12, // Keypool ran out, call keypoolrefill first
		RpcWalletUnlockNeeded = -13, // Enter the wallet passphrase with walletpassphrase first
		RpcWalletPassphraseIncorrect = -14, // The wallet passphrase entered was incorrect
		RpcWalletWrongEncState = -15, // Command given in wrong wallet encryption state (encrypting an encrypted wallet etc.)
		RpcWalletEncryptionFailed = -16, // Failed to encrypt the wallet
		RpcWalletAlreadyUnlocked = -17, // Wallet is already unlocked
	}


	
	public class RpcException : Exception
	{
		public RpcException(RpcErrorCode code, string message, RpcResponse result)
			: base(String.IsNullOrEmpty(message) ? FindMessage(code) : message)
		{
			_rpcCode = code;
			_rpcCodeMessage = FindMessage(code);
			_rpcResult = result;
		}

		private readonly RpcResponse _rpcResult;
		public RpcResponse RpcResult
		{
			get
			{
				return _rpcResult;
			}
		}

		private static string FindMessage(RpcErrorCode code)
		{
			switch(code)
			{
				case RpcErrorCode.RpcMiscError:
					return "std::exception thrown in command handling";
				case RpcErrorCode.RpcForbiddenBySafeMode:
					return "Server is in safe mode, and command is not allowed in safe mode";
				case RpcErrorCode.RpcTypeError:
					return "Unexpected type was passed as parameter";
				case RpcErrorCode.RpcInvalidAddressOrKey:
					return "Invalid address or key";
				case RpcErrorCode.RpcOutOfMemory:
					return "Ran out of memory during operation";
				case RpcErrorCode.RpcInvalidParameter:
					return "Invalid, missing or duplicate parameter";
				case RpcErrorCode.RpcDatabaseError:
					return "Database error";
				case RpcErrorCode.RpcDeserializationError:
					return "Error parsing or validating structure in raw format";
				case RpcErrorCode.RpcTransactionError:
					return "General error during transaction submission";
				case RpcErrorCode.RpcTransactionRejected:
					return "Transaction was rejected by network rules";
				case RpcErrorCode.RpcTransactionAlreadyInChain:
					return "Transaction already in chain";
				default:
					return code.ToString();
			}
		}

		private readonly RpcErrorCode _rpcCode;
		public RpcErrorCode RpcCode
		{
			get
			{
				return _rpcCode;
			}
		}

		private readonly string _rpcCodeMessage;
		public string RpcCodeMessage
		{
			get
			{
				return _rpcCodeMessage;
			}
		}
	}
}
