using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EpicorRESTAPITools
{
    public static class ODataHelper
    {
        public static string CreateODataExpandOptionsString(object obj, string topLevelService)
        {
            string expandString = null;
            List<string> expandStrings = new List<string>();
            //get distinct attribute values for building OData query options string
            List<LookupAttribute> lookupAttributes = new List<LookupAttribute>();
            foreach (PropertyInfo headerProp in obj.GetType().GetProperties())
            {
                List<LookupAttribute> propLookups = headerProp.GetCustomAttributes<LookupAttribute>().ToList();
                lookupAttributes.AddRange(propLookups);
            }
            List<PropertyInfo> collectionProps = obj.GetType().GetProperties()
                .Where(x => x.PropertyType.GetInterface("IEnumerable") != null && x.PropertyType != typeof(string)).ToList();
            foreach(PropertyInfo collectionProp in collectionProps)
            {
                foreach (PropertyInfo lineProp in collectionProp.PropertyType.GetGenericArguments().First().GetProperties())
                {
                    List<LookupAttribute> propLookups = lineProp.GetCustomAttributes<LookupAttribute>().ToList();
                    lookupAttributes.AddRange(propLookups);
                }
            }
            lookupAttributes = lookupAttributes.Distinct().ToList();
            foreach (LookupAttribute attr in lookupAttributes)
            {
                if (attr.ODataMapping.GetTopLevelParent().ServiceName == topLevelService)
                {
                    expandStrings.Add(attr.ODataMapping.GetParentsToString());
                }
            }
            expandString = expandStrings.OrderByDescending(x => x.Length).First(); //gets longest string which should always be the deepest expand option required
            return expandString.Substring(topLevelService.Length, expandString.Length - topLevelService.Length).TrimStart('/'); //remove highest level service from expand string
        }

        public static string CreateODataSelectOptionsString(object obj, string topLevelService)
        {
            string selectString = null;
            List<string> selectOptions = new List<string>();
            List<PropertyInfo> props = obj.GetType().GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(LookupAttribute)) != null).ToList();
            List<PropertyInfo> collectionProps = obj.GetType().GetProperties()
                .Where(x => x.PropertyType.GetInterface("IEnumerable") != null && x.PropertyType != typeof(string)).ToList();
            foreach(PropertyInfo collectionProp in collectionProps)
            {
                props.AddRange(collectionProp.PropertyType.GetGenericArguments().First().GetProperties()
                    .Where(y => y.GetCustomAttributes(typeof(LookupAttribute)) != null));
            }
            foreach (PropertyInfo prop in props)
            {
                List<Attribute> lookupAttributes = prop.GetCustomAttributes(typeof(LookupAttribute)).ToList();
                foreach (LookupAttribute attr in lookupAttributes)
                {
                    if (attr.ODataMapping.GetTopLevelParent().ServiceName == topLevelService)
                    {
                        string key = null;
                        if (attr.AltKey == null)
                        {
                            key = prop.Name;
                        }
                        else
                        {
                            key = attr.AltKey;
                        }
                        string strToAdd = $"{attr.ODataMapping.GetParentsToString()}/{key}";
                        strToAdd = strToAdd.Substring(topLevelService.Length, strToAdd.Length - topLevelService.Length).TrimStart('/'); //substring removes topLevelService
                        selectOptions.Add(strToAdd);
                    }
                }
            }
            foreach (string selStr in selectOptions.Distinct().ToList())
            {
                selectString += selStr + ",";
            }
            return selectString.TrimEnd(',');
        }

        public static string CreateODataFilterString(object obj, string topLevelService)
        {
            string filterString = null;
            string andOperator = " and ";
            Dictionary<string, string> filters = new Dictionary<string, string>();
            List<PropertyInfo> props = obj.GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(FilterValueAttribute)) != null).ToList();
            List<PropertyInfo> collectionProps = obj.GetType().GetProperties()
                .Where(x => x.PropertyType.GetInterface("IEnumerable") != null && x.PropertyType != typeof(string)).ToList();
            foreach (PropertyInfo collectionProp in collectionProps)
            {
                props.AddRange(collectionProp.PropertyType.GetGenericArguments().First().GetProperties()
                    .Where(y => y.GetCustomAttributes(typeof(LookupAttribute)) != null));
            }
            foreach (PropertyInfo prop in props)
            {
                List<Attribute> filterAttrs = prop.GetCustomAttributes(typeof(FilterValueAttribute)).ToList();
                foreach (FilterValueAttribute filterAttr in filterAttrs)
                {
                    if (filterAttr.ServiceName == topLevelService)
                    {
                        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool))
                        {
                            filters.Add(filterAttr.ServiceEntity.PrimaryKey, prop.GetValue(obj).ToString());
                        }
                        else
                        {
                            filters.Add(filterAttr.ServiceEntity.PrimaryKey, $"%27{prop.GetValue(obj).ToString()}%27"); //add single quotes in case of non-int, non-bool value
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, string> filter in filters.Distinct())
            {
                filterString += $"{filter.Key} eq {filter.Value}{andOperator}";
            }
            return filterString.Substring(0, filterString.Length - andOperator.Length); //return substring consisting of all filters values without final 'AND' operator
        }
    }
}
