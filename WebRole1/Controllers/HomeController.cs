using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebRole1.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Text;

namespace WebRole1.Controllers
{
    static class Config
    {
        public static readonly string DatabaseName = RoleEnvironment.GetConfigurationSettingValue("DatabaseName");
        public static readonly string DataCollectionName = RoleEnvironment.GetConfigurationSettingValue("CollectionName");
        public static readonly string MetricCollectionName = RoleEnvironment.GetConfigurationSettingValue("MetricCollectionName");
        public static readonly DocumentClient client = new DocumentClient(new Uri(RoleEnvironment.GetConfigurationSettingValue("DocumentDBEndpoint")), RoleEnvironment.GetConfigurationSettingValue("DocumentDBKey"));
    }

    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public Uri collectionUri;

        public ActionResult Index()
        {
            ViewBag.Message = "";
            ViewBag.Title = "Index";

            return View();
        }

        public ActionResult Introduction()
        {
            ViewBag.Message = "";
            ViewBag.Title = "Introduction";
            return View();
        }

        public ActionResult RealTimeQuery()
        {
            ViewBag.Message = "";
            ViewBag.Title = "RealTimeQuery";
            return View();
        }

        public ActionResult HomeDocDB()
        {
            ViewBag.Message = "";

            return View();
        }

        public JsonResult InsertFiles(int NumberOfDocumentsToInsert, int Throughput)
        {
            string filename = "~/Player.json";
            Dictionary<string, object> sampleDocument = JsonConvert.DeserializeObject<Dictionary<string, object>>(System.IO.File.ReadAllText(Server.MapPath(filename)));
            return Json("Test Data", JsonRequestBehavior.AllowGet);
        }
        //Query that collects the data from metric collection 
        [HttpPost]
        public async Task<ActionResult> GetMetrix()
        {
            //await UpdateCollectionThrouhput(25500);
            IEnumerable<MetrixModel> items = await Constant.GetMetrixAsync();
            return Json(new { items = items, success = true }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<ActionResult> UpdateCollectionThrouhput(int Throughput)
        {
            string result = null;
            var collection = Config.client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(Config.DatabaseName)).Where(c => c.Id == Config.DataCollectionName).AsEnumerable().FirstOrDefault();

            // Read current offer
            Offer offer = (await Config.client.ReadOffersFeedAsync()).Single(o => o.ResourceLink == collection.SelfLink);

            try
            {
                //int Throughput = 14000;
                await Config.client.ReplaceOfferAsync(new OfferV2(offer, Throughput));
                result = "Success";
            }
            catch (DocumentClientException ex)
            {
                Console.WriteLine(ex.Message);
                result = "Fail";
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<ActionResult> GetCurrentThroughput()
        {
            int result = 0;
            try
            {
                var collection = Config.client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(Config.DatabaseName)).Where(c => c.Id == Config.DataCollectionName).AsEnumerable().FirstOrDefault();
                Offer offer = (await Config.client.ReadOffersFeedAsync()).Single(o => o.ResourceLink == collection.SelfLink);
                result = ((OfferV2)offer).Content.OfferThroughput;
            }
            catch (DocumentClientException ex)
            {
                result = -1;
                string errMessage = ex.Message.ToString();
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public static async Task<IEnumerable<QueryModel>> GetQueryData(string Query)
        {
            var query = Config.client.CreateDocumentQuery((UriFactory.CreateDocumentCollectionUri(Config.DatabaseName, Config.MetricCollectionName)), Query, new FeedOptions { MaxItemCount = 100, EnableScanInQuery = true }).AsDocumentQuery<dynamic>();
            
            // List<QueryModel> query = client.CreateDocumentQuery<QueryModel>(
            //UriFactory.CreateDocumentCollectionUri(DatabaseName, DataCollectionName)).Take(100);

            List<QueryModel> results = new List<QueryModel>();
            while (query.HasMoreResults)
            {
                
                results.AddRange(await query.ExecuteNextAsync<QueryModel>());
            }
            return results;
        }
        [HttpPost]
        public ActionResult metricsQuery(string Query, string ContinuationToken = "")
        {
            //ContinuationToken = "-RID:6yVmAM2jPQAtAQAAAAAAAA==#RT:3#TRC:300#PKRID:0";
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(Environment.NewLine);
            try
            {
                IDocumentQuery<dynamic> docQuery = null;
                if (ContinuationToken != "")
                {
                    docQuery = Config.client.CreateDocumentQuery(
                                UriFactory.CreateDocumentCollectionUri(Config.DatabaseName, Config.DataCollectionName),
                                Query,
                                new FeedOptions
                                {
                                    MaxItemCount = 100,
                                    EnableScanInQuery = true,
                                    RequestContinuation = ContinuationToken,
                                    EnableCrossPartitionQuery = true
                                }).AsDocumentQuery<dynamic>();
                }
                else
                {
                    docQuery = Config.client.CreateDocumentQuery(
                                UriFactory.CreateDocumentCollectionUri(Config.DatabaseName, Config.DataCollectionName),
                                Query,
                                new FeedOptions
                                {
                                    MaxItemCount = 100,
                                    EnableScanInQuery = true,
                                    EnableCrossPartitionQuery = true
                                }).AsDocumentQuery<dynamic>();
                }

                var results = docQuery.ExecuteNextAsync().Result;
                while (results.Count == 0 && docQuery.HasMoreResults)
                {
                    results = docQuery.ExecuteNextAsync().Result;
                }

                foreach (dynamic result in results)
                {
                    string json = result.ToString();
                    string formattedJson = formattedJson = (json.StartsWith("{", StringComparison.InvariantCulture) || json.StartsWith("[", StringComparison.InvariantCulture)) ? JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented) : json;
                    sb.Append(formattedJson);
                    sb.Append("," + Environment.NewLine);
                }
                sb.Append("]");

                if (docQuery.HasMoreResults)
                {
                    ContinuationToken = results.ResponseContinuation;
                }
            }
            catch (DocumentClientException ex)
            {
                string str = ex.Message.ToString();
            }
            return Json(new { items = sb.ToString(), ContinuationToken = ContinuationToken }, JsonRequestBehavior.AllowGet);
        }
    }
}
