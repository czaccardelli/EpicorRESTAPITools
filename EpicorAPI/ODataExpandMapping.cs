using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorRESTAPITools
{
    public class ODataExpandMapping
    {
        public ODataExpandMapping(string serviceName)
        {
            ServiceName = serviceName;
        }

        public ODataExpandMapping(string serviceName, ODataExpandMapping relative, bool isParent)
        {
            ServiceName = serviceName;
            if (isParent)
            {
                ParentService = relative;
            }
            else
            {
                ChildService = relative;
            }
        }

        public ODataExpandMapping(string serviceName, ODataExpandMapping parent, ODataExpandMapping child)
        {
            ServiceName = serviceName;
            ParentService = parent;
            ChildService = child;
        }

        public string ServiceName { get; }
        public ODataExpandMapping ParentService { get; }
        public ODataExpandMapping ChildService { get; }

        public string GetParentsToString()
        {
            string serviceString = null;
            ODataExpandMapping currentMapping = this;
            while (currentMapping != null)
            {
                serviceString = "/" + currentMapping.ServiceName + serviceString;
                currentMapping = currentMapping.ParentService;
            }
            return serviceString.TrimStart('/');
        }
        public ODataExpandMapping GetTopLevelParent()
        {
            ODataExpandMapping currentMapping = this;
            while (currentMapping.ParentService != null)
            {
                currentMapping = currentMapping.ParentService;
            }
            return currentMapping;
        }
    }
}
