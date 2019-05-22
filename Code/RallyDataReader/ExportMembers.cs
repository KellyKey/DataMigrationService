using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using V1DataCore;
using CsvHelper;
using System.IO;

namespace RallyDataReader
{
    public class ExportMembers : IExportAssets
    {
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
            string SQL = BuildMemberInsertStatement();
            int assetCounter = 0;

            TextReader contents = new StringReader(File.ReadAllText(_config.RallySourceConnection.ExportFileDirectory + _config.RallySourceConnection.UserExportFilePrefix + ".csv"));
            var csv = new CsvReader(contents);
            var users = csv.GetRecords<MemberInfo>();

            foreach (MemberInfo member in users)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;

                    cmd.Parameters.AddWithValue("@AssetOID", member.ObjectID.Trim());
                    cmd.Parameters.AddWithValue("@Username", member.UserName.Substring(0, member.UserName.IndexOf("@")));
                    cmd.Parameters.AddWithValue("@Password", member.UserName.Substring(0, member.UserName.IndexOf("@")));
                    cmd.Parameters.AddWithValue("@AssetState", GetMemberState(member.Disabled.ToUpper()));
                    cmd.Parameters.AddWithValue("@Email", member.UserName.Trim());
                    cmd.Parameters.AddWithValue("@Nickname", member.FirstName.Trim() + " " + member.LastName.Trim());
                    cmd.Parameters.AddWithValue("@Name", member.FirstName.Trim() + " " + member.LastName.Trim());
                    cmd.Parameters.AddWithValue("@Description", "Imported from Rally on " + DateTime.Now.ToShortDateString() + ".");
                    cmd.Parameters.AddWithValue("@DefaultRole", GetMemberDefaultRole(member.Permission.Trim()));
                    cmd.Parameters.AddWithValue("@NotifyViaEmail", "False");
                    cmd.Parameters.AddWithValue("@SendConversationEmails", "False");

                    cmd.ExecuteNonQuery();
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
