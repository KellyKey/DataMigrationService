﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{

    public class ImportSchedules : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportSchedules(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Schedules");

            int importCount = 0;
            int skippedCount = 0;
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
                            _logger.Error("Asset: " + sdr["AssetOID"].ToString() + " Skipped - Count = " + ++skippedCount);
                            continue;
                        }
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Schedule");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString() + nameModifer);

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

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
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Schedules", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        _logger.Error("Asset: " + sdr["AssetOID"].ToString() + " Failed to Import ");
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
