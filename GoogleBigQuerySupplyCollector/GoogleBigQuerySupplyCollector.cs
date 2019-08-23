using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Bigquery.v2;
using Google.Apis.Bigquery.v2.Data;
using Google.Apis.Services;
using Google.Cloud.BigQuery.V2;
using S2.BlackSwan.SupplyCollector;
using S2.BlackSwan.SupplyCollector.Models;

namespace GoogleBigQuerySupplyCollector
{
    public class GoogleBigQuerySupplyCollector: SupplyCollectorBase
    {
        public override List<string> DataStoreTypes() {
            return (new[] { "Google BigQuery" }).ToList();
        }

        public string BuildConnectionString(string certificateFilePath, string project) {
            return $"Certificate={certificateFilePath};Project={project}";
        }

        private BigQueryClient BuildClient(DataContainer container) {
            string certificateFilePath = null;
            string projectId = null;

            var parts = container.ConnectionString.Split(";");
            foreach (var part in parts) {
                if(String.IsNullOrEmpty(part))
                    continue;

                var keyvalue = part.Split("=");
                if (keyvalue.Length == 2) {
                    if (keyvalue[0].Equals("Certificate", StringComparison.InvariantCultureIgnoreCase))
                    {
                        certificateFilePath = keyvalue[1];
                    }
                    else if (keyvalue[0].Equals("Project", StringComparison.InvariantCultureIgnoreCase))
                    {
                        projectId = keyvalue[1];
                    }
                }
            }
            
            return BigQueryClient.Create(projectId, GoogleCredential.FromFile(certificateFilePath));
        }

        public override List<string> CollectSample(DataEntity dataEntity, int sampleSize) {
            var samples = new List<string>();

            using (var client = BuildClient(dataEntity.Container)) {
                var job = client.CreateQueryJob($"SELECT {dataEntity.Name} FROM {dataEntity.Collection.Name} LIMIT {sampleSize}", null);
                var results = job.GetQueryResults();

                foreach (var result in results) {
                    samples.Add(result[0].ToString());
                }
            }

            return samples;
        }

        public override List<DataCollectionMetrics> GetDataCollectionMetrics(DataContainer container) {
            var metrics = new List<DataCollectionMetrics>();

            using (var client = BuildClient(container))
            {
                var datasets = client.ListDatasets(client.ProjectId);
                foreach (var dataset in datasets)
                {
                    var tables = dataset.ListTables();

                    foreach (var table in tables)
                    {
                        var tblSchema = client.GetTable(table.Reference);

                        metrics.Add(new DataCollectionMetrics() {
                            Name = $"{table.Reference.DatasetId}.{table.Reference.TableId}",
                            RowCount = (long)tblSchema.Resource.NumRows.GetValueOrDefault(),
                            TotalSpaceKB = (decimal)tblSchema.Resource.NumBytes.GetValueOrDefault() / 1024
                        });
                        
                    }
                }
            }

            return metrics;
        }

        private DataType ConvertDataType(string dbDataType) {
            if ("STRING".Equals(dbDataType)) {
                return DataType.String;
            }
            else if ("BYTES".Equals(dbDataType))
            {
                return DataType.ByteArray;
            }
            else if ("INTEGER".Equals(dbDataType))
            {
                return DataType.Long;
            }
            else if ("INT64".Equals(dbDataType))
            {
                return DataType.Long;
            }
            else if ("FLOAT".Equals(dbDataType))
            {
                return DataType.Float;
            }
            else if ("FLOAT64".Equals(dbDataType))
            {
                return DataType.Float;
            }
            else if ("BOOLEAN".Equals(dbDataType))
            {
                return DataType.Boolean;
            }
            else if ("BOOL".Equals(dbDataType))
            {
                return DataType.Boolean;
            }
            else if ("TIMESTAMP".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("DATE".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("TIME".Equals(dbDataType))
            {
                return DataType.DateTime;
            }
            else if ("DATETIME".Equals(dbDataType))
            {
                return DataType.DateTime;
            }

            return DataType.Unknown;
        }

        private void AddFields(DataContainer container, DataCollection collection, List<DataEntity> entities,
            IList<TableFieldSchema> fields, string prefix) {

            foreach (var field in fields)
            {
                if ("RECORD".Equals(field.Type) || "STRUCT".Equals(field.Type)) {
                    AddFields(container, collection, entities, field.Fields, $"{prefix}{field.Name}.");
                }
                else {
                    entities.Add(new DataEntity(
                        $"{prefix}{field.Name}",
                        ConvertDataType(field.Type),
                        field.Type,
                        container,
                        collection
                    ));
                }
            }
        }

        public override (List<DataCollection>, List<DataEntity>) GetSchema(DataContainer container) {
            var collections = new List<DataCollection>();
            var entities = new List<DataEntity>();

            using (var client = BuildClient(container)) {
                var datasets = client.ListDatasets(client.ProjectId);
                foreach (var dataset in datasets) {
                    var tables = dataset.ListTables();

                    foreach (var table in tables) {
                        var collection = new DataCollection(container, $"{table.Reference.DatasetId}.{table.Reference.TableId}");

                        var tblSchema = client.GetTable(table.Reference);
                        AddFields(container, collection, entities, tblSchema.Schema.Fields, "");

                        collections.Add(collection);
                    }
                }
            }

            return (collections, entities);
        }

        public override bool TestConnection(DataContainer container) {
            using (var client = BuildClient(container)) {
                var projects = client.ListProjects();

                foreach (var project in projects) {
                    if (project.ProjectId.Equals(client.ProjectId)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
