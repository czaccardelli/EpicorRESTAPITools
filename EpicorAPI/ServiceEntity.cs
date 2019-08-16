namespace EpicorRESTAPITools
{
    public class ServiceEntity //TODO: change PrimaryKey and Key into collection objects to support cases where more than one primary key is necessary (e.g. CustomerPartXRef)
    {
        public string PrimaryKey { get; set; }
        public string Key { get; set; }
        public string EntityService { get; set; }
        public string PropertyName { get; set; }
    }
}
