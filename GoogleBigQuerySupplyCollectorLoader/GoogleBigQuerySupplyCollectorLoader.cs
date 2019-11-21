using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Bigquery.v2.Data;
using Google.Cloud.BigQuery.V2;
using S2.BlackSwan.SupplyCollector.Models;
using SupplyCollectorDataLoader;

namespace GoogleBigQuerySupplyCollectorLoader
{
    public class GoogleBigQuerySupplyCollectorLoader : SupplyCollectorDataLoaderBase
    {
        public override void InitializeDatabase(DataContainer dataContainer) {
            
        }

        public override void LoadSamples(DataEntity[] dataEntities, long count) {
            using (var client =
                GoogleBigQuerySupplyCollector.GoogleBigQuerySupplyCollector.BuildClient(dataEntities[0].Container))
            {

                var dataset = client.GetOrCreateDataset("TESTDATASET");
                var table = dataset.GetTable(dataEntities[0].Collection.Name);
                table?.Delete();

                var fields = new List<TableFieldSchema>();
                fields.Add(new TableFieldSchema() {Name = "Id", Type = "INTEGER" });
                foreach (var dataEntity in dataEntities) {
                    string dataType;
                    switch (dataEntity.DataType) {
                        case DataType.String:
                            dataType = "STRING";
                            break;
                        case DataType.Int:
                            dataType = "INTEGER";
                            break;
                        case DataType.Long:
                            dataType = "INTEGER";
                            break;
                        case DataType.Double:
                            dataType = "FLOAT";
                            break;
                        case DataType.Float:
                            dataType = "FLOAT";
                            break;
                        case DataType.Boolean:
                            dataType = "BOOL";
                            break;
                        case DataType.DateTime:
                            dataType = "DATETIME";
                            break;
                        default:
                            dataType = "INTEGER";
                            break;
                    }

                    fields.Add(new TableFieldSchema() {Name = dataEntity.Name, Type = dataType});
                }

                table = dataset.CreateTable(dataEntities[0].Collection.Name, new TableSchema() {
                    Fields = fields
                });

                long rows = 0;
                var r = new Random();

                while (rows < count) {
                    if (rows % 1000 == 0) {
                        Console.Write(".");
                    }

                    var row = new BigQueryInsertRow();
                    row["Id"] = rows;

                    foreach (var dataEntity in dataEntities) {
                        object val;

                        switch (dataEntity.DataType)
                        {
                            case DataType.String:
                                val = new Guid().ToString();
                                break;
                            case DataType.Int:
                                val = r.Next();
                                break;
                            case DataType.Double:
                                val = r.NextDouble();
                                break;
                            case DataType.Boolean:
                                val = r.Next(100) > 50;
                                break;
                            case DataType.DateTime:
                                val = DateTimeOffset
                                    .FromUnixTimeMilliseconds(
                                        DateTimeOffset.Now.ToUnixTimeMilliseconds() + r.Next()).DateTime;
                                break;
                            default:
                                val = r.Next();
                                break;
                        }

                        row[dataEntity.Name] = val;
                    }

                    table.InsertRow(row);

                    rows++;
                }

                Console.WriteLine();
            }
        }

        private void LoadFile(string fileName, BigQueryDataset dataset) {
            using (var reader = new StreamReader($"tests/{fileName}")) {
                var header = reader.ReadLine();
                var columnsNames = header.Split(",");

                var tableName = Path.GetFileNameWithoutExtension(fileName);
                var table = dataset.GetOrCreateTable(tableName, new TableSchema()
                    {
                        Fields = columnsNames.Select(x => new TableFieldSchema() { Name = x, Type = "STRING" }).ToList()
                    });

                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    if (String.IsNullOrEmpty(line))
                        continue;

                    var cells = line.Split(",");

                    var row = new BigQueryInsertRow();

                    for (int i = 0; i < columnsNames.Length && i < cells.Length; i++) {
                        row[columnsNames[i]] = cells[i];
                    }

                    table.InsertRow(row);
                }
            }
        }

        public override void LoadUnitTestData(DataContainer dataContainer) {
            using (var client =
                GoogleBigQuerySupplyCollector.GoogleBigQuerySupplyCollector.BuildClient(dataContainer)) {

                var dataset = client.GetOrCreateDataset("TESTDATASET");

                LoadFile("CONTACTS_AUDIT.CSV", dataset);
                LoadFile("EMAILS.CSV", dataset);
                LoadFile("LEADS.CSV", dataset);
            }
        }
    }
}
