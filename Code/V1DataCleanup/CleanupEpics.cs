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
    public class CleanupEpics : ICleanup
    {
        public CleanupEpics(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override void Cleanup()
        {
            RemapStatuses();
        }

        private void RemapStatuses()
        {
            string SQL = "SELECT * FROM EPICS WHERE ImportStatus = 'IMPORTED' AND AssetState = 'Active';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Epic");
                IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("AssetState");
                IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");

                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                if (asset != null)
                {
                    string currentState = asset.GetAttribute(stateAttribute).Value.ToString();

                    if (currentState == "Closed")
                        ExecuteOperationInV1("Epic.Reactivate", asset.Oid);

                    asset.SetAttributeValue(statusAttribute, MapEpicStatus(sdr["Status"].ToString()));
                    try
                    {
                        _dataAPI.Save(asset);
                        Console.WriteLine("Updated epic {0}.", asset.Oid.Token.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to update {0}. ERROR: {1}.", asset.Oid.ToString(), ex.Message);
                    }
                    finally
                    {
                        if (currentState == "Closed")
                            ExecuteOperationInV1("Epic.Inactivate", asset.Oid);
                    }
                }
            }
            sdr.Close();
        }

        private string MapEpicStatus(string Status)
        {
            switch (Status)
            {
                case "Accepted":
                    return "EpicStatus:253965"; //Accepted
                case "Defined":
                    return "EpicStatus:253940"; //Pending
                case "Blessed":
                    return "EpicStatus:253965"; //Accepted
                case "In-Progress":
                    return "EpicStatus:253941"; //In Progress
                case "Completed":
                    return "EpicStatus:253946"; //Done  
                default:
                    return "EpicStatus:253965"; //Accepted
            }
        }

    }
}
