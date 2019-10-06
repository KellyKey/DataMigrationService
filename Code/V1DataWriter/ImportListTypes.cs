using System;
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
    public class ImportListTypes : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportListTypes(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("ListTypes");

            int importCount = 0;
            int skippedCount = 0;
            while (sdr.Read())
            {
                try
                {

                    //DUPLICATE CHECK:
                    string currentAssetOID = CheckForDuplicateListTypesInV1(sdr["AssetType"].ToString(), "Name", sdr["Name"].ToString(), "Team", sdr["Team"].ToString());
                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        UpdateNewAssetOIDAndStatus("ListTypes", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate list type.");
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Skipped - Count: " + ++skippedCount);
                    }
                    else
                    {

                        IAssetType assetType = _metaAPI.GetAssetType(sdr["AssetType"].ToString());
                        Asset asset = _dataAPI.New(assetType, null);

                        IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                        asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString());

                        IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                        asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                        _dataAPI.Save(asset);
                        UpdateNewAssetOIDAndStatus("ListTypes", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "List type imported.");
                        importCount++;
                        _logger.Info("New List, AssetOID is {0} and Count is {1}", sdr["AssetOID"].ToString(), importCount);

                    }

                    //SPECIAL CASE: Need to handle conversion of epic list types.
                    if (sdr["AssetType"].ToString() == "StoryStatus" || sdr["AssetType"].ToString() == "StoryCategory" || sdr["AssetType"].ToString() == "WorkitemPriority")
                    {
                        importCount += ConvertEpicListType(sdr["AssetOID"].ToString(), sdr["AssetType"].ToString(), sdr["Name"].ToString(), sdr["Description"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    //_logger.Info("Catching:  AssetOID is {0}", sdr["AssetOID"].ToString());

                    
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("ListTypes", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Failed - Count: " + ++skippedCount);
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

        private int ConvertEpicListType(string AssetOID, string AssetType, string Name, string Description)
        {
            int importCount = 0;

            //Convert story list asset to epic list asset.
            string convertedAssetType;
            if (AssetType == "StoryStatus")
                convertedAssetType = "EpicStatus";
            else if (AssetType == "StoryCategory")
                convertedAssetType = "EpicCategory";
            else if (AssetType == "WorkitemPriority")
                convertedAssetType = "EpicPriority";
            else
                return 0;

            string currentAssetOID = CheckForDuplicateInV1(convertedAssetType, "Name", Name);

            if (string.IsNullOrEmpty(currentAssetOID) == false)
            {
                UpdateNewEpicAssetOIDInDB("ListTypes", AssetOID, currentAssetOID);
            }
            else
            {
                IAssetType assetType = _metaAPI.GetAssetType(convertedAssetType);
                Asset asset = _dataAPI.New(assetType, null);

                IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                asset.SetAttributeValue(fullNameAttribute, Name);

                IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                asset.SetAttributeValue(descAttribute, Description);

                _dataAPI.Save(asset);
                UpdateNewEpicAssetOIDInDB("ListTypes", AssetOID, asset.Oid.Momentless.ToString());
                importCount++;
            }
            return importCount;
        }

        public int CloseListTypes()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("ListTypes");
            int assetCount = 0;
            int exceptionCount = 0;
            int colonLocation = 0;

            while (sdr.Read())
            {
                Asset asset = null;

                if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                {
                    asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                }
                else
                {
                    continue;
                }

                try
                {
                    colonLocation = asset.Oid.ToString().IndexOf(":");
                    ExecuteOperationInV1(asset.Oid.ToString().Substring(0,colonLocation) + ".Inactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Close - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }

            }
            sdr.Close();
            return assetCount;
        }

        public int OpenListTypes()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Listtypes");
            int assetCount = 0;
            int exceptionCount = 0;
            int colonLocation = 0;

            while (sdr.Read())
            {
                Asset asset = null;

                if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                {
                    asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                }
                else
                {
                    continue;
                }

                try
                {
                    colonLocation = asset.Oid.ToString().IndexOf(":");
                    ExecuteOperationInV1(asset.Oid.ToString().Substring(0, colonLocation) + ".Reactivate", asset.Oid);
                    assetCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Open - Count: " + assetCount);
                }
                catch (Exception ex)
                {
                    exceptionCount++;
                    _logger.Info("Exception: " + sdr["AssetOID"].ToString() + " Exception - Count: " + exceptionCount);
                    continue;
                }

            }
            sdr.Close();
            return assetCount;
        }


    }
}
