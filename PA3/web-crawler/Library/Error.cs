using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;
using System.Text;

namespace Library
{
    public class Error : TableEntity
    {
        public string url { get; set; }

        public string errorMessage { get; set; }

        public Error() { }

        public Error(string errorType, string url, string message)
        {
            this.PartitionKey = errorType;

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
            this.errorMessage = message;
        }

    }
}
