using Newtonsoft.Json.Linq;
using System;

namespace EpicorRESTAPITools
{
	public class EpicorAPIException : Exception
	{
		public int HttpStatus { get; }
		public string ReasonPhrase { get; }
		public override string Message { get; }
		public string ErrorType { get; }
		public string RequestPayload { get; }

        public JToken httpStatus { get; set; }
        private JToken reasonPhrase { get; }
        private JToken message { get; }
        private JToken errorType { get; }
        private JToken requestPayload { get; }

		public EpicorAPIException(string responsePayload, string requestPayload)
		{
			JObject jObject = JObject.Parse(responsePayload);
            httpStatus = jObject.SelectToken("HttpStatus");
            reasonPhrase = jObject.SelectToken("ReasonPhrase");
            message = jObject.SelectToken("ErrorMessage");
            errorType = jObject.SelectToken("ErrorType");
            HttpStatus = httpStatus != null ? (int)httpStatus : 500;
            ReasonPhrase = reasonPhrase != null ? (string)reasonPhrase : "";
            Message = message != null ? (string)message : "";
            ErrorType = errorType != null ? (string)errorType : "";
			RequestPayload = requestPayload;
		}
	}
}
