using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;
using System.Text;

namespace Library
{
    public class PageItem : TableEntity
    {
        public string url { get; set; }

        public string title { get; set; }

        public PageItem() { }

        public PageItem(string url, string title)
        {
            this.PartitionKey = "OK";

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(url);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            this.RowKey = sb.ToString();

            this.url = url;
            this.title = title;
        }

    }
}
