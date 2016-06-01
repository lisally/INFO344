using Library;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {


        private CloudQueue robotQueue;
        private CloudQueue sitemapQueue;
        private CloudQueue urlQueue;
        private CloudQueue statusQueue;
        private CloudTable urlTable;
        private CloudTable errorTable;
        private CloudTable countTable;
        private CloudTable listTable;
        private CloudTable titleTable;
        private static Dictionary<string, Tuple<List<string>, DateTime>> cache;

        public Admin()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            robotQueue = queueClient.GetQueueReference("robot-queue");
            robotQueue.CreateIfNotExists();

            sitemapQueue = queueClient.GetQueueReference("sitemap-queue");
            sitemapQueue.CreateIfNotExists();

            urlQueue = queueClient.GetQueueReference("url-queue");
            urlQueue.CreateIfNotExists();

            statusQueue = queueClient.GetQueueReference("status-queue");
            statusQueue.CreateIfNotExists();

            urlTable = tableClient.GetTableReference("urls");

            errorTable = tableClient.GetTableReference("errors");

            countTable = tableClient.GetTableReference("counts");

            listTable = tableClient.GetTableReference("lists");

            titleTable = tableClient.GetTableReference("titles");
        }

        [WebMethod]
        public string start()
        {
            CloudQueueMessage statusMessage = new CloudQueueMessage("start");

            statusQueue.AddMessage(statusMessage);

            CloudQueueMessage robotMessage = new CloudQueueMessage("http://www.cnn.com/robots.txt");

            robotQueue.AddMessage(robotMessage);

            return "Added " + robotMessage.AsString;
        }

        [WebMethod]
        public string stop()
        {
            CloudQueueMessage message = new CloudQueueMessage("stop");

            statusQueue.AddMessage(message);

            return "Stopping Worker Role";
        }


        [WebMethod]
        public string clear()
        {
            robotQueue.Clear();
            sitemapQueue.Clear();
            urlQueue.Clear();
            urlTable.DeleteIfExists();
            errorTable.DeleteIfExists();
            countTable.DeleteIfExists();
            listTable.DeleteIfExists();

            Thread.Sleep(50000);

            return "Successfully Cleared Tables and Queues";
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchUrl(string url)
        {
            string result = "Title Not Found";

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(url);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            string search = sb.ToString();

            TableQuery<PageItem> query1 = new TableQuery<PageItem>()
                .Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "OK"), 
                TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, search))
            );

            var searchList = urlTable.ExecuteQuery(query1).ToList();

            if (urlTable.Exists())
            {
                if (searchList.Count > 0)
                {
                    result = searchList[0].title;
                }

            }

            return new JavaScriptSerializer().Serialize(result);
        }



        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getWorkerStatus()
        {
            string result = "";

            sitemapQueue.FetchAttributes();
            urlQueue.FetchAttributes();

            if (sitemapQueue.ApproximateMessageCount == 0 && urlQueue.ApproximateMessageCount == 0)
            {
                result = "Idle";
            }
            else if (sitemapQueue.ApproximateMessageCount > 0)
            {
                result = "Loading";
            }
            else if (urlQueue.ApproximateMessageCount > 0)
            {
                result = "Crawling";
            }
            return new JavaScriptSerializer().Serialize(result);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getCpuRam()
        {
            List<string> result = new List<string>();

            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            int cpu = (int)cpuCounter.NextValue();
            Thread.Sleep(500);
            cpu = (int)cpuCounter.NextValue();

            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            int ram = (int)ramCounter.NextValue();

            result.Add("" + cpu);
            result.Add("" + ram);

            return new JavaScriptSerializer().Serialize(result);

        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStats()
        {
            List<string> result = new List<string>();
            int indexSize = 0;
            int urlsCrawled = 0;

            if (countTable.Exists())
            {
                TableQuery<Count> query1 = new TableQuery<Count>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "indexSize")
                );

                TableQuery<Count> query2 = new TableQuery<Count>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlsCrawled")
                );


                var indexSizeList = countTable.ExecuteQuery(query1).ToList();
                var urlsCrawledList = countTable.ExecuteQuery(query2).ToList();

                if (indexSizeList.Count > 0)
                {
                    indexSize = indexSizeList[0].count;
                }

                if (urlsCrawledList.Count > 0)
                {
                    urlsCrawled = urlsCrawledList[0].count;
                }
            }
            urlQueue.FetchAttributes();
            int urlQueueSize = (int)urlQueue.ApproximateMessageCount;

            result.Add("" + indexSize);
            result.Add("" + urlsCrawled);
            result.Add("" + urlQueueSize);

            return new JavaScriptSerializer().Serialize(result);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getLists()
        {
            List<string> result = new List<string>();
            string urlList = "";
            string errorList = "";

            TableQuery<List> query1 = new TableQuery<List>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlList")
            );

            TableQuery<List> query2 = new TableQuery<List>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "errorList")
            );

            if (listTable.Exists())
            {
                var listUrlList = listTable.ExecuteQuery(query1).ToList();
                if (listUrlList.Count > 0)
                {
                    urlList = listUrlList[0].list;
                }
                var listErrorList = listTable.ExecuteQuery(query2).ToList();
                if (listErrorList.Count > 0)
                {
                    errorList = listErrorList[0].list;
                }
            }
            result.Add(urlList);
            result.Add(errorList);

            return new JavaScriptSerializer().Serialize(result);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string checkCache(string search)
        {
            List<string> results = new List<string>();
            if (cache == null)
            {
                cache = new Dictionary<string, Tuple<List<string>, DateTime>>();
            }

            if (cache.Count < 100)
            {
                if (cache.ContainsKey(search))
                {
                    if (cache[search].Item2 > DateTime.Now.AddMinutes(-10))
                    {
                        results = cache[search].Item1;
                    }
                    else
                    {
                        results = this.search(search);
                    }
                }
                else
                {
                    results = this.search(search);
                }
            }
            else
            {
                cache = new Dictionary<string, Tuple<List<string>, DateTime>>();
                results = this.search(search);
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> search(string search)
        {
            List<string> results = new List<string>();

            List<InvertedItem> searchResults = new List<InvertedItem>();

            string[] searchArray = search.ToLower().Split(' ');

            string pattern = "^[a-z0-9]+$";
            Regex regex = new Regex(pattern);

            foreach (string w in searchArray)
            {
                string word = "";

                for (int i = 0; i < w.Length; i++)
                {
                    if (regex.IsMatch("" + w[i]))
                    {
                        word += w[i];
                    }
                }

                if (word.Length > 0)
                {
                    TableQuery<InvertedItem> query = new TableQuery<InvertedItem>()
                       .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word)
                   );

                    var searchList = titleTable.ExecuteQuery(query).ToList();

                    if (searchList.Count > 0)
                    {
                        for (int i = 0; i < searchList.Count; i++)
                        {
                            searchResults.Add(searchList[i]);
                        }
                    }
                }
            }

            var searchQuery = searchResults
                .GroupBy(x => x.RowKey)
                .Select(x => new Tuple<string, int, string, string, string>(x.Key, x.ToList().Count(), x.First().title, x.First().url, x.First().date))
                .OrderByDescending(x => x.Item2).ThenByDescending(x => x.Item5)
                .Take(20)
                .ToList();

            for (int i = 0; i < searchQuery.Count(); i++)
            {
                if (results.Count < 60)
                {
                    results.Add(searchQuery[i].Item3);
                    results.Add(searchQuery[i].Item4);
                    results.Add(searchQuery[i].Item5);
                }
            }

            cache[search] = new Tuple<List<string>, DateTime>(results, DateTime.Now);

            return results;
        }
    }
}
