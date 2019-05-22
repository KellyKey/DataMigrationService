using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using V1DataCore;
using NLog;

namespace RallyDataReader
{
    public class ExportProjects : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportProjects(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            //string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.ProjectExportFilePrefix + "_*.xml");
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.ProjectExportFilePrefix + "*.json");
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
                assetCounter += ProcessExportFile(jsonFile);
            }
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildProjectInsertStatement();
            int assetCounter = 0;

            //XDocument xmlDoc = XDocument.Load(FileName);
            var xmlDoc = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(
                    Encoding.ASCII.GetBytes(FileName), new XmlDictionaryReaderQuotas()));
            
            var assets = from asset in xmlDoc.Root.Elements("Project") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetProjectState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Schedule", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Parent", GetParentOID(asset.Element("Parent")));
                    cmd.Parameters.AddWithValue("@IsRelease", "FALSE");
                    cmd.Parameters.AddWithValue("@Owner", GetMemberOIDFromDB(GetOwner(asset.Element("Owner"))));
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Description").Value, asset.Element("Notes").Value, "Notes"));
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@BeginDate", ConvertRallyDate(asset.Element("CreationDate").Value));
                    cmd.Parameters.AddWithValue("@Members", DBNull.Value);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }

                    catch(Exception ex)
                    {
                        _logger.Error("ERROR: " + ex.Message + " - " + asset.Element("ObjectID").Value);
                        _logger.Info("***");
                        continue;
                    }
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetProjectState(string State)
        {
            switch (State)
            {
                case "Open":
                    return "Active";
                case "Closed":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        //TO DO: Add support for merging project trees.
        private string GetParentOID(XElement Parent)
        {
            if (Parent.Attribute("type").Value == "null")
            {
                return "Scope:0";
            }
            else
            {
                return GetRefValue(Parent.Element("_ref").Value);
            }
            //return Parent.Attribute("type") != null ? GetRefValue(Parent.Element("_ref").Value) : "Scope:0";
        }

        // MTB - Added to handle nodes with no Owner
        private string GetOwner(XElement Owner)
        {
            if (Owner.Attribute("type").Value == "null")
            {
                return string.Empty;
            }
            else
            {
                return GetRefValue(Owner.Element("_ref").Value);
            }
             
            //return Owner != null ? GetRefValue(Owner.Element("_ref").Value) : string.Empty;
        }

        private string BuildProjectInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO PROJECTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Schedule,");
            sb.Append("Parent,");
            sb.Append("IsRelease,");
            sb.Append("Owner,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("BeginDate,");
            sb.Append("Members) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Schedule,");
            sb.Append("@Parent,");
            sb.Append("@IsRelease,");
            sb.Append("@Owner,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@BeginDate,");
            sb.Append("@Members);");
            return sb.ToString();
        }
    }
}
