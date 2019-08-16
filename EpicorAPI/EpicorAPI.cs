using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMAApp
{
    public class EpicorAPI
    {
        public EpicorAPI(Dictionary<string, NetworkCredential> epicorCredentials, string baseAddress)
        {
            BaseAddress = new Uri(baseAddress);
            EpicorCredentials = new Dictionary<string, NetworkCredential>(epicorCredentials);
        }

        public Uri BaseAddress { get; set; }

        public Dictionary<string, NetworkCredential> EpicorCredentials { get; set; }

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

		public static async Task<string> GetLookupValues(ServiceEntity serviceEntity, string expandOptions = null, string selectOptions = null, string filterKey = null)
		{
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
				HttpClientHandler epicorHandler = new HttpClientHandler() { Credentials = EpicorCredentials["MfgSys"] };
				HttpClient epicorClient = new HttpClient(epicorHandler) { BaseAddress = BaseAddress };
				HttpRequestMessage epicorRequest = new HttpRequestMessage(HttpMethod.Get, //continued on next line
					new Uri($"{serviceEntity.EntityService}?{oDataQueryString}", UriKind.Relative));
				epicorRequest.Headers.Add("Accept", "application/json");
				HttpResponseMessage epicorResponse = await epicorClient.SendAsync(epicorRequest);
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
		}
        public static async Task<string> EpicorRequest(string jsonPayload, Uri endPoint, HttpMethod httpMethod)
        {
            HttpClientHandler epicorHandler = new HttpClientHandler() { Credentials = EpicorCredentials["MfgSys"] }; //TODO: refactor to allow different sites
            HttpClient epicorClient = new HttpClient(epicorHandler);
            HttpRequestMessage epicorRequest = null;
            HttpResponseMessage epicorResponse = null;
            epicorClient.BaseAddress = BaseAddress;
            try
            {
                epicorRequest = new HttpRequestMessage(httpMethod, new Uri(epicorClient.BaseAddress, endPoint));
                epicorRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                epicorResponse = await epicorClient.SendAsync(epicorRequest);
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
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                epicorClient.Dispose();
                epicorRequest.Dispose();
                epicorResponse.Dispose();
            }
        }

		public static async Task<string> PostToEpicor(string jsonPayload, Uri endPoint, NetworkCredential credentials)
		{
            HttpClientHandler epicorHandler = new HttpClientHandler() { Credentials = credentials };
            HttpClient epicorClient = new HttpClient(epicorHandler);
            epicorClient.BaseAddress = BaseAddress;
            HttpRequestMessage epicorRequest = null;
			try
			{
                epicorRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(epicorClient.BaseAddress, endPoint));
				epicorRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
				HttpResponseMessage epicorResponse = await epicorClient.SendAsync(epicorRequest);
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
                throw;
			}
            finally
            {
                epicorClient.Dispose();
                epicorRequest.Dispose();
                epicorHandler.Dispose();
            }
		}
        public static string AddParamsToResponse(string response, object obj)
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
            foreach (PropertyInfo prop in obj.GetType().GetProperties()) //TODO: refactor to avoid using reflection since it bypasses default Json.NET behavior
            {
                if(prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null)
                {
                    ds.AddFirst(new JProperty(prop.Name, prop.GetValue(obj)));
                }
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
    }
}
