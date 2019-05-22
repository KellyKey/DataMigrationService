using System;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using V1DataCore;

namespace RallyDataReader
{
    public class ExportListTypes : IExportAssets
    {
        public ExportListTypes(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        private int listTypeCount = 0;

        public override int Export()
        {
            //EPIC STATUS:
            InsertListType("EpicStatus", "Accepted");
            InsertListType("EpicStatus", "Blessed");
            InsertListType("EpicStatus", "Completed");
            InsertListType("EpicStatus", "Defined");
            InsertListType("EpicStatus", "In-Progress");
            InsertListType("EpicStatus", "Backlog");

            InsertListType("EpicCategory", "Theme");
            InsertListType("EpicCategory", "Initiative");
            InsertListType("EpicCategory", "Feature");

            //STORY/DEFECT STATUS:
            InsertListType("StoryStatus", "Accepted");
            InsertListType("StoryStatus", "Blessed");
            InsertListType("StoryStatus", "Completed");
            InsertListType("StoryStatus", "Defined");
            InsertListType("StoryStatus", "In-Progress");
            InsertListType("StpruStatis", "Backlog");

            //STORY/DEFECT PRIORITY:
            InsertListType("WorkitemPriority", "Blocker");
            InsertListType("WorkitemPriority", "Critical");
            InsertListType("WorkitemPriority", "Major");
            InsertListType("WorkitemPriority", "Minor");
            InsertListType("WorkitemPriority", "Trivial");

            //DEFECT SEVERITY (TYPE): 
            InsertListType("DefectType", "Cosmetic");
            InsertListType("DefectType", "Crash/Data Loss");
            InsertListType("DefectType", "Major Problem");
            InsertListType("DefectType", "Minor Problem");

            //DEFECT RESOLUTION:
            InsertListType("DefectResolution", "Cannot Reproduce");
            InsertListType("DefectResolution", "Code Change");
            InsertListType("DefectResolution", "Converted");
            InsertListType("DefectResolution", "Duplicate");
            InsertListType("DefectResolution", "Fixed");
            InsertListType("DefectResolution", "Incomplete");
            InsertListType("DefectResolution", "Invalid");
            InsertListType("DefectResolution", "Moved");
            InsertListType("DefectResolution", "Other Resolution");
            InsertListType("DefectResolution", "Won't Fix");

            //TASK STATUS:
            InsertListType("TaskStatus", "Completed");
            InsertListType("TaskStatus", "Defined");
            InsertListType("TaskStatus", "In-Progress");

            //TEST STATUS:
            InsertListType("TestStatus", "Blocked");
            InsertListType("TestStatus", "Error");
            InsertListType("TestStatus", "Fail");
            InsertListType("TestStatus", "Inconclusive");
            InsertListType("TestStatus", "Pass");

            //TEST CATEGORY:
            InsertListType("TestCategory", "Acceptance");
            InsertListType("TestCategory", "Functional");
            InsertListType("TestCategory", "Integration");
            InsertListType("TestCategory", "Performance");
            InsertListType("TestCategory", "Regression");
            InsertListType("TestCategory", "Usability");
            InsertListType("TestCategory", "User Interface");

            //REGRESSION TEST STATUS:
            InsertListType("RegressionTestStatus", "Blocked");
            InsertListType("RegressionTestStatus", "Error");
            InsertListType("RegressionTestStatus", "Fail");
            InsertListType("RegressionTestStatus", "Inconclusive");
            InsertListType("RegressionTestStatus", "Pass");

            return listTypeCount;
        }

        private void InsertListType(string ListTypeName, string ListTypeValue)
        {
            string SQL = BuildListTypeInsertStatement();

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.AddWithValue("@AssetOID", ListTypeName + ":" + ListTypeValue.Replace(" ", "").Replace("'", ""));
                cmd.Parameters.AddWithValue("@AssetType", ListTypeName);
                cmd.Parameters.AddWithValue("@AssetState", "Active");
                cmd.Parameters.AddWithValue("@Description", "Imported from Rally on " + DateTime.Now.ToString() + ".");
                cmd.Parameters.AddWithValue("@Name", ListTypeValue);
                cmd.ExecuteNonQuery();
            }
            listTypeCount++;
        }

        private string BuildListTypeInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO LISTTYPES (");
            sb.Append("AssetOID,");
            sb.Append("AssetType,");
            sb.Append("AssetState,");
            sb.Append("Description,");
            sb.Append("Name) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetType,");
            sb.Append("@AssetState,");
            sb.Append("@Description,");
            sb.Append("@Name);");
            return sb.ToString();
        }
    }
}
