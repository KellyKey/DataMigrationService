using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportRegressionTests : IExportAssets
    {
        public ExportRegressionTests(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.RegressionTestExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildRegressionTestInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("TestCase") select asset;

            foreach (var asset in assets)
            {
                //Only process regression tests, all other tests are handled by the ExportTests class.
                if (asset.Element("Type").Value != "Regression") continue;

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("FormattedID").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);

                    //NOTE: Unable to determine asset state from Rally data, so all get set to "Active".
                    cmd.Parameters.AddWithValue("@AssetState", "Active");

                    //Mutiple Rally fields are concatenated for the V1 description (Description + Notes + Objective):
                    string combinedDescription = GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes").ToString();
                    combinedDescription = GetCombinedDescription(combinedDescription, asset.Element("Objective").Value, "Objective").ToString();
                    cmd.Parameters.AddWithValue("@Description", combinedDescription);
                    cmd.Parameters.AddWithValue("@Category", asset.Element("Type").Value);

                    //HACK: For Tripwire export, needs refactoring.
                    cmd.Parameters.AddWithValue("@Reference", "RallyID: " + asset.Element("FormattedID").Value);

                    //Rally LastVerdict contains Pass|Fail data.
                    if (asset.Descendants("LastVerdict").Any())
                        cmd.Parameters.AddWithValue("@Status", asset.Element("LastVerdict").Value);
                    else
                        cmd.Parameters.AddWithValue("@Status", DBNull.Value);

                    if (asset.Descendants("Owner").Any())
                        cmd.Parameters.AddWithValue("@Owners", GetMemberOIDFromDB(GetRefValue(asset.Element("Owner").Attribute("ref").Value)));
                    else
                        cmd.Parameters.AddWithValue("@Owners", DBNull.Value);

                    if (asset.Descendants("Project").Any())
                        cmd.Parameters.AddWithValue("@Scope", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    else
                       cmd.Parameters.AddWithValue("@Scope", DBNull.Value);

                    cmd.Parameters.AddWithValue("@Setup", asset.Element("PreConditions").Value);
                    cmd.Parameters.AddWithValue("@Inputs", asset.Element("ValidationInput").Value);
                    cmd.Parameters.AddWithValue("@Steps", GetTestSteps(asset.Element("ObjectID").Value));
                    cmd.Parameters.AddWithValue("@ExpectedResults", GetCombinedDescription(asset.Element("ValidationExpectedResult").Value, asset.Element("PostConditions").Value, "PostConditions").ToString());

                    //ATTACHMENTS: Hack for Tripwire Chould be refactored into its own class.
                    if (System.Convert.ToInt32(asset.Element("Attachments").Element("Count").Value) > 0)
                        SaveAttachmentRecords(asset.Element("ObjectID").Value, "RegressionTest", asset.Element("Attachments"));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private string BuildRegressionTestInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO REGRESSIONTESTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Scope,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Category,");
            sb.Append("Reference,");
            sb.Append("Steps,");
            sb.Append("Inputs,");
            sb.Append("Setup,");
            sb.Append("ExpectedResults,");
            sb.Append("Status) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Scope,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Category,");
            sb.Append("@Reference,");
            sb.Append("@Steps,");
            sb.Append("@Inputs,");
            sb.Append("@Setup,");
            sb.Append("@ExpectedResults,");
            sb.Append("@Status);");
            return sb.ToString();
        }

    }
}
