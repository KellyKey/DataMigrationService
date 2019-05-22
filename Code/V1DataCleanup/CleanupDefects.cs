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
    public class CleanupDefects : ICleanup
    {
        public CleanupDefects(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override void Cleanup()
        {
            RemapStatuses();
        }

        private void RemapStatuses()
        {
            string SQL = "SELECT * FROM DEFECTS WHERE ImportStatus = 'IMPORTED' AND AssetState = 'Active';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Defect");
                IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("AssetState");
                IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");

                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                if (asset != null)
                {
                    string currentState = asset.GetAttribute(stateAttribute).Value.ToString();

                    if (currentState == "Closed")
                        ExecuteOperationInV1("Defect.Reactivate", asset.Oid);

                    asset.SetAttributeValue(statusAttribute, MapDefectStatus(sdr["Status"].ToString()));
                    try
                    {
                        _dataAPI.Save(asset);
                        Console.WriteLine("Updated defect {0}.", asset.Oid.Token.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to update {0}. ERROR: {1}.", asset.Oid.ToString(), ex.Message);
                    }
                    finally
                    {
                        if (currentState == "Closed")
                            ExecuteOperationInV1("Defect.Inactivate", asset.Oid);
                    }
                }
            }
            sdr.Close();
        }

        private string MapDefectStatus(string Status)
        {
            switch (Status)
            {
                case "Accepted":
                    return "StoryStatus:59533"; //Accepted
                case "Defined":
                    return "StoryStatus:133"; //Pending
                case "Blessed":
                    return "StoryStatus:59533"; //Accepted
                case "In-Progress":
                    return "StoryStatus:137"; //In Progress
                case "Completed":
                    return "StoryStatus:2155"; //Done  
                default:
                    return "StoryStatus:59533"; //Accepted
            }
        }

    }
}
