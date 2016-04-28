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
    /// Summary description for getQuerySuggestions
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {

        private static Trie trie;
        private string filePath = System.IO.Path.GetTempPath() + "\\data.txt";

        /// <summary>
        /// Downloads the wikipedia text file to a temporary filepath.
        /// </summary>
        /// <returns>success message if wikipedia file is downloaded</returns>
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
            return "success downloading wiki data";

        }

        /// <summary>
        /// Returns the current memory of cloud storage.
        /// </summary>
        /// <returns>memory message as string</returns>
        [WebMethod]
        public string getMemory()
        {
            float mem = (new PerformanceCounter("Memory", "Available MBytes")).NextValue();
            return mem.ToString();
        }

        /// <summary>
        /// Reads the downloaded wikipedia text file line by line and adds titles 
        /// to the Trie data structure. Stops adding titles when there is less than
        /// 50 MB left in the storage.
        /// </summary>
        /// <returns>success message, the last line added to the trie, and current memory in MB</returns>
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
            return"success building trie " + this.getMemory() + " " + lineTest + " " + count;
        }

        /// <summary>
        /// Searches through the Trie data structure to find suggestions
        /// </summary>
        /// <param name="search"></param>
        /// <returns>up to 10 search results as string</returns>
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string search)
        {
            List<string> results = trie.SearchForPrefix(search.ToLower().Trim().Replace(' ', '_'));
            return new JavaScriptSerializer().Serialize(results);
        }

    }
}
