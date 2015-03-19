using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataCleanup
{
    public class CleanupTasks : ICleanup
    {
        public CleanupTasks(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override void Cleanup()
        {
            RemapStatuses();
        }

        private void RemapStatuses()
        {
            string SQL = "SELECT * FROM TASKS WHERE ImportStatus = 'IMPORTED' AND AssetState = 'Active';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Task");
                IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("AssetState");
                IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");

                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                if (asset != null)
                {
                    string currentState = asset.GetAttribute(stateAttribute).Value.ToString();

                    if (currentState == "Closed")
                        ExecuteOperationInV1("Task.Reactivate", asset.Oid);

                    asset.SetAttributeValue(statusAttribute, MapTaskStatus(sdr["Status"].ToString()));
                    try
                    {
                        _dataAPI.Save(asset);
                        Console.WriteLine("Updated task {0}.", asset.Oid.Token.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to update {0}. ERROR: {1}.", asset.Oid.ToString(), ex.Message);
                    }
                    finally
                    {
                        if (currentState == "Closed")
                            ExecuteOperationInV1("Task.Inactivate", asset.Oid);
                    }
                }
            }
            sdr.Close();
        }

        private string MapTaskStatus(string Status)
        {
            switch (Status)
            {
                case "Defined":
                    return "TaskStatus:14674"; //Pending
                case "In-Progress":
                    return "TaskStatus:123"; //In Progress
                case "Completed":
                    return "TaskStatus:125"; //Done  
                default:
                    return "TaskStatus:125"; //Accepted
            }
        }

    }
}
