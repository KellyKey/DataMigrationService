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
    public class CleanupTests : ICleanup
    {
        public CleanupTests(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override void Cleanup()
        {
            RemapStatuses();
        }

        private void RemapStatuses()
        {
            string SQL = "SELECT * FROM TESTS WHERE ImportStatus = 'IMPORTED' AND NewAssetOID LIKE 'Test:%';";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Test");
                IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("AssetState");
                IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");

                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                if (asset != null)
                {
                    string currentState = asset.GetAttribute(stateAttribute).Value.ToString();

                    if (currentState == "Closed")
                    {
                        ExecuteOperationInV1("Test.Reactivate", asset.Oid);
                        //Console.WriteLine("Reopened test {0}.", asset.Oid.ToString());
                    }

                    asset.SetAttributeValue(statusAttribute, MapTestStatus(sdr["Status"].ToString()));
                    try
                    {
                        _dataAPI.Save(asset);
                        Console.WriteLine("Updated test {0}.", asset.Oid.Token.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to update {0}. ERROR: {1}.", asset.Oid.ToString(), ex.Message);
                    }

                    if (currentState == "Closed")
                    {
                        ExecuteOperationInV1("Test.Inactivate", asset.Oid);
                        //Console.WriteLine("Closed test {0}.", asset.Oid.ToString());
                    }
                }
            }
            sdr.Close();
        }

        private string MapTestStatus(string Status)
        {
            switch (Status)
            {
                case "Pass":
                    return "TestStatus:129"; //Passed
                case "Fail":
                    return "TestStatus:155"; //Failed
                case "Inconclusive":
                    return "TestStatus:360027"; //Invalid
                default:
                    return "TestStatus:129"; //Accepted
            }
        }

    }
}
