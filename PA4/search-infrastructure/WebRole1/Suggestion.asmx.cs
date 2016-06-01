using Library;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for QuerySuggest
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Suggestion : System.Web.Services.WebService
    {

        private static Trie trie;
        private string filePath = System.IO.Path.GetTempPath() + "\\data.txt";
        private static List<string> stats = new List<string>();

        [WebMethod]
        public string downloadWiki()
        {
            File.Delete(this.filePath);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("wiki-data");
            {
                CloudBlockBlob blob = container.GetBlockBlobReference("wiki-data.txt");

                using (var fileStream = System.IO.File.OpenWrite(this.filePath))
                {
                    blob.DownloadToStream(fileStream);
                }
            }
            return "Successfully Downloaded Wikipedia Page Titles";
        }


        [WebMethod]
        public string buildTrie()
        {
            int count = 0;
            string lineTest = "";

            trie = new Trie();

            using (StreamReader reader = new StreamReader(this.filePath))
            {
                PerformanceCounter counter = new PerformanceCounter("Memory", "Available MBytes");
                while (!reader.EndOfStream)
                {
                    if (count % 1000 == 0)
                    {
                        if (counter.NextValue() < 50)
                        {
                            break;
                        }
                    }
                    string line = reader.ReadLine();
                    trie.AddTitle(line);
                    count++;
                    lineTest = line;
                }
            }
            stats.Clear();
            stats.Add("" + count);
            stats.Add(lineTest);

            return new JavaScriptSerializer().Serialize(stats);
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string search)
        {
            List<string> results = trie.SearchForPrefix(search.ToLower().Trim().Replace(' ', '_'));
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStats()
        {
            return new JavaScriptSerializer().Serialize(stats);
        }
    }
}
