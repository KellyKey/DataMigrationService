using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportPortfolio : IExportAssets
    {
        public ExportPortfolio(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.PortfolioExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file, "Theme");
                assetCounter += ProcessExportFile(file, "Initiative");
                assetCounter += ProcessExportFile(file, "Feature");
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName, string NodeName)
        {
            string SQL = BuildEpicInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements(NodeName) select asset;

            foreach (var asset in assets)
            {

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", "Active");
                    cmd.Parameters.AddWithValue("@Category", NodeName); 
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("FormattedID").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@Scope", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));

                    if (asset.Descendants("Owner").Any())
                        cmd.Parameters.AddWithValue("@Owners", GetMemberOIDFromDB(GetRefValue(asset.Element("Owner").Attribute("ref").Value)));
                    else
                        cmd.Parameters.AddWithValue("@Owners", DBNull.Value);

                    string parentOid = string.Empty;
                    if (asset.Descendants("Parent").Any())
                    {
                        parentOid = GetRefValue(asset.Element("Parent").Attribute("ref").Value);
                    }
                    else
                    {
                        XElement xParent = asset.Element("PortfolioItem");
                        if (xParent != null)
                        {
                            parentOid = GetRefValue(xParent.Attribute("ref").Value);
                        }
                    }
                    if (string.IsNullOrEmpty(parentOid) == false)
                        cmd.Parameters.AddWithValue("@Super", parentOid);
                    else
                        cmd.Parameters.AddWithValue("@Super", DBNull.Value);

                    //ATTACHMENTS: Hack for Tripwire Chould be refactored into its own class.
                    if (System.Convert.ToInt32(asset.Element("Attachments").Element("Count").Value) > 0)
                        SaveAttachmentRecords(asset.Element("ObjectID").Value, "Epic", asset.Element("Attachments"));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private string BuildEpicInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO EPICS (");
            sb.Append("AssetOID,");
            sb.Append("AssetNumber,");
            sb.Append("AssetState,");
            sb.Append("Name,");
            sb.Append("Scope,");
            sb.Append("Owners,");
            sb.Append("Super,");
            sb.Append("Category,");
            sb.Append("Description) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetNumber,");
            sb.Append("@AssetState,");
            sb.Append("@Name,");
            sb.Append("@Scope,");
            sb.Append("@Owners,");
            sb.Append("@Super,");
            sb.Append("@Category,");
            sb.Append("@Description);");
            return sb.ToString();
        }

    }
}
