using System;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;
using NLog;

namespace RallyDataReader
{
    public class ExportTestSteps : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportTestSteps(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            //string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.TestStepExportFilePrefix + "_*.xml");
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.TestStepExportFilePrefix + "*.json");
            foreach (string file in files)
            {
                string jsonFile;

                StreamReader reader = new StreamReader(file);
                try
                {
                    do
                    {
                        jsonFile = reader.ReadToEnd();
                    }
                    while (reader.Peek() != -1);
                }

                catch
                {
                    jsonFile = "File is Empty";
                }

                finally
                {
                    reader.Close();
                }
                Debug.WriteLine("Processing File = " + jsonFile);


                assetCounter += ProcessExportFile(jsonFile);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildTestStepInsertStatement();
            int assetCounter = 0;

            //XDocument xmlDoc = XDocument.Load(FileName);
            var xmlDoc = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(
                Encoding.ASCII.GetBytes(FileName), new XmlDictionaryReaderQuotas()));

            var assets = from asset in xmlDoc.Root.Elements("TestCaseStep") select asset;

            foreach (var asset in assets)
            {
                _logger.Info("Object ID: " + asset.Element("ObjectID").Value);

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@TestCaseOID", GetRefValue(asset.Element("TestCase").Element("_ref").Value));
                    cmd.Parameters.AddWithValue("@ExpectedResult", asset.Element("ExpectedResult").Value);
                    cmd.Parameters.AddWithValue("@Input", asset.Element("Input").Value);
                    cmd.Parameters.AddWithValue("@StepIndex", asset.Element("StepIndex").Value);
                    cmd.Parameters.AddWithValue("@CreateDate", ConvertRallyDate(asset.Element("CreationDate").Value));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private string BuildTestStepInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO TESTSTEPS (");
            sb.Append("AssetOID,");
            sb.Append("TestCaseOID,");
            sb.Append("ExpectedResult,");
            sb.Append("Input,");
            sb.Append("StepIndex,");
            sb.Append("CreateDate) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@TestCaseOID,");
            sb.Append("@ExpectedResult,");
            sb.Append("@Input,");
            sb.Append("@StepIndex,");
            sb.Append("@CreateDate);");
            return sb.ToString();
        }

    }
}
