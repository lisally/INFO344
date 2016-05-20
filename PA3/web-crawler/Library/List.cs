using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
