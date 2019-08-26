using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S2.BlackSwan.SupplyCollector.Models;
using Xunit;

namespace GoogleBigQuerySupplyCollectorTests
{
    public class GoogleBigQuerySupplyCollectorTests
    {
        private readonly GoogleBigQuerySupplyCollector.GoogleBigQuerySupplyCollector _instance;
        public readonly DataContainer _container;

        private string GetProjectId(string filename) {
            using (var file = File.OpenText(filename))
            {
                var reader = new JsonTextReader(file);
                var jObject = JObject.Load(reader);

                return jObject.GetValue("project_id").ToString();
            }
        }

        public GoogleBigQuerySupplyCollectorTests()
        {
            _instance = new GoogleBigQuerySupplyCollector.GoogleBigQuerySupplyCollector();
            _container = new DataContainer()
            {
                ConnectionString = _instance.BuildConnectionString("service-credentials.json", GetProjectId("service-credentials.json"))
            };
        }

        [Fact]
        public void DataStoreTypesTest()
        {
            var result = _instance.DataStoreTypes();
            Assert.Contains("Google BigQuery", result);
        }

        [Fact]
        public void TestConnectionTest()
        {
            var result = _instance.TestConnection(_container);
            Assert.True(result);
        }

        [Fact]
        public void GetTableNamesTest()
        {
            var (tables, elements) = _instance.GetSchema(_container);
            Assert.Equal(3, tables.Count);
            Assert.Equal(156, elements.Count);

            var tableNames = new string[] { "TESTDATASET.LEADS", "TESTDATASET.EMAILS", "TESTDATASET.CONTACTS_AUDIT" };
            foreach (var tableName in tableNames)
            {
                var table = tables.Find(x => x.Name.Equals(tableName));
                Assert.NotNull(table);
            }
        }

        [Fact]
        public void GetDataCollectionMetricsTest()
        {
            var metrics = new DataCollectionMetrics[] {
                new DataCollectionMetrics()
                    {Name = "TESTDATASET.EMAILS", RowCount = 200, TotalSpaceKB = 97.15M},
                new DataCollectionMetrics()
                    {Name = "TESTDATASET.LEADS", RowCount = 200, TotalSpaceKB = 78.53M},
                new DataCollectionMetrics()
                    {Name = "TESTDATASET.CONTACTS_AUDIT", RowCount = 200, TotalSpaceKB = 110.12M},
            };

            var result = _instance.GetDataCollectionMetrics(_container);
            Assert.Equal(metrics.Length, result.Count);

            foreach (var metric in metrics)
            {
                var resultMetric = result.Find(x => x.Name.Equals(metric.Name));
                Assert.NotNull(resultMetric);

                Assert.Equal(metric.RowCount, resultMetric.RowCount);
                Assert.Equal(metric.TotalSpaceKB, resultMetric.TotalSpaceKB, 2);
            }
        }

        [Fact]
        public void CollectSampleTest()
        {
            var entity = new DataEntity("FROM_ADDR", DataType.String, "STRING", _container,
                new DataCollection(_container, "TESTDATASET.EMAILS"));

            var samples = _instance.CollectSample(entity, 2);
            Assert.Equal(2, samples.Count);
        }

    }
}
