using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using Library;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CloudQueue robotQueue;
        private CloudQueue sitemapQueue;
        private CloudQueue urlQueue;
        private CloudQueue statusQueue;
        private CloudTable urlTable;
        private CloudTable errorTable;
        private CloudTable countTable;
        private CloudTable listTable;
        private List<string> Disallows;
        private HashSet<string> urlSet;
        private int urlsCrawled;
        private int indexSize;
        private List<string> urlList;
        private List<string> errorList;
        private bool run = true;

        public override void Run()
        {
            setUp();
            while (true)
            {
                checkStatus();
                if (run == true)
                {
                    readRobot();
                    readXML();
                    crawlUrls();
                }
            }

        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");

        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Uses Cloud Storage to retrieve or create tables and queues.
        /// Initializes lists and counts.
        /// </summary>
        public void setUp()
        {
            // Retrieves Cloud Storage account from Storage Connection String
            // to initialize tables and queues.
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
            urlTable.CreateIfNotExists();

            errorTable = tableClient.GetTableReference("errors");
            errorTable.CreateIfNotExists();

            countTable = tableClient.GetTableReference("counts");

            listTable = tableClient.GetTableReference("lists");
            listTable.CreateIfNotExists();

            // Reads cnn.com/robot.txt to add disallows to list
            Disallows = new List<string>();
            WebResponse response;
            WebRequest request = WebRequest.Create("http://www.cnn.com/robots.txt");
            response = request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith("disallow: "))
                    {
                        string disallowed = line.Substring(9);
                        Disallows.Add("cnn.com" + disallowed);
                    }
                }
            }
            // Reads bleacherreport.com/robot.txt to add disallows to list
            request = WebRequest.Create("http://bleacherreport.com/robots.txt");
            response = request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith("disallow: "))
                    {
                        string disallowed = line.Substring(9);
                        Disallows.Add("bleacherreport.com" + disallowed);
                    }
                }
            }

            urlSet = new HashSet<string>();
            urlsCrawled = 0;
            indexSize = 0;

            // Retrieves counts from table if exists
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
            else
            {
                countTable.CreateIfNotExists();

                // Inserts new count of urls crawled and table size to count table
                Count count1 = new Count("urlsCrawled", urlsCrawled);
                TableOperation insert1 = TableOperation.InsertOrReplace(count1);
                countTable.Execute(insert1);

                Count count2 = new Count("indexSize", indexSize);
                TableOperation insert2 = TableOperation.InsertOrReplace(count2);
                countTable.Execute(insert2);
            }


            urlList = new List<string>();
            errorList = new List<string>();


            if (listTable.Exists())
            {
                TableQuery<List> query1 = new TableQuery<List>()
               .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlList")
                );

                TableQuery<List> query2 = new TableQuery<List>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "errorList")
                );

                var listUrlList = listTable.ExecuteQuery(query1).ToList();
                if (listUrlList.Count > 0)
                {
                    string urlListString = listUrlList[0].list;
                    string[] urlListArray = urlListString.Split(',');
                    urlList = urlListArray.ToList();
                }

                var listErrorList = listTable.ExecuteQuery(query2).ToList();
                if (listErrorList.Count > 0)
                {
                    string errorListString = listErrorList[0].list;
                    string[] errorListArray = errorListString.Split(',');
                    errorList = errorListArray.ToList();
                } 
            }
            else
            {
                listTable.CreateIfNotExists();

                List list1 = new List("urlList", "");
                List list2 = new List("errorList", "");

                // Inserts empty url and error list to list table
                TableOperation insert3 = TableOperation.InsertOrReplace(list1);
                listTable.Execute(insert3);

                TableOperation insert4 = TableOperation.InsertOrReplace(list2);
                listTable.Execute(insert4);
            }
        }

        /// <summary>
        /// Reads robot.txt file from cnn.com
        /// Adds sitemaps to queue
        /// </summary>
        public void readRobot()
        {
            checkStatus();
            CloudQueueMessage robotMessage = robotQueue.GetMessage();

            if (robotMessage != null)
            {
                string roboturl = robotMessage.AsString;

                WebResponse response;
                WebRequest request = WebRequest.Create(roboturl);
                response = request.GetResponse();

                // Uses stream reader to read robot.txt and adds sitemaps to sitemap queue
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine().ToLower().Trim();
                        if (line.StartsWith("sitemap: http://www.cnn.com/sitemaps") || line.Contains("bleacherreport.com/sitemap/nba.xml"))
                        {
                            string xmlurl = line.Substring(8);
                            CloudQueueMessage newSitemap = new CloudQueueMessage(xmlurl);
                            sitemapQueue.AddMessage(newSitemap);
                        }
                    }
                }
                robotQueue.DeleteMessage(robotMessage);
            }
            
        }

        /// <summary>
        /// Uses XElement to parse sitemaps
        /// Adds messages to sitemap and url queue
        /// </summary>
        public void readXML()
        {
            // Retrieves message from sitemap queue to see if anything needs to be parsed
            CloudQueueMessage sitemapMessage = sitemapQueue.GetMessage();
            checkStatus();

            while (run == true && sitemapMessage != null)
            {
                // If cnn.com sitemap is being parsed, sets xmlns to appropriate schema
                string sitemapUrl = sitemapMessage.AsString;
                XElement sitemapIndex = XElement.Load(sitemapUrl);
                XName sitemap = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName lastmod = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");

                DateTime restrictDate = new DateTime(2016, 3, 1);

                // If bleacherreport.com sitemap is being parsed, sets xmlns to appropriate schema
                if (sitemapIndex.Elements(sitemap).Count() == 0 && sitemapIndex.Elements(url).Count() == 0)
                {
                    sitemap = XName.Get("sitemap", "http://www.google.com/schemas/sitemap/0.9");
                    url = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                    loc = XName.Get("loc", "http://www.google.com/schemas/sitemap/0.9");
                    lastmod = XName.Get("lastmod", "http://www.google.com/schemas/sitemap/0.9");
                }

                // If element is a sitemap of xml items, add to sitemap queue
                checkStatus();
                if (run == true && sitemapIndex.Elements(sitemap).Count() > 0)
                {
                    foreach (var sitemapElement in sitemapIndex.Elements(sitemap))
                    {
                        var locElement = sitemapElement.Element(loc);
                        var lastmodElement = sitemapElement.Element(lastmod);

                        // Adds cnn sitemaps to queue if they are not older that 3/1/16
                        if (lastmodElement.Value != null)
                        {
                            DateTime modifiedDate = DateTime.Parse(lastmodElement.Value);

                            if (modifiedDate > restrictDate)
                            {
                                CloudQueueMessage newSitemap = new CloudQueueMessage(locElement.Value);
                                sitemapQueue.AddMessage(newSitemap);
                            }
                        }
                        else
                        {
                            CloudQueueMessage newSitemap = new CloudQueueMessage(locElement.Value);
                            sitemapQueue.AddMessage(newSitemap);
                        }
                    }
                }
                // If element is a sitemap of url items, add to url queue
                else
                {
                    if (run == true)
                    {
                        foreach (var urlElement in sitemapIndex.Elements(url))
                        {
                            var locElement = urlElement.Element(loc);
                            string urlString = locElement.Value;

                            if (!urlSet.Contains(urlString))
                            {
                                urlSet.Add(urlString);
                                CloudQueueMessage newUrl = new CloudQueueMessage(urlString);
                                urlQueue.AddMessage(newUrl);
                            }
                        }
                    }
                }
                sitemapQueue.DeleteMessage(sitemapMessage);
                sitemapMessage = sitemapQueue.GetMessage();
                checkStatus();
            }
        }

        /// <summary>
        /// Uses HTMLAgilityPack to crawl and index urls
        /// Adds url title, href, and date to table
        /// </summary>
        public void crawlUrls()
        {
            CloudQueueMessage urlMessage = urlQueue.GetMessage();

            checkStatus();
            while (run == true && urlMessage != null)
            {
                string url = urlMessage.AsString;
                // Attempts to retrieve 'a hrefs' from cnn and bleacherreport url
                try
                {
                    HtmlWeb htmlWeb = new HtmlWeb();
                    HtmlDocument doc = htmlWeb.Load(url);

                    // Url returns 200 status code
                    if (htmlWeb.StatusCode == HttpStatusCode.OK)
                    {
                        // Url contains 'a href' tags
                        if (doc.DocumentNode.SelectNodes("//a[@href]") != null)
                        {
                            // Adds each href to url queue to crawl
                            foreach (HtmlNode hrefNode in doc.DocumentNode.SelectNodes("//a[@href]"))
                            {
                                string href = hrefNode.Attributes["href"].Value;

                                bool disallowed = false;

                                // Href is absolute
                                if (href.StartsWith("//"))
                                {
                                    href = "http:" + href;
                                }
                                // Href is relative
                                else if (href.StartsWith("/") && href.Length > 1)
                                {
                                    if (url.Contains("bleacherreport.com"))
                                    {
                                        href = "http://" + "bleacherreport.com" + href;
                                    }
                                    else if (url.Contains("cnn.com"))
                                    {
                                        href = "http://" + "www.cnn.com" + href;
                                    }
                                }

                                checkStatus();
                                if (!urlSet.Contains(href) && href.StartsWith("http://"))
                                {
                                    // Href is of cnn or bleacherreport domain
                                    if (href.Contains("http://www.cnn.com/") || href.Contains("http://bleacherreport.com/"))
                                    {
                                        foreach (string disallow in Disallows)
                                        {
                                            if (href.Contains(disallow))
                                            {
                                                disallowed = true;
                                            }
                                        }

                                        if (disallowed == false)
                                        {
                                            urlSet.Add(href);
                                            CloudQueueMessage newUrl = new CloudQueueMessage(href);
                                            urlQueue.AddMessage(newUrl);
                                        }
                                    }
                                }
                            }
                        }

                        checkStatus();

                        // Retrieves the url title and date if exists to store in table
                        HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//head/title");
                        string title = null;
                        if (titleNode != null)
                        {
                            title = titleNode.InnerText.ToString();
                        }
                        HtmlNode dateNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:pubdate']");

                        string date = null;
                        if (dateNode != null)
                        {
                            date = dateNode.Attributes["content"].Value;
                        }
                        string status = htmlWeb.StatusCode.ToString();

                        PageItem newPage = new PageItem(status, url, title, date);

                        // Inserts new page to url table
                        TableOperation insertNewPage = TableOperation.Insert(newPage);
                        urlTable.Execute(insertNewPage);

                        // Increments count for urls crawled and url table size
                        urlsCrawled++;
                        indexSize++;

                        // Retrieves count from table and sets to new value
                        TableQuery<Count> query1 = new TableQuery<Count>()
                             .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "indexSize")
                         );

                        var listIndexSize = countTable.ExecuteQuery(query1).ToList();
                        Count newIndexSize = listIndexSize[0];
                        newIndexSize.count = urlsCrawled;

                        TableQuery<Count> query2 = new TableQuery<Count>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlsCrawled")
                        );

                        var listUrlsCrawled = countTable.ExecuteQuery(query2).ToList();
                        Count newUrlsCrawled = listUrlsCrawled[0];
                        newUrlsCrawled.count = indexSize;

                        // Inserts new count to count table
                        TableOperation insertIndexSize = TableOperation.InsertOrReplace(newIndexSize);
                        countTable.Execute(insertIndexSize);

                        TableOperation insertUrlsCrawled = TableOperation.InsertOrReplace(newUrlsCrawled);
                        countTable.Execute(insertUrlsCrawled);

                        // Adds url to 10 recently crawled urls
                        if (urlList.Count < 10)
                        {
                            urlList.Add(url);
                        }
                        else
                        {
                            urlList.Remove(urlList[0]);
                            urlList.Add(url);
                        }

                        // Retrieves url list and set to new list
                        TableQuery<List> query3 = new TableQuery<List>()
                           .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlList")
                        );
                        var oldUrlList = listTable.ExecuteQuery(query3).ToList();
                        List newUrlList = oldUrlList[0];

                        newUrlList.list = string.Join(",", urlList);

                        // Inserts new list
                        TableOperation insertUrlList = TableOperation.InsertOrReplace(newUrlList);
                        listTable.Execute(insertUrlList);
                    }
                    // Url status code is not equal to 200 (error)
                    else
                    {
                        checkStatus();

                        string error = htmlWeb.StatusCode.ToString();
                        Error newError = new Error("ERROR", url, error);

                        // Inserts new error to error table
                        TableOperation insertOperation = TableOperation.Insert(newError);
                        errorTable.Execute(insertOperation);

                        urlsCrawled++;

                        // Retrieves count of urls crawled
                        TableQuery<Count> query1 = new TableQuery<Count>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "urlsCrawled")
                        );

                        var listUrlsCrawled = countTable.ExecuteQuery(query1).ToList();
                        Count newUrlsCrawled = listUrlsCrawled[0];
                        newUrlsCrawled.count = urlsCrawled;

                        // Inserts incrememted count to count table
                        TableOperation insertUrlsCrawled = TableOperation.InsertOrReplace(newUrlsCrawled);
                        countTable.Execute(insertUrlsCrawled);

                        // Adds error to error list
                        if (errorList.Count < 20)
                        {
                            errorList.Add(url);
                            errorList.Add(error);
                        }
                        else
                        {
                            errorList.Remove(errorList[0]);
                            errorList.Remove(errorList[0]);
                            errorList.Add(url);
                            errorList.Add(error);
                        }

                        // Retrieves error list
                        TableQuery<List> query2 = new TableQuery<List>()
                            .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "errorList")
                        );
                        var oldErrorList = listTable.ExecuteQuery(query2).ToList();
                        List newErrorList = oldErrorList[0];

                        newErrorList.list = string.Join(",", errorList);

                        // Inserts new error list
                        TableOperation insertErrorList = TableOperation.InsertOrReplace(newErrorList);
                        listTable.Execute(insertErrorList);
                    }
                }
                // Exception is caught
                catch (Exception ex)
                {
                    string error = ex.Message;

                    Error newError = new Error("CATCH EXCEPTION", url, error);

                    // Inserts new error to the error table
                    if (errorTable.Exists())
                    {
                        TableOperation insertOperation = TableOperation.Insert(newError);
                        errorTable.Execute(insertOperation);
                    }
                }
                urlQueue.DeleteMessage(urlMessage);
                urlMessage = urlQueue.GetMessage();
                checkStatus();
            }
        }
   
        /// <summary>
        /// Checks if Worker Role should run or not
        /// </summary>
        public void checkStatus() {
            // Retrieves message from status queue
            CloudQueueMessage statusMessage = statusQueue.GetMessage();
            if (statusMessage != null)
            {
                // Stops worker role
                if (statusMessage.AsString.Equals("stop"))
                {
                    run = false;
                }
                // Starts worker role
                else if (statusMessage.AsString.Equals("start"))
                {
                    run = true;
                }
                statusQueue.DeleteMessage(statusMessage);
            }
        }
    }
}
