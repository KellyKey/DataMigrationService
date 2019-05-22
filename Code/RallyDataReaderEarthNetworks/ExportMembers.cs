using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Json;
using V1DataCore;
using CsvHelper;
using System.IO;
using NLog;

namespace RallyDataReader
{
    public class ExportMembers : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public struct MemberInfo
        {
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DisplayName { get; set; }
            public string Disabled { get; set; }
            public string Permission { get; set; }
            public string ObjectID { get; set; }
        }

        public ExportMembers(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        public override int Export()
        {
            int assetCounter = 0;

            //TextReader contents = new StringReader(File.ReadAllText(_config.RallySourceConnection.ExportFileDirectory + _config.RallySourceConnection.UserExportFilePrefix + ".csv"));
            //var csv = new CsvReader(contents);
            //var users = csv.GetRecords<MemberInfo>();
            //string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.UserExportFilePrefix + "*.json");
            string[] files = Directory.GetFiles(_config.RallySourceConnection.ExportFileDirectory, _config.RallySourceConnection.DefectExportFilePrefix + "*.json");

            //foreach (MemberInfo member in users)
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
            string SQL = BuildMemberInsertStatement();
            int assetCounter = 0;

            //XDocument xmlDoc = XDocument.Load(FileName);
            var xmlDoc = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(
                Encoding.ASCII.GetBytes(FileName), new XmlDictionaryReaderQuotas()));

            var assets = from asset in xmlDoc.Root.Elements("Defect") select asset;

            foreach (var asset in assets)
            {

                _logger.Info("Object ID: " + asset.Element("ObjectID").Value);

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    //cmd.Parameters.AddWithValue("@AssetOID", member.ObjectID.Trim());
                    //cmd.Parameters.AddWithValue("@Username", member.UserName.Substring(0, member.UserName.IndexOf("@")));
                    //cmd.Parameters.AddWithValue("@Password", member.UserName.Substring(0, member.UserName.IndexOf("@")));
                    //cmd.Parameters.AddWithValue("@AssetState", GetMemberState(member.Disabled.ToUpper()));
                    //cmd.Parameters.AddWithValue("@Email", member.UserName.Trim());
                    //cmd.Parameters.AddWithValue("@Nickname", member.FirstName.Trim() + " " + member.LastName.Trim());
                    //cmd.Parameters.AddWithValue("@Name", member.FirstName.Trim() + " " + member.LastName.Trim());
                    //cmd.Parameters.AddWithValue("@Description", "Imported from Rally on " + DateTime.Now.ToShortDateString() + ".");
                    //cmd.Parameters.AddWithValue("@DefaultRole", GetMemberDefaultRole(member.Permission.Trim()));
                    //cmd.Parameters.AddWithValue("@NotifyViaEmail", "False");
                    //cmd.Parameters.AddWithValue("@SendConversationEmails", "False");

                    string currentOwner = String.Empty;
                    if (asset.Element("Owner").Attribute("type").Value != "null")
                    {
                        string ownerID = asset.Element("Owner").Element("_ref").Value;
                        cmd.Parameters.AddWithValue("@AssetOID", GetRefValue(ownerID));
                        currentOwner = asset.Element("Owner").Element("_refObjectName").Value;
                    }
                    else
                    {
                        _logger.Error("ERROR: " + asset.Element("ObjectID").Value);
                        _logger.Info("***No Owner");
                        continue;
                    }

                    cmd.Parameters.AddWithValue("@Username", currentOwner);
                    cmd.Parameters.AddWithValue("@Password", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AssetState", GetMemberState("FALSE"));
                    cmd.Parameters.AddWithValue("@Email", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nickname", currentOwner);
                    cmd.Parameters.AddWithValue("@Name", currentOwner);
                    cmd.Parameters.AddWithValue("@Description", "Imported from Rally on " + DateTime.Now.ToShortDateString() + ".");
                    cmd.Parameters.AddWithValue("@DefaultRole", GetMemberDefaultRole("Workspace User"));
                    cmd.Parameters.AddWithValue("@NotifyViaEmail", "False");
                    cmd.Parameters.AddWithValue("@SendConversationEmails", "False");

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {
                        _logger.Error("ERROR: " + ex.Message + " - " + asset.Element("ObjectID").Value);
                        _logger.Info("***Duplicate");
                        continue;
                    }
                }
                assetCounter++;
            }
            return assetCounter;
        }

        private object GetMemberState(string State)
        {
            switch (State)
            {
                case "FALSE":
                    return "Active";
                case "TRUE":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        private object GetMemberDefaultRole(string Role)
        {
            switch (Role)
            {
                case "Workspace User":
                    return "Role:4"; //V1 Team Member
                case "Subscription Admin":
                    return "Role:2"; //V1 Project Admin
                case "Workspace Admin":
                    return "Role:12"; //V1 Member Admin
                default:
                    return "Role:4";
            }
        }

        private string BuildMemberInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO MEMBERS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Email,");
            sb.Append("Nickname,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("DefaultRole,");
            sb.Append("Username,");
            sb.Append("Password,");
            sb.Append("NotifyViaEmail, ");
            sb.Append("SendConversationEmails) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Email,");
            sb.Append("@Nickname,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@DefaultRole,");
            sb.Append("@Username, ");
            sb.Append("@Password,");
            sb.Append("@NotifyViaEmail, ");
            sb.Append("@SendConversationEmails);");
            return sb.ToString();
        }
    }
}
