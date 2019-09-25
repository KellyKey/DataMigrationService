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
    public class ImportTeams : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportTeams(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Teams");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    string currentAssetOID = CheckForDuplicateInV1("Team", "Name", sdr["Name"].ToString());

                    if (string.IsNullOrEmpty(currentAssetOID) == false)
                    {
                        UpdateNewAssetOIDAndStatus("Teams", sdr["AssetOID"].ToString(), currentAssetOID, ImportStatuses.SKIPPED, "Duplicate team.");
                        continue;
                    }
                    else
                    {
                        IAssetType assetType = _metaAPI.GetAssetType("Team");
                        Asset asset = _dataAPI.New(assetType, null);

                        IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                        asset.SetAttributeValue(fullNameAttribute, sdr["Name"].ToString().Trim());

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

                        _dataAPI.Save(asset);
                        UpdateNewAssetOIDAndStatus("Teams", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), ImportStatuses.IMPORTED, "Team imported.");
                        importCount++;
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                    }
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Teams", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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

        public int CloseTeams()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Teams");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                        ExecuteOperationInV1("Team.Inactivate", asset.Oid);
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

        public int OpenTeams()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Teams");
            int assetCount = 0;
            int exceptionCount = 0;
            while (sdr.Read())
            {
                Asset asset = null;

                try
                {
                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    ExecuteOperationInV1("Team.Reactivate", asset.Oid);
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
