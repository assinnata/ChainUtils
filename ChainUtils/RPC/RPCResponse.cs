using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChainUtils.RPC
{
	//{"code":-32601,"message":"Method not found"}
	public class RpcError
	{
		public RpcError(JObject error)
		{
			Code = (RpcErrorCode)((int)error.GetValue("code"));
			Message = (string)error.GetValue("message");
		}
		public RpcErrorCode Code
		{
			get;
			set;
		}

		public string Message
		{
			get;
			set;
		}
	}
	//{"result":null,"error":{"code":-32601,"message":"Method not found"},"id":1}
	public class RpcResponse
	{
		public RpcResponse(JObject json)
		{
			var error = json.GetValue("error") as JObject;
			if(error != null)
			{
				Error = new RpcError(error);
			}
			Result = json.GetValue("result") as JToken;
		}
		public RpcError Error
		{
			get;
			set;
		}

		public JToken Result
		{
			get;
			set;
		}

		public static RpcResponse Load(Stream stream)
		{
			var reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8));
			return new RpcResponse(JObject.Load(reader));
		}

		public void ThrowIfError()
		{
			if(Error != null)
			{
				throw new RpcException(Error.Code, Error.Message, this);
			}
		}
	}
}
