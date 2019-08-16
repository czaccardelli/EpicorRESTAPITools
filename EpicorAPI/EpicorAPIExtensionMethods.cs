using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EpicorRESTAPITools
{
    public static class EpicorAPIExtensionMethods
    {
        public static string ToJsonString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd" + "T" + "HH:mm:ss");
        }
        public static string ToJsonStringDateOnly(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd" + "T" + "00:00:00");
        }
    }
}