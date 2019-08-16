using System;


namespace EpicorRESTAPITools
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class FilterValueAttribute : Attribute
    {
        public FilterValueAttribute(string serviceName)
        {
            ServiceName = serviceName;
            ServiceEntity = EpicorAPI.ServiceDictionary[serviceName];
        }

        public string ServiceName { get; set; }

        public ServiceEntity ServiceEntity { get; set; } 
    }
}
