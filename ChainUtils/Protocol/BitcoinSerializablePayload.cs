namespace ChainUtils.Protocol
{
	public class BitcoinSerializablePayload<T> : Payload where T : IBitcoinSerializable, new()
	{
		public BitcoinSerializablePayload()
		{

		}
		public BitcoinSerializablePayload(T obj)
		{
			_object = obj;
		}
		T _object = new T();
		public T Object
		{
			get
			{
				return _object;
			}
			set
			{
				_object = value;
			}
		}
		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _object);
		}
	}
}
