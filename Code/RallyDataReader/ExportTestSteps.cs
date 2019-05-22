using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportTestSteps : IExportAssets
    {
        public ExportTestSteps(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.TestStepExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildTestStepInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("TestCaseStep") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@TestCaseOID", GetRefValue(asset.Element("TestCase").Attribute("ref").Value));
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
