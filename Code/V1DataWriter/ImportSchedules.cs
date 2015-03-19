using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataWriter
{
    public class ImportSchedules : IImportAssets
    {
        public ImportSchedules(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Schedules");

            int importCount = 0;
            int duplicateCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Handle duplicate schedules.
                    string currentAssetOID = CheckForDuplicateInV1("Schedule", "Name", sdr["Name"].ToString());
                    string nameModifer = String.Empty;
                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        if (_config.V1Configurations.MigrateDuplicateSchedules == true)
                        {
                            nameModifer = " (" + duplicateCount++ +")";
                        }
                        else
                        {
                            UpdateNewAssetOIDAndStatus("Schedules", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate schedule.");
                            continue;
                        }
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Schedule");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString() + nameModifer);

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition tbLengthAttribute = assetType.GetAttributeDefinition("TimeboxLength");
                    asset.SetAttributeValue(tbLengthAttribute, sdr["TimeboxLength"].ToString());

                    IAttributeDefinition tbGapAttribute = assetType.GetAttributeDefinition("TimeboxGap");
                    asset.SetAttributeValue(tbGapAttribute, sdr["TimeboxGap"].ToString());

                    _dataAPI.Save(asset);
                    if (String.IsNullOrEmpty(nameModifer))
                        UpdateNewAssetOIDAndStatus("Schedules", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Schedule imported.");
                    else
                        UpdateNewAssetOIDAndStatus("Schedules", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Duplicate schedule found, schedule imported with modified name.");
                    importCount++;
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Schedules", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            sdr.Close();
            return importCount;
        }

        public int CloseSchedules()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Schedules");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Schedule.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
