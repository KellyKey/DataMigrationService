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
    public class ImportRegressionSuites : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportRegressionSuites(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("RegressionSuites");

            int importCount = 0;
            int skippedCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: Orphaned RegressionPlans that has no assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("RegressionSuites", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "RegressionSuite has no scope.");
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Skipped - Count: " + ++skippedCount);
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("RegressionSuites");
                    Asset asset = _dataAPI.New(assetType, null);

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString()));

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");
                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["RegressionTests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "RegressionTests", sdr["RegressionTests"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["TestSets"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "TestSets", sdr["TestSets"].ToString());
                    }

                    _dataAPI.Save(asset);
                    string newAssetNumber = GetAssetNumberV1("RegressionSuite", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("RegressionSuites", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("RegressionSuites", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "RegressionSuite imported.");
                    importCount++;
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("RegressionSuites", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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

        public int CloseRegressionSuites()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("RegressionSuites");
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
                    ExecuteOperationInV1(asset.Oid.ToString().Substring(0, colonLocation) + ".Inactivate", asset.Oid);
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

        public int OpenRegressionSuites()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("RegressionSuites");
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
