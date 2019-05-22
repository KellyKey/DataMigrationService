using System;
using System.Data.SqlClient;
using System.Text;
using V1DataCore;

namespace JiraReaderService
{
    public class ExportListTypes : IExportAssets
    {
        public ExportListTypes(SqlConnection sqlConn, MigrationConfiguration Configurations) : base(sqlConn, Configurations) { }

        private int listTypeCount = 0;

        public override int Export()
        {
            //EPIC STATUS:
            InsertListType("EpicStatus", "Open");
            InsertListType("EpicStatus", "Reopened");
            InsertListType("EpicStatus", "In Progress");
            InsertListType("EpicStatus", "Completed");
            InsertListType("EpicStatus", "Resolved");
            InsertListType("EpicStatus", "Closed");

            //STORY/DEFECT STATUS:
            InsertListType("StoryStatus", "Open");
            InsertListType("StoryStatus", "Reopened");
            InsertListType("StoryStatus", "In Progress");
            InsertListType("StoryStatus", "Completed");
            InsertListType("StoryStatus", "Resolved");
            InsertListType("StoryStatus", "Closed");

            //STORY TYPE
            InsertListType("StoryCategory", "Task");

            //TASK STATUS:
            InsertListType("TaskStatus", "Open");
            InsertListType("TaskStatus", "Reopened");
            InsertListType("TaskStatus", "In Progress");
            InsertListType("TaskStatus", "Completed");
            InsertListType("TaskStatus", "Resolved");
            InsertListType("TaskStatus", "Closed");

            //DEFECT REASON
            InsertListType("DefectResolution","Unresolved");
            InsertListType("DefectResolution", "Duplicate");
            InsertListType("DefectResolution", "Fixed");
            InsertListType("DefectResolution", "Won't Fix");
            InsertListType("DefectResolution", "Cannot Reproduce");

            //ISSUE PRIORITY:
            InsertListType("IssuePriority", "Trivial [P3]");
            InsertListType("IssuePriority", "Minor [P2-Med]");
            InsertListType("IssuePriority", "Major [P2-High]");
            InsertListType("IssuePriority", "Critical [P1-Med]");
            InsertListType("IssuePriority", "Blocker [P1-High]");

            //ISSUE STATUS:
            InsertListType("IssueStatus", "Open");
            InsertListType("IssueStatus", "Reopened");
            InsertListType("IssueStatus", "In Progress");
            InsertListType("IssueStatus", "Completed");
            InsertListType("IssueStatus", "Resolved");
            InsertListType("IssueStatus", "Closed");

            //REQUEST PRIORITY:
            InsertListType("RequestPriority", "Trivial [P3]");
            InsertListType("RequestPriority", "Minor [P2-Med]");
            InsertListType("RequestPriority", "Major [P2-High]");
            InsertListType("RequestPriority", "Critical [P1-Med]");
            InsertListType("RequestPriority", "Blocker [P1-High]");

            //REQUEST STATUS:
            InsertListType("RequestStatus", "Open");
            InsertListType("RequestStatus", "Reopened");
            InsertListType("RequestStatus", "In Progress");
            InsertListType("RequestStatus", "Completed");
            InsertListType("RequestStatus", "Resolved");
            InsertListType("RequestStatus", "Closed");


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
                cmd.Parameters.AddWithValue("@Description", "Imported from JIra on " + DateTime.Now.ToString() + ".");
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
