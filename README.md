# GoogleBigQuerySupplyCollector
A supply collector designed to connect to Google BigQuery

## Building
Run `dotnet build` in the root project folder

## Testing - manual mode
1) Create a project in Google Cloud Platform https://console.cloud.google.com
2) Create a BigQuery service if necessary at https://console.cloud.google.com/bigquery
3) Create `TESTDATASET` dataset 
4) Create tables `LEADS`, `EMAILS`, `CONTACTS_AUDIT` by uploading appropriate .csv file

![Upload csv](/docs/upload_table.png?raw=true)
5) Go to IAM and add a new Role `BigQueryServiceAccountRole` with these permissions:
- bigquery.datasets.get
- bigquery.jobs.create
- bigquery.jobs.get
- bigquery.jobs.list
- bigquery.jobs.listAll
- bigquery.jobs.update
- bigquery.tables.get
- bigquery.tables.getData
- bigquery.tables.list
- resourcemanager.projects.get

![Create role](/docs/create_role.png?raw=true)

6) Create service account (IAM -> Service Accounts) with a name `collector-service-account`
7) Create and download service account key as a .json file. Save it to `GoogleBigQuerySupplyCollectorTests/service-credentials.json`
8) Assign BigQueryServiceAccountRole to collector-service-account:
Open IAM &amp; Admin page at https://console.cloud.google.com/iam-admin/iam
Choose "Members", `collector-service-account`, and click on edit permissions button.
Add `BigQueryServiceAccountRole` role

![Assign role](/docs/assign_role.png?raw=true)

9) Choose dataset and click on "Share dataset" button. Add BigQueryServiceAccountRole to allow dataset access
![Share dataset](/docs/share_dataset.png?raw=true)

10) Run `dotnet test`

## Known issues
- Disabled data metrics test. GBQ returns 0 in numRows right after table is loaded. Couldn't find a way to make it calculate metrics
- Unable to load 100k records within 15 minutes
