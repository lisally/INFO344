using Microsoft.WindowsAzure.Storage.Table;

namespace Library
{
    public class List : TableEntity
    {
        public string list { get; set; }

        public List() { }

        public List(string title, string list)
        {
            this.PartitionKey = title;
            this.RowKey = "0";

            this.list = list;
        }
    }
}
