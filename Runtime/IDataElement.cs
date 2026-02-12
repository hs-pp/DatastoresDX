namespace DatastoresDX.Runtime
{
    public interface IDataElement
    {
        Uid Id { get; }
        string DisplayName { get; }
    }
}