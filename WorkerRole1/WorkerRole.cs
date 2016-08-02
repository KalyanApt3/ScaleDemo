namespace WorkerRole1
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class WorkerRole : RoleEntryPoint
    {
        private const int MinThreadPoolSize = 100;

        private static readonly bool ShouldDeleteAndRecreateDatabaseAndCollection = bool.Parse(RoleEnvironment.GetConfigurationSettingValue("ShouldDeleteAndRecreateDatabaseAndCollection"));
        private static readonly string DatabaseName = RoleEnvironment.GetConfigurationSettingValue("DatabaseName");
        private static readonly string DataCollectionName = RoleEnvironment.GetConfigurationSettingValue("CollectionName");
        private static readonly string MetricCollectionName = RoleEnvironment.GetConfigurationSettingValue("MetricCollectionName");
        private static readonly int DefaultConnectionLimit = int.Parse(RoleEnvironment.GetConfigurationSettingValue("DegreeOfParallelism"));
        private static readonly string DocumentTemplateFile = RoleEnvironment.GetConfigurationSettingValue("DocumentTemplateFile");
        private static readonly string Endpoint = RoleEnvironment.GetConfigurationSettingValue("DocumentDBEndpoint");
        private static readonly string AuthKey = RoleEnvironment.GetConfigurationSettingValue("DocumentDBKey");
        private static readonly int CollectionThroughput = int.Parse(RoleEnvironment.GetConfigurationSettingValue("CollectionThroughput"));
        private static readonly long TotalNumberOfDocumentsToInsert = long.Parse(RoleEnvironment.GetConfigurationSettingValue("NumberOfDocumentsToInsert"));
        private static readonly int NumberOfThreads = int.Parse(RoleEnvironment.GetConfigurationSettingValue("DegreeOfParallelism"));
        private static readonly string CollectionPartitionKey = RoleEnvironment.GetConfigurationSettingValue("CollectionPartitionKey");
        private static readonly string ClientId = Guid.NewGuid().ToString();
        private static readonly int MetricsCollectionTtl = (int)TimeSpan.FromMinutes(60).TotalSeconds;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private long count = 0;
        private long previousCount = 0;

        private double totalRus = 0;
        private double previousRus = 0;

        private readonly ConnectionPolicy ConnectionPolicy = new ConnectionPolicy
        {
            ConnectionMode = ConnectionMode.Gateway,
            ConnectionProtocol = Protocol.Https,

            MaxConnectionLimit = 1000,

            RetryOptions = new RetryOptions
            {
                MaxRetryAttemptsOnThrottledRequests = 10,
                MaxRetryWaitTimeInSeconds = 60
            }
        };

        private readonly TimeSpan Month = TimeSpan.FromDays(30);
        private readonly Dictionary<string, object> SampleDocument = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(DocumentTemplateFile));
        private readonly Dictionary<int, Dictionary<string, object>> Statistics = new Dictionary<int, Dictionary<string, object>>();
        private readonly ConcurrentDictionary<int, double> requestChargeByThread = new ConcurrentDictionary<int, double>();

        private DocumentClient client;
        private Database database;
        private DocumentCollection dataCollection;
        private DocumentCollection metricsCollection;

        private CancellationToken cancellationToken;

        public override void Run()
        {
            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("stopping ...");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            await this.Initialize();

            List<Task> tasks = new List<Task>();
            tasks.Add(this.StoreStats());
            for (int taskId = 0; taskId < WorkerRole.NumberOfThreads; taskId++)
            {
                tasks.Add(this.InsertDocuments());
            }

            await Task.WhenAll(tasks);
        }

        public async Task Initialize()
        {
            Trace.TraceInformation("initializing ...");

            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;
            ThreadPool.SetMinThreads(MinThreadPoolSize, MinThreadPoolSize);

            this.client = new DocumentClient(new Uri(Endpoint), AuthKey, ConnectionPolicy);

            try
            {
                this.database = client.CreateDatabaseQuery().Where(d => d.Id == DatabaseName).AsEnumerable().FirstOrDefault();
            }
            catch
            {
                this.client = null;
            }

            if (this.database != null && ShouldDeleteAndRecreateDatabaseAndCollection)
            {
                Trace.TraceInformation("deleting database ...");
                await client.DeleteDatabaseAsync(this.database.SelfLink);
            }

            if (this.database == null)
            {
                Trace.TraceInformation("creating database ...");
                this.database = await client.CreateDatabaseAsync(new Database { Id = DatabaseName });
            }
            Trace.TraceInformation("using database " + this.database.Id);

            try
            {
                this.dataCollection = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(DatabaseName)).Where(c => c.Id == DataCollectionName).AsEnumerable().FirstOrDefault();
            }
            catch
            {
                this.dataCollection = null;
            }

            if (this.dataCollection == null)
            {
                Trace.TraceInformation("creating data collection ...");
                DocumentCollection newCollectionSpec = new DocumentCollection { Id = DataCollectionName };
                newCollectionSpec.PartitionKey.Paths.Add(CollectionPartitionKey);

                this.dataCollection = await this.client.CreateDocumentCollectionAsync(this.database.SelfLink, newCollectionSpec, new RequestOptions { OfferThroughput = CollectionThroughput });
            }
            Trace.TraceInformation("using data collection " + this.dataCollection.Id);

            try
            {
                this.metricsCollection = client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(DatabaseName)).Where(c => c.Id == MetricCollectionName).AsEnumerable().FirstOrDefault();
            }
            catch
            {
                this.dataCollection = null;
            }

            if (this.metricsCollection == null)
            {
                Trace.TraceInformation("creating metrics collection ...");
                var collection = new DocumentCollection
                {
                    Id = MetricCollectionName
                };
                await this.client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(DatabaseName), collection, new RequestOptions { OfferThroughput = 5000 });
            }
            Trace.TraceInformation("using metrics collection " + this.dataCollection.Id);
        }

        private async Task InsertDocuments()
        {
            string partitionKeyProperty = dataCollection.PartitionKey.Paths[0].Replace("/", "");
            Dictionary<string, object> documentBody = new Dictionary<string, object>(SampleDocument);

            while (!this.cancellationToken.IsCancellationRequested)
            {
                documentBody["id"] = Guid.NewGuid().ToString();
                documentBody[partitionKeyProperty] = Guid.NewGuid().ToString();

                try
                {
                    ResourceResponse<Document> response = await this.client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DataCollectionName), documentBody);
                    this.ComputeInterlockedStats(response.RequestCharge);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Failed to write {0}. Exception was {1}", JsonConvert.SerializeObject(documentBody), e);
                }
            }
        }

        private async Task StoreStats()
        {
            Trace.TraceInformation("storing metrics ...");

            while (!this.cancellationToken.IsCancellationRequested)
            {
                double documentsCreatedInLastSecond = this.count - this.previousCount;
                double requestUnitsInLastSecond = this.totalRus - this.previousRus;
                double requestUnitsPerMonth = requestUnitsInLastSecond * Month.TotalSeconds;

                Dictionary<string, object> stats = new Dictionary<string, object>();
                stats["id"] = ClientId;
                stats["totalDocumentsCreated"] = this.count;
                stats["documentsCreatedInLastSecond"] = documentsCreatedInLastSecond;
                stats["requestUnitsPerMonth"] = requestUnitsPerMonth;
                stats["requestUnitsInLastSecond"] = requestUnitsInLastSecond;
                //                stats["requestUnitsTotal"] = this.totalRus;

                foreach (var property in stats)
                {
                    Trace.TraceInformation(string.Format("{0} = {1}", property.Key, property.Value));
                }

                try
                {
                    var result = await this.client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, MetricCollectionName), stats);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Updating  metrics document failed with {0}", e);
                }

                this.previousCount = this.count;
                this.previousRus = this.totalRus;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private void ComputeInterlockedStats(double requestCharge)
        {
            Interlocked.Increment(ref this.count);

            double myTotal = this.totalRus;
            double sharedTotal;

            bool updatedTotal = false;
            while (!updatedTotal)
            {
                sharedTotal = Interlocked.CompareExchange(ref this.totalRus, myTotal + requestCharge, myTotal);
                updatedTotal = myTotal != sharedTotal;
            }
        }
    }
}