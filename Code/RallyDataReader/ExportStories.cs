using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

using System.Diagnostics;

namespace RallyDataReader
{
    public class ExportStories : IExportAssets
    {
        public ExportStories(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.StoryExportFilePrefix + "_*.xml");
            foreach (string file in files)
            {
                Debug.WriteLine("Processing File = " + file);
                
                assetCounter += ProcessExportFile(file);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildStoryInsertStatement();
            int assetCounter = 0;

            XDocument xmlDoc = XDocument.Load(FileName);
            var assets = from asset in xmlDoc.Root.Elements("HierarchicalRequirement") select asset;

            foreach (var asset in assets)
            {
                //Determine if story is actually an epic.
                if (System.Convert.ToInt32(asset.Element("DirectChildrenCount").Value) > 0) continue;

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetNumber", asset.Element("FormattedID").Value);
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@Scope", GetRefValue(asset.Element("Project").Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    cmd.Parameters.AddWithValue("@AssetState", GetStoryState(asset.Element("ScheduleState").Value));
                    cmd.Parameters.AddWithValue("@Status", GetStatus(asset.Element("ScheduleState").Value));

                    if (asset.Descendants("PlanEstimate").Any())
                        cmd.Parameters.AddWithValue("@Estimate", asset.Element("PlanEstimate").Value.Trim());
                    else
                        cmd.Parameters.AddWithValue("@Estimate", DBNull.Value);

                    if (asset.Descendants("Owner").Any())
                        cmd.Parameters.AddWithValue("@Owners", GetMemberOIDFromDB(GetRefValue(asset.Element("Owner").Attribute("ref").Value)));
                    else
                        cmd.Parameters.AddWithValue("@Owners", DBNull.Value);

                    if (asset.Descendants("Iteration").Any())
                        cmd.Parameters.AddWithValue("@Timebox", GetRefValue(asset.Element("Iteration").Attribute("ref").Value));
                    else
                        cmd.Parameters.AddWithValue("@Timebox", DBNull.Value);

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

                    if (asset.Descendants("Package").Any())
                        cmd.Parameters.AddWithValue("@Parent", asset.Element("Package").Value);
                    else
                        cmd.Parameters.AddWithValue("@Parent", DBNull.Value);

                    if (System.Convert.ToInt32(asset.Element("Defects").Element("Count").Value) > 0)
                        cmd.Parameters.AddWithValue("@AffectedByDefects", GetRefValueList(asset.Element("Defects"), "Defect", "ref"));
                    else
                        cmd.Parameters.AddWithValue("@AffectedByDefects", DBNull.Value);

                    if (System.Convert.ToInt32(asset.Element("Predecessors").Element("Count").Value) > 0)
                        cmd.Parameters.AddWithValue("@Dependencies", GetRefValueList(asset.Element("Predecessors"), "HierarchicalRequirement", "ref"));
                    else
                        cmd.Parameters.AddWithValue("@Dependencies", DBNull.Value);

                    if (System.Convert.ToInt32(asset.Element("Successors").Element("Count").Value) > 0)
                        cmd.Parameters.AddWithValue("@Dependants", GetRefValueList(asset.Element("Successors"), "HierarchicalRequirement", "ref"));
                    else
                        cmd.Parameters.AddWithValue("@Dependants", DBNull.Value);

                    //CUSTOM FIELDS
                    if (asset.Descendants("c_PDXTeam").Any())
                        cmd.Parameters.AddWithValue("@Team", asset.Element("c_PDXTeam").Value);
                    else if (asset.Descendants("c_Teamname").Any())
                        cmd.Parameters.AddWithValue("@Team", asset.Element("c_Teamname").Value);
                    else
                        cmd.Parameters.AddWithValue("@Team", DBNull.Value);
                    
                    if (asset.Element("Tags").Element("Count").Value != "0")
                    {
                        string tags = GetTagsList(asset.Element("Tags"));
                        CreateCustomField(asset.Element("ObjectID").Value, "Tags", "Text", tags);
                    }

                    if (asset.Descendants("c_Theme").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_Theme").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_Theme", "Text", asset.Element("c_Theme").Value);
                    }

                    if (asset.Descendants("c_AcceptanceCriteria").Any())
                    {
                        if (string.IsNullOrEmpty(asset.Element("c_AcceptanceCriteria").Value) == false)
                            CreateCustomField(asset.Element("ObjectID").Value, "c_AcceptanceCriteria", "Text", asset.Element("c_AcceptanceCriteria").Value);
                    }
                    
                    //ATTACHMENTS: Hack for Tripwire Chould be refactored into its own class.
                    if (System.Convert.ToInt32(asset.Element("Attachments").Element("Count").Value) > 0)
                        SaveAttachmentRecords(asset.Element("ObjectID").Value, "Story", asset.Element("Attachments"));

                    cmd.ExecuteNonQuery();
                }
                assetCounter++;
            }
            return assetCounter;
        }

        //NOTE: Rally data contains no "state" field, so asset state is derived from "ScheduleState" field.
        private string GetStoryState(string State)
        {
            switch (State)
            {
                case "Accepted":
                    return "Closed";
                default:
                    return "Active";
            }
        }

        private string BuildStoryInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO STORIES (");
            sb.Append("AssetOID,");
            sb.Append("AssetNumber,");
            sb.Append("AssetState,");
            sb.Append("Name,");
            sb.Append("Scope,");
            sb.Append("Owners,");
            sb.Append("Super,");
            sb.Append("Description,");
            sb.Append("Estimate,");
            sb.Append("Status,");
            sb.Append("Team,");
            sb.Append("Parent,");
            sb.Append("AffectedByDefects,");
            sb.Append("Dependencies,");
            sb.Append("Dependants,");
            sb.Append("Timebox) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetNumber,");
            sb.Append("@AssetState,");
            sb.Append("@Name,");
            sb.Append("@Scope,");
            sb.Append("@Owners,");
            sb.Append("@Super,");
            sb.Append("@Description,");
            sb.Append("@Estimate,");
            sb.Append("@Status,");
            sb.Append("@Team,");
            sb.Append("@Parent,");
            sb.Append("@AffectedByDefects,");
            sb.Append("@Dependencies,");
            sb.Append("@Dependants,");
            sb.Append("@Timebox);");
            return sb.ToString();
        }
    }
}
