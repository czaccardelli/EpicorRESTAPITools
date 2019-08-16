using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EpicorRESTAPITools
{
    public class EpicorAPI : IDisposable
    {
        public EpicorAPI(NetworkCredential epicorCredentials, string baseAddress)
        {
            BaseAddress = new Uri(baseAddress);
            EpicorCredentials = epicorCredentials;
            EpicorClientHandler = new HttpClientHandler() { Credentials = EpicorCredentials };
            EpicorClient = new HttpClient(EpicorClientHandler);
            EpicorClient.BaseAddress = BaseAddress;
        }

        public EpicorAPI(string userName, string password, string baseAddress)
        {
            BaseAddress = new Uri(baseAddress);
            EpicorCredentials = new NetworkCredential(userName, password);
            EpicorClientHandler = new HttpClientHandler() { Credentials = EpicorCredentials };
            EpicorClient = new HttpClient(EpicorClientHandler);
            EpicorClient.BaseAddress = BaseAddress;
        }
        public EpicorAPI(string userName, SecureString password, string baseAddress)
        {
            BaseAddress = new Uri(baseAddress);
            EpicorCredentials = new NetworkCredential(userName, password);
            EpicorClientHandler = new HttpClientHandler() { Credentials = EpicorCredentials };
            password.Dispose();
            EpicorClient.BaseAddress = BaseAddress;
        }

        private bool disposed = false;

        public HttpClient EpicorClient { get; set; }

        public HttpClientHandler EpicorClientHandler { get; set; }

        public Uri BaseAddress { get; set; }

        public NetworkCredential EpicorCredentials { get; set; }

        public static readonly Dictionary<string, ServiceEntity> ServiceDictionary = new Dictionary<string, ServiceEntity>()
		{
			{"POes", new ServiceEntity { PrimaryKey="PONum",  EntityService="Erp.BO.POSvc/POes", Key="PONum" } },
			{"CustomerPartXRef", new ServiceEntity {PrimaryKey="PartNum",  EntityService="Erp.BO.CustomerPartXRefSvc/CustomerPartXRefs", Key="PartNum" } },
			{"Part", new ServiceEntity {PrimaryKey="PartNum", EntityService="Erp.BO.PartSvc/Parts", Key="PartNum"} },
			{"DropShips", new ServiceEntity {PrimaryKey = "PONum", EntityService="Erp.BO.DropShipSvc/DropShips", Key="PONum"} },
            {"JobEntries", new ServiceEntity {PrimaryKey="JobNum", EntityService="Erp.BO.JobEntrySvc/JobEntries", Key="JobNum"} }
		};

		public static readonly ODataExpandMapping Part = new ODataExpandMapping("Part");
		public static readonly ODataExpandMapping CustomerPartXRef = new ODataExpandMapping("CustomerPartXRef");
		public static readonly ODataExpandMapping PO = new ODataExpandMapping("POes", PODetails, false);
		public static readonly ODataExpandMapping PODetails = new ODataExpandMapping("PODetails", PO, PORels);
		public static readonly ODataExpandMapping PORels = new ODataExpandMapping("PORels", PODetails, true);
		public static readonly ODataExpandMapping DropShips = new ODataExpandMapping("DropShips", DropShipDtls, false);
		public static readonly ODataExpandMapping DropShipDtls = new ODataExpandMapping("DropShipDtls", DropShips, true);

		public static readonly Dictionary<string, ODataExpandMapping> ODataMappings = new Dictionary<string, ODataExpandMapping>()
		{
			{ PO.ServiceName, PO },
			{ PODetails.ServiceName, PODetails},
			{ PORels.ServiceName, PORels},
			{ CustomerPartXRef.ServiceName, CustomerPartXRef },
			{ Part.ServiceName, Part },
			{ DropShips.ServiceName, DropShips },
			{ DropShipDtls.ServiceName, DropShipDtls }
		};

		public class EpicorAPIPayload<T>
		{
			[JsonProperty("value")]
			public IEnumerable<T> json_value { get; set; }

			[JsonProperty("odata.metadata")]
			public string metadata { get; set; }
		}

		public async Task<string> GetLookupValues(ServiceEntity serviceEntity, string expandOptions = null, string selectOptions = null, string filterKey = null)
		{
            HttpRequestMessage epicorRequest = null;
            HttpResponseMessage epicorResponse = null;
            string oDataQueryString = null;
			if (!string.IsNullOrEmpty(expandOptions)) //build OData query string depending on if some options are blank or not
			{
				oDataQueryString = "$expand=" + expandOptions;
			}
			if (!string.IsNullOrEmpty(selectOptions))
			{
				if (!string.IsNullOrEmpty(expandOptions))
				{
					oDataQueryString += "&$select=" + selectOptions;
				}
				else
				{
					oDataQueryString += "$select=" + selectOptions;
				}
			}
			if (!string.IsNullOrEmpty(filterKey))
			{
				if (!string.IsNullOrEmpty(expandOptions) || !string.IsNullOrEmpty(selectOptions))
				{
					oDataQueryString += "&$filter=" + filterKey;
				}
				else
				{
					oDataQueryString += "$filter=" + filterKey;
				}
			}
			try
			{
				epicorRequest = new HttpRequestMessage(HttpMethod.Get, //continued on next line
					new Uri($"{serviceEntity.EntityService}?{oDataQueryString}", UriKind.Relative));
				epicorRequest.Headers.Add("Accept", "application/json");
				epicorResponse = await EpicorClient.SendAsync(epicorRequest);
				string contentString = await epicorResponse.Content.ReadAsStringAsync();
				if (epicorResponse.IsSuccessStatusCode)
				{
					return contentString;
				}
				else
				{
					throw new EpicorAPIException(await epicorRequest.Content.ReadAsStringAsync(), await epicorResponse.Content.ReadAsStringAsync());
				}
			}
			catch (EpicorAPIException exp)
			{
				throw exp;
			}
            finally
            {
                epicorRequest.Dispose();
                epicorResponse.Dispose();
            }
		}
        public async Task<string> EpicorRequest(string jsonPayload, Uri endPoint, HttpMethod httpMethod)
        {
            HttpRequestMessage epicorRequest = null;
            HttpResponseMessage epicorResponse = null;
            try
            {
                epicorRequest = new HttpRequestMessage(httpMethod, new Uri(EpicorClient.BaseAddress, endPoint));
                epicorRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                epicorResponse = await EpicorClient.SendAsync(epicorRequest);
                if (epicorResponse.IsSuccessStatusCode)
                {
                    return await epicorResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new EpicorAPIException(await epicorResponse.Content.ReadAsStringAsync(), jsonPayload);
                }
            }
            catch (EpicorAPIException epicorEx)
            {
                throw epicorEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                epicorRequest.Dispose();
                epicorResponse.Dispose();
            }
        }
        public static string AddParamsToResponse(string response, object objParams)
        {
            JObject jResponse = JObject.Parse(response);
            JContainer dsCont = new JArray();
            if(jResponse.SelectToken("['odata.metadata']") != null) //check for POST response to Epicor OData POST request (brackets are to escape . char in JPath expression)
            {
                jResponse.SelectToken("['odata.metadata']").Parent.Remove();
                dsCont.Add(jResponse); //add response to dsCont JArray for proper formatting
            }
            else
            {
                dsCont = GetRawJContainer(response);
            }
            JObject ds = new JObject();
            ds.Add(new JProperty("ds", dsCont));
            JObject dsParams = JObject.FromObject(objParams);
            foreach (JProperty jProp in dsParams.Properties())
            {
                ds.AddFirst(new JProperty(jProp));
            }
            return ds.ToString();
        }
        public static string FormatUpdateRequest(string response)
        {
            JContainer dsCont = GetRawJContainer(response);
            JObject ds = new JObject();
            ds.Add(new JProperty("ds", dsCont));
            return ds.ToString();
        }
        public static string RemoveMetadataFromResponse(string response)
        {
            JObject jResponse = JObject.Parse(response);
            JContainer dsCont = new JArray();
            if (jResponse.SelectToken("['odata.metadata']") != null) //check for POST response to Epicor OData POST request (brackets are to escape . char in JPath expression)
            {
                return jResponse.SelectToken("value").ToString();
            }
            else
            {
                dsCont = GetRawJContainer(response);
                return dsCont.ToString();
            }
        }
        private static JContainer GetRawJContainer(string response)
        {
            JObject jResponse = JObject.Parse(response);
            JContainer dsCont =
                (JContainer)jResponse.SelectToken("parameters.ds") ?? //different types of silly metadata returned by inconsistent Epicor REST API
                (JContainer)jResponse.SelectToken("returnObj") ??
                (JContainer)jResponse.SelectToken("ds") ??
                (JContainer)jResponse;
            return dsCont;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if(disposing)
            {
                EpicorClient.Dispose();
                EpicorClientHandler.Dispose();
            }
            disposed = true;
        }

    }
}
