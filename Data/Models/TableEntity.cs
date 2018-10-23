using Microsoft.WindowsAzure.Storage.Table;

namespace Data.Models
{
    class Entity : TableEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
