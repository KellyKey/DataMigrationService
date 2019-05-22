using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml.XPath;
using V1DataCore;

namespace RallyDataReader
{
    abstract public class IExportAssets
    {
        protected SqlConnection _sqlConn;
        protected MigrationConfiguration _config;

        public IExportAssets(SqlConnection sqlConn, MigrationConfiguration Configurations)
        {
            _sqlConn = sqlConn;
            _config = Configurations;
        }

        /**************************************************************************************
         * Virtual method that must be implemented in derived classes.
         **************************************************************************************/
        public abstract int Export();

        protected object GetMemberOIDFromDB(string AssetOID)
        {
            string SQL = "SELECT AssetOID FROM Members WHERE AssetOID = '" + AssetOID + "';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            string result = (string)cmd.ExecuteScalar();
            if (String.IsNullOrEmpty(result) == false)
                return result;
            else
                return DBNull.Value;
        }

        protected object GetCombinedDescription(string FirstString, string SecondString, string SecondStringHeader)
        {
            if (String.IsNullOrEmpty(FirstString) == true && String.IsNullOrEmpty(SecondString) == true)
                return DBNull.Value;
            else if (String.IsNullOrEmpty(FirstString) == false && String.IsNullOrEmpty(SecondString) == true)
                return FirstString;
            else if (String.IsNullOrEmpty(FirstString) == true && String.IsNullOrEmpty(SecondString) == false)
                return SecondString;
            else
                return FirstString + "<br/><br/><b>" + SecondStringHeader + ":</b><br/>" + SecondString;
        }

        protected string ConvertRallyDate(string RallyDate)
        {
            DateTime dtStart = DateTime.Parse(RallyDate);
            return dtStart.ToString();
        }

        protected string GetRefValue(string RefValue)
        {
            string[] segments = RefValue.Split('/');
            return segments[segments.Length - 1];
        }

        protected SqlDataReader GetProjectsFromDB(string ParentOID)
        {
            string SQL = "SELECT * FROM Projects WITH (NOLOCK) WHERE Parent = '" + ParentOID + "';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected SqlDataReader GetAttachmentsFromDB()
        {
            string SQL = "SELECT * FROM Attachments WITH (NOLOCK);";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected string GetStatus(string Status)
        {
            switch (Status)
            {
                case "In Progress":
                    return "In-Progress";
                case "Blessed":
                    return string.Empty;
                case "Backlog":
                    return string.Empty;
                case "Developing":
                    return string.Empty;
                default:
                    return Status;
            }
        }

        protected string GetTagsList(XElement TagsList)
        {
            string listOfTags = string.Empty;

            var tags = from tag in TagsList.XPathSelectElements("_tagsNameArray/Tag") select tag;
            foreach (var tag in tags)
            {
                if (tags.Count() == 1)
                {
                    listOfTags = tag.Element("Name").Value;
                }
                else
                {
                    listOfTags += tag.Element("Name").Value + " , ";
                }
            }

            return listOfTags;
        }
        
        protected string GetRefValueList(XElement ValueList, string ValueName, string RefName)
        {
            string listOfValues = null;

            var values = ValueList.Descendants(ValueName);
            foreach (var value in values)
            {
                if (values.Count() == 1)
                    listOfValues = GetRefValue(value.Attribute(RefName).Value);
                else
                    listOfValues += GetRefValue(value.Attribute(RefName).Value) + ";";
            }
            return listOfValues;
        }

        protected void CreateCustomField(string AssetOID, string FieldName, string FieldType, string FieldValue)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO CUSTOMFIELDS (");
            sb.Append("AssetOID,");
            sb.Append("FieldName,");
            sb.Append("FieldType,");
            sb.Append("FieldValue) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@FieldName,");
            sb.Append("@FieldType,");
            sb.Append("@FieldValue);");

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = sb.ToString();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.AddWithValue("@AssetOID", AssetOID);
                cmd.Parameters.AddWithValue("@FieldName", FieldName);
                cmd.Parameters.AddWithValue("@FieldType", FieldType);
                cmd.Parameters.AddWithValue("@FieldValue", FieldValue);
                cmd.ExecuteNonQuery();
            }
        }

        protected object GetTestSteps(string AssetOID)
        {
            StringBuilder sb = new StringBuilder();

            string SQL = "SELECT * FROM TestSteps WITH (NOLOCK) WHERE TestCaseOID = '" + AssetOID + "' ORDER BY StepIndex ASC;";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                sb.AppendLine("<b>STEP " + sdr["StepIndex"].ToString() + "</b><br/>");
                sb.AppendLine("Input: " + sdr["Input"].ToString() + "<br/>");
                sb.AppendLine("Expected result: " + sdr["ExpectedResult"].ToString() + "<br/><br/>");
            }
            sdr.Close();

            if (String.IsNullOrEmpty(sb.ToString()) == false)
                return sb.ToString();
            else
                return DBNull.Value;
        }

        protected void SaveAttachmentRecords(string ParentAssetOID, string AssetType, XElement Attachments)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ATTACHMENTS (");
            sb.Append("AssetOID,");
            sb.Append("URL,");
            sb.Append("Asset,");
            sb.Append("AssetType) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@URL,");
            sb.Append("@Asset,");
            sb.Append("@AssetType);");

            var assets = from asset in Attachments.Descendants("Attachment") select asset;

            foreach (var asset in assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = sb.ToString();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@AssetOID", GetRefValue(asset.Attribute("ref").Value));
                    cmd.Parameters.AddWithValue("@URL", asset.Attribute("ref").Value);
                    cmd.Parameters.AddWithValue("@Asset", ParentAssetOID);
                    cmd.Parameters.AddWithValue("@AssetType", AssetType);
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}
