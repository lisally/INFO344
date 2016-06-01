using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Library
{
    public class Count : TableEntity
    {
        public int count { get; set; }
        public Count() { }

        public Count(string title, int count)
        {
            this.PartitionKey = title;
            this.RowKey = "0";

            this.count = count;

        }
    }
}
