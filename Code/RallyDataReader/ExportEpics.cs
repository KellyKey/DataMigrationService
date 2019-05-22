using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;
using NLog;

namespace RallyDataReader
{
    public class ExportEpics : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
  
        public ExportEpics(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.EpicExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildEpicInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("Feature") select asset;

            foreach (var asset in assets)
            {
                //Determine if story is actually an epic.
                if (System.Convert.ToInt32(asset.Element("DirectChildrenCount").Value) == 0) continue;

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    //cmd.Parameters.AddWithValue("@AssetState", GetEpicState(asset.Element("ScheduleState").Value));
                    cmd.Parameters.AddWithValue("@AssetState", GetEpicState("Active"));
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("FormattedID").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@Scope", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    //cmd.Parameters.AddWithValue("@Status", GetStatus(asset.Element("State").Attribute("refObjectName").Value));
                    cmd.Parameters.AddWithValue("@Status", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Swag", asset.Element("RefinedEstimate").Value);

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
                    _logger.Info("-> Exporting {0} Epic.", asset.Element("ObjectID").Value);

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        //NOTE: Rally data contains no "state" field, so asset state is derived from "ScheduleState" field.
        private string GetEpicState(string State)
        {
            switch (State)
            {
                case "Accepted":
                    return "Closed";
                default:
                    return "Active";
            }
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
            sb.Append("Status,");
            sb.Append("Swag,");
            sb.Append("Description) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetNumber,");
            sb.Append("@AssetState,");
            sb.Append("@Name,");
            sb.Append("@Scope,");
            sb.Append("@Owners,");
            sb.Append("@Super,");
            sb.Append("@Status,");
            sb.Append("@Swag,");
            sb.Append("@Description);");
            return sb.ToString();
        }

    }
}
