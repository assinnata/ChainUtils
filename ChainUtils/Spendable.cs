using System;

namespace ChainUtils
{
	public class Spendable : IBitcoinSerializable
	{
		public Spendable()
		{

		}
		public Spendable(OutPoint output, TxOut txout)
		{
			if(output == null)
				throw new ArgumentNullException("output");
			if(txout == null)
				throw new ArgumentNullException("txout");
			_out = txout;
			_outPoint = output;
		}

		private OutPoint _outPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _outPoint;
			}
		}
		private TxOut _out;
		public TxOut TxOut
		{
			get
			{
				return _out;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _outPoint);
			if(stream.Serializing)
			{
				var compressor = new TxOutCompressor(_out);
				stream.ReadWrite(ref compressor);
			}
			else
			{
				var compressor = new TxOutCompressor();
				stream.ReadWrite(ref compressor);
				_out = compressor.TxOut;
			}
		}

		#endregion


		public override string ToString()
		{
			if(TxOut != null && TxOut.Value != null)
				return TxOut.Value.ToString();
			return "?";
		}

	}
}
