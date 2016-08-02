//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace WebRole1
{
    using System;
    using System.Configuration;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.Linq;
    using WebRole1.Models;
    using Microsoft.WindowsAzure.ServiceRuntime;
    public class Constant
    {

        private static readonly string DatabaseName = RoleEnvironment.GetConfigurationSettingValue("DatabaseName");
        private static readonly string DataCollectionName = RoleEnvironment.GetConfigurationSettingValue("CollectionName");
        private static readonly string MetricCollectionName = RoleEnvironment.GetConfigurationSettingValue("MetricCollectionName");

        private static readonly ConnectionPolicy ConnectionPolicy = new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https, RequestTimeout = new TimeSpan(1, 0, 0) };

        static DocumentClient client = new DocumentClient(new Uri(RoleEnvironment.GetConfigurationSettingValue("DocumentDBEndpoint")), RoleEnvironment.GetConfigurationSettingValue("DocumentDBKey"));

        DocumentCollection metrixCollection = client
            .CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(DatabaseName))
            .Where(c => c.Id == MetricCollectionName)
            .AsEnumerable()
            .FirstOrDefault();

        public static async Task<IEnumerable<MetrixModel>> GetMetrixAsync()
        {
            #region for single worker role

            /* Commented as per requirement - to add multiple worker roles
            IDocumentQuery<MetrixModel> query = client.CreateDocumentQuery<MetrixModel>(
            UriFactory.CreateDocumentCollectionUri(DatabaseName, MetricCollectionName)).AsDocumentQuery();

            List<MetrixModel> results = new List<MetrixModel>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<MetrixModel>());
            }
            return results;
            */

            #endregion

            string strquery = "SELECT* FROM c";
            IDocumentQuery<MetrixModel> query = client.CreateDocumentQuery<MetrixModel>(UriFactory.CreateDocumentCollectionUri(DatabaseName, MetricCollectionName), strquery).AsDocumentQuery();

            List<MetrixModel> results = new List<MetrixModel>();
            MetrixModel model = new MetrixModel();
            List<MetrixModel> data = new List<MetrixModel>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<MetrixModel>());
            }

            foreach (MetrixModel row in results)
            {
                model.totalDocumentsCreated += row.totalDocumentsCreated;
                model.documentsCreatedPerSecond += row.documentsCreatedPerSecond;
                model.documentsCreatedInLastSecond += row.documentsCreatedInLastSecond;
                model.requestUnitsPerMonth += row.requestUnitsPerMonth;
                model.requestUnitsPerSecond += row.requestUnitsPerSecond;
                model.requestUnitsInLastSecond += row.requestUnitsInLastSecond;
            }
            data.Add(model);
            return data;
        }

        public int GetNumberOfDocumentsInCollection(string StrDatabaseName, string StrMetricCollectionName, string Query)
        {
            //Get no of documents in a metric collection
            //string query= "SELECT* FROM c";
            var documentCount = client.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri(StrDatabaseName, StrMetricCollectionName), Query).ToList();
            return documentCount.Count();
        }

    }
}
