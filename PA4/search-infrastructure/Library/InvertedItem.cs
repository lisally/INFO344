using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;
using System.Text;

namespace Library
{
    public class InvertedItem : TableEntity
    {
        public string url { get; set; }

        public string title { get; set; }
        public string date { get; set; }

        public InvertedItem() { }

        public InvertedItem(string word, string url, string title, string date)
        {
            this.PartitionKey = word;

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
            this.date = date;
        }

    }
}
