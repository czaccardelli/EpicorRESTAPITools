using System;
using static RMAApp.EpicorAPI;

namespace RMAApp
{
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class LookupAttribute : Attribute
	{

		public LookupAttribute(string serviceName, string altKey = null)
		{
			ServiceName = serviceName;
			ODataMapping = ODataMappings[serviceName];
			ODataExpandMapping topParent = ODataMapping.GetTopLevelParent();
			ServiceEntity = ServiceDictionary[topParent.ServiceName];
			ServiceUri = new Uri(ServiceEntity.EntityService, UriKind.Relative);
			if (altKey != null) //if alternate key is provided, assign it to ServiceEntity
			{
				AltKey = altKey;
			}
		}

		public Uri ServiceUri { get; set; }

		public ServiceEntity ServiceEntity { get; set; }

		public string ServiceName { get; set; }

		public ODataExpandMapping ODataMapping { get; set; }

		public string AltKey { get; set; }
	}
}
