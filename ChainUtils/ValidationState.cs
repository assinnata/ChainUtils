using System;
using System.Collections.Generic;
using ChainUtils.Protocol;

namespace ChainUtils
{
	public class ValidationState
	{
		static readonly uint MaxBlockSize = 1000000;
		static readonly ulong MaxMoney = (ulong)21000000 * (ulong)Money.Coin;
		internal static readonly uint MaxBlockSigops = MaxBlockSize / 50;
		enum ModeState
		{
			ModeValid,   // everything ok
			ModeInvalid, // network rule violation (DoS value may be set)
			ModeError,   // run-time error
		};


		ModeState _mode;
		int _nDoS;
		string _strRejectReason;
		RejectCode _chRejectCode;
		bool _corruptionPossible;

		private readonly Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}
		public ValidationState(Network network)
		{
			_network = network;
			_mode = ModeState.ModeValid;
			_nDoS = 0;
			_corruptionPossible = false;
			CheckProofOfWork = true;
			CheckMerkleRoot = true;
		}

		public bool CheckProofOfWork
		{
			get;
			set;
		}
		public bool CheckMerkleRoot
		{
			get;
			set;
		}
		public
			bool DoS(int level, bool ret = false,
			 RejectCode chRejectCodeIn = 0, string strRejectReasonIn = "",
			 bool corruptionIn = false)
		{
			_chRejectCode = chRejectCodeIn;
			_strRejectReason = strRejectReasonIn;
			_corruptionPossible = corruptionIn;
			if(_mode == ModeState.ModeError)
				return ret;
			_nDoS += level;
			_mode = ModeState.ModeInvalid;
			return ret;
		}

		public bool Invalid(bool ret = false,
				 RejectCode _chRejectCode = 0, string _strRejectReason = "")
		{
			return DoS(0, ret, _chRejectCode, _strRejectReason);
		}
		public bool Error(string strRejectReasonIn = "")
		{
			if(_mode == ModeState.ModeValid)
				_strRejectReason = strRejectReasonIn;
			_mode = ModeState.ModeError;
			return false;
		}

		public bool IsValid
		{
			get
			{
				return _mode == ModeState.ModeValid;
			}
		}
		public bool IsInvalid
		{
			get
			{
				return _mode == ModeState.ModeInvalid;
			}
		}
		public bool IsError
		{
			get
			{
				return _mode == ModeState.ModeError;
			}
		}
		public bool IsInvalidEx(ref int nDoSOut)
		{
			if(IsInvalid)
			{
				nDoSOut = _nDoS;
				return true;
			}
			return false;
		}
		public bool CorruptionPossible()
		{
			return _corruptionPossible;
		}
		public RejectCode RejectCode
		{
			get
			{
				return _chRejectCode;
			}
		}
		//struct GetRejectReason()  { return strRejectReason; }

		public bool CheckTransaction(Transaction tx)
		{
			// Basic checks that don't depend on any context
			if(tx.Inputs.Count == 0)
				return DoS(10, Utils.error("CheckTransaction() : vin empty"),
								 RejectCode.Invalid, "bad-txns-vin-empty");
			if(tx.Outputs.Count == 0)
				return DoS(10, Utils.error("CheckTransaction() : vout empty"),
								 RejectCode.Invalid, "bad-txns-vout-empty");
			// Size limits
			if(tx.ToBytes().Length > MaxBlockSize)
				return DoS(100, Utils.error("CheckTransaction() : size limits failed"),
								 RejectCode.Invalid, "bad-txns-oversize");

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach(var txout in tx.Outputs)
			{
				if(txout.Value < 0)
					return DoS(100, Utils.error("CheckTransaction() : txout.nValue negative"),
									 RejectCode.Invalid, "bad-txns-vout-negative");
				if(txout.Value > MaxMoney)
					return DoS(100, Utils.error("CheckTransaction() : txout.nValue too high"),
									 RejectCode.Invalid, "bad-txns-vout-toolarge");
				nValueOut += (long)txout.Value;
				if(!((nValueOut >= 0 && nValueOut <= (long)MaxMoney)))
					return DoS(100, Utils.error("CheckTransaction() : txout total out of range"),
									 RejectCode.Invalid, "bad-txns-txouttotal-toolarge");
			}

			// Check for duplicate inputs
			var vInOutPoints = new HashSet<OutPoint>();
			foreach(var txin in tx.Inputs)
			{
				if(vInOutPoints.Contains(txin.PrevOut))
					return DoS(100, Utils.error("CheckTransaction() : duplicate inputs"),
									 RejectCode.Invalid, "bad-txns-inputs-duplicate");
				vInOutPoints.Add(txin.PrevOut);
			}

			if(tx.IsCoinBase)
			{
				if(tx.Inputs[0].ScriptSig.Length < 2 || tx.Inputs[0].ScriptSig.Length > 100)
					return DoS(100, Utils.error("CheckTransaction() : coinbase script size"),
									 RejectCode.Invalid, "bad-cb-length");
			}
			else
			{
				foreach(var txin in tx.Inputs)
					if(txin.PrevOut.IsNull)
						return DoS(10, Utils.error("CheckTransaction() : prevout is null"),
										 RejectCode.Invalid, "bad-txns-prevout-null");
			}

			return true;
		}


		public bool CheckBlock(Block block)
		{
			// These are checks that are independent of context
			// that can be verified before saving an orphan block.

			// Size limits

			var root = block.GetMerkleRoot();

			if(block.Transactions.Count == 0 || block.Transactions.Count > MaxBlockSize || block.Length > MaxBlockSize)
				return DoS(100, Error("CheckBlock() : size limits failed"),
								 RejectCode.Invalid, "bad-blk-length");

			// Check proof of work matches claimed amount
			if(CheckProofOfWork && !CheckProofOfWorkCore(block))
				return DoS(50, Error("CheckBlock() : proof of work failed"),
								 RejectCode.Invalid, "high-hash");

			// Check timestamp
			if(block.Header.BlockTime > Now + TimeSpan.FromSeconds(2 * 60 * 60))
				return Invalid(Error("CheckBlock() : block timestamp too far in the future"),
									 RejectCode.Invalid, "time-too-new");

			// First transaction must be coinbase, the rest must not be
			if(block.Transactions.Count == 0 || !block.Transactions[0].IsCoinBase)
				return DoS(100, Error("CheckBlock() : first tx is not coinbase"),
								 RejectCode.Invalid, "bad-cb-missing");
			for(var i = 1 ; i < block.Transactions.Count ; i++)
				if(block.Transactions[i].IsCoinBase)
					return DoS(100, Error("CheckBlock() : more than one coinbase"),
									 RejectCode.Invalid, "bad-cb-multiple");

			// Check transactions
			foreach(var tx in block.Transactions)
				if(!CheckTransaction(tx))
					return Error("CheckBlock() : CheckTransaction failed");

		
			// Check for duplicate txids. This is caught by ConnectInputs(),
			// but catching it earlier avoids a potential DoS attack:
			var uniqueTx = new HashSet<Uint256>();
			for(var i = 0 ; i < block.Transactions.Count ; i++)
			{
				uniqueTx.Add(root.GetLeaf(i).Hash);
			}
			if(uniqueTx.Count != block.Transactions.Count)
				return DoS(100, Error("CheckBlock() : duplicate transaction"),
								 RejectCode.Invalid, "bad-txns-duplicate", true);

			var nSigOps = 0;
			foreach(var tx in block.Transactions)
			{
				nSigOps += GetLegacySigOpCount(tx);
			}
			if(nSigOps > MaxBlockSigops)
				return DoS(100, Error("CheckBlock() : out-of-bounds SigOpCount"),
								 RejectCode.Invalid, "bad-blk-sigops", true);

			// Check merkle root
			if(CheckMerkleRoot && block.Header.HashMerkleRoot != root.Hash)
				return DoS(100, Error("CheckBlock() : hashMerkleRoot mismatch"),
								 RejectCode.Invalid, "bad-txnmrklroot", true);

			return true;
		}

		public bool CheckProofOfWorkCore(Block block)
		{
			var target = block.Header.Bits;
			// Check range
			if(target <= new Target(0) || target > Network.ProofOfWorkLimit)
				return Error("CheckProofOfWork() : nBits below minimum work");

			// Check proof of work matches claimed amount
			if(!block.Header.CheckProofOfWork())
				return Error("CheckProofOfWork() : hash doesn't match nBits");
			return true;

		}
		private int GetLegacySigOpCount(Transaction tx)
		{
			uint nSigOps = 0;
			foreach(var txin in tx.Inputs)
			{
				nSigOps += txin.ScriptSig.GetSigOpCount(false);
			}
			foreach(var txout in tx.Outputs)
			{
				nSigOps += txout.ScriptPubKey.GetSigOpCount(false);
			}
			return (int)nSigOps;
		}




		DateTimeOffset _now;
		public DateTimeOffset Now
		{
			get
			{
				if(_now == default(DateTimeOffset))
					return DateTimeOffset.UtcNow;
				return _now;
			}
			set
			{
				_now = value;
			}
		}
	}
}
