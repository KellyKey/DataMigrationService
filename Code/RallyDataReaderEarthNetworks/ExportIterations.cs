using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Runtime.Serialization.Json;
using System.IO;
using V1DataCore;
using NLog;

namespace RallyDataReader
{
    public class ExportIterations : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportIterations(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;
            //string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.IterationExportFilePrefix + "_*.xml");
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.IterationExportFilePrefix + "*.json");
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
            SetIterationSchedules();
            return assetCounter;
        }

        private int ProcessExportFile(string FileName)
        {
            string SQL = BuildIterationInsertStatement();
            int assetCounter = 0;

            //XDocument xmlDoc = XDocument.Load(FileName);
            var xmlDoc = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(
        Encoding.ASCII.GetBytes(FileName), new XmlDictionaryReaderQuotas()));

            
            var assets = from asset in xmlDoc.Root.Elements("Iteration") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", asset.Element("ObjectID").Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetIterationState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@State", GetIterationState(asset.Element("State").Value));
                    cmd.Parameters.AddWithValue("@Owner", DBNull.Value);
                    //cmd.Parameters.AddWithValue("@Parent", GetParentOID(asset.Element("Parent")));
                    cmd.Parameters.AddWithValue("@Parent", String.Empty);
                    cmd.Parameters.AddWithValue("@Schedule", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", GetCombinedDescription(asset.Element("Notes").Value, asset.Element("Theme").Value, "Theme"));
                    cmd.Parameters.AddWithValue("@Name", asset.Element("Name").Value);
                    cmd.Parameters.AddWithValue("@BeginDate", ConvertRallyDate(asset.Element("StartDate").Value));
                    cmd.Parameters.AddWithValue("@EndDate", ConvertRallyDate(asset.Element("EndDate").Value));

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }

                    catch (Exception ex)
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

        private object GetIterationState(string State)
        {
            switch (State)
            {
                case "Accepted":
                    return "Closed";
                case "Planning":
                    return "Future";
                case "Committed":
                    return "Active";
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

        
        private string BuildIterationInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ITERATIONS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Owner,");
            sb.Append("Parent,");
            sb.Append("Schedule,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("EndDate,");
            sb.Append("BeginDate,");
            sb.Append("State) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Owner,");
            sb.Append("@Parent,");
            sb.Append("@Schedule,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@EndDate,");
            sb.Append("@BeginDate,");
            sb.Append("@State);");
            return sb.ToString();
        }

        private void SetIterationSchedules()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Iterations ");
            sb.Append("SET Iterations.Schedule = Projects.Schedule ");
            sb.Append("FROM Iterations, Projects ");
            sb.Append("WHERE Iterations.Parent = Projects.AssetOID;");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = sb.ToString();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }
    
    }
}
