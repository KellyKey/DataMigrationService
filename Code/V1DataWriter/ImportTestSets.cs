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
    public class ImportTestSets : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportTestSets(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("TestSet");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("TestSets");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: No assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Defects", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Defect has no scope.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("TestSet");
                    Asset asset = _dataAPI.New(assetType, null);

                    if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    {
                        IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                        asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                    }

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    //asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());
                    string targetURL = _config.V1TargetConnection.Url;
                    string newDescription = SetEmbeddedImageContent(sdr["Description"].ToString(), targetURL);
                    asset.SetAttributeValue(descAttribute, newDescription);

                    IAttributeDefinition iterationAttribute = assetType.GetAttributeDefinition("Timebox");
                    asset.SetAttributeValue(iterationAttribute, GetNewAssetOIDFromDB(sdr["Timebox"].ToString(), "Timebox"));

                    IAttributeDefinition regressionSuiteAttribute = assetType.GetAttributeDefinition("RegressionSuite");
                    asset.SetAttributeValue(regressionSuiteAttribute, GetNewAssetOIDFromDB(sdr["RegressionSuite"].ToString(), "RegressionSuites"));

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
                    asset.SetAttributeValue(teamAttribute, GetNewAssetOIDFromDB(sdr["Team"].ToString()));

                    if (String.IsNullOrEmpty(sdr["Goals"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Goals", sdr["Goals"].ToString());
                    }

                    IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
                    asset.SetAttributeValue(superAttribute, GetNewAssetOIDFromDB(sdr["Super"].ToString()));

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
                    asset.SetAttributeValue(detailEstimateAttribute, sdr["DetailEstimate"].ToString());

                    IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
                    asset.SetAttributeValue(toDoAttribute, sdr["ToDo"].ToString());

                    IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
                    asset.SetAttributeValue(estimateAttribute, sdr["Estimate"].ToString());

                    IAttributeDefinition environmentAttribute = assetType.GetAttributeDefinition("Environment");
                    asset.SetAttributeValue(environmentAttribute, GetNewAssetOIDFromDB(sdr["Environment"].ToString()));

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Scope"));
                    //asset.SetAttributeValue(scopeAttribute, _config.V1Configurations.p);

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    ////HACK: For Rally import, needs to be refactored.
                    ////asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("StoryStatus", sdr["Status"].ToString()));
                    ////asset.SetAttributeValue(statusAttribute, GetStatusAssetOID(sdr["Status"].ToString()));

                    ////HACK: For Rally import, needs to be refactored.
                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    if (String.IsNullOrEmpty(_config.V1Configurations.SourceListTypeValue) == false)
                        asset.SetAttributeValue(sourceAttribute, _config.V1Configurations.SourceListTypeValue);
                    else
                        asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));
                    ////HACK: For Rally import, needs to be refactored.
                    ////asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB("WorkitemPriority", sdr["Priority"].ToString()));

                    IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString()));

                    if (String.IsNullOrEmpty(sdr["AffectedByDefects"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "AffectedByDefects", sdr["AffectedByDefects"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Requests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Requests", sdr["Requests"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["BlockingIssues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "BlockingIssues", sdr["BlockingIssues"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Issues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Issues", sdr["Issues"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

                    _dataAPI.Save(asset);

                    string newAssetNumber = GetAssetNumberV1("TestSet", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("TestSets", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("TestSets", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "TestSet imported.");
                    importCount++;

                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);
                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("TestSets", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
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
            SetTestSetDependencies();
            return importCount;
        }

        private void SetTestSetDependencies()
        {
                int testSetCount = 0;

                SqlDataReader sdr = GetImportDataFromDBTable("TestSets");
                while (sdr.Read())
                {
                    testSetCount++;
                    _logger.Info("*****Setting TestSet Dependencies - Count is " + testSetCount);

                    IAssetType assetType = _metaAPI.GetAssetType("TestSet");
                    Asset asset = null;

                    bool saveDependencies = false;

                    if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    {
                        asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                    }
                    else
                    {
                        continue;
                    }

                    if (String.IsNullOrEmpty(sdr["Dependencies"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "TestSets", "Dependencies", sdr["Dependencies"].ToString());
                        saveDependencies = true;
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Dependencies Added ");
                    }

                    if (String.IsNullOrEmpty(sdr["Dependants"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "TestSets", "Dependants", sdr["Dependants"].ToString());
                        saveDependencies = true;
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Dependents Added ");
                    }

                    if (saveDependencies == true)
                    {
                        _dataAPI.Save(asset);
                    }

                }
                sdr.Close();
         }

         public int CloseTestSets()
         {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("TestSets");
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

        public int OpenTestSets()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("TestSets");
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
