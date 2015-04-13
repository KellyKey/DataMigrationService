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
    public class ImportStories : IImportAssets
    {

        
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        
        public ImportStories(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Story");
            _logger.Info("Custom field Config is {0}", _config.V1Configurations.CustomV1IDField);
            _logger.Info("Custom Field Name is {0}", customV1IDFieldName);

            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Stories");

            //_logger.Info("Story Count is {0}", sdr.RecordsAffected);


            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: No assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Stories", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Story has no scope.");
                        continue;
                    }

                    IAssetType assetType = _metaAPI.GetAssetType("Story");
                    Asset asset = _dataAPI.New(assetType, null);

                    if (String.IsNullOrEmpty(customV1IDFieldName) == false)
                    {
                        IAttributeDefinition customV1IDAttribute = assetType.GetAttributeDefinition(customV1IDFieldName);
                        asset.SetAttributeValue(customV1IDAttribute, sdr["AssetNumber"].ToString());
                    }

                    IAttributeDefinition fullNameAttribute = assetType.GetAttributeDefinition("Name");
                    asset.SetAttributeValue(fullNameAttribute, AddV1IDToTitle(sdr["Name"].ToString(), sdr["AssetNumber"].ToString()));

                    IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
                    asset.SetAttributeValue(descAttribute, sdr["Description"].ToString());

                    IAttributeDefinition iterationAttribute = assetType.GetAttributeDefinition("Timebox");
                    asset.SetAttributeValue(iterationAttribute, GetNewAssetOIDFromDB(sdr["Timebox"].ToString(), "Iterations"));

                    IAttributeDefinition customerAttribute = assetType.GetAttributeDefinition("Customer");
                    asset.SetAttributeValue(customerAttribute, GetNewAssetOIDFromDB(sdr["Customer"].ToString()));

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

                    //TO DO: Test for V1 version number for epic conversion. Right now, assume epic.
                    IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
                    asset.SetAttributeValue(superAttribute, GetNewEpicAssetOIDFromDB(sdr["Super"].ToString()));

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
                    asset.SetAttributeValue(detailEstimateAttribute, sdr["DetailEstimate"].ToString());

                    IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
                    asset.SetAttributeValue(estimateAttribute, sdr["Estimate"].ToString());

                    IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
                    asset.SetAttributeValue(toDoAttribute, sdr["ToDo"].ToString());

                    IAttributeDefinition lastVersionAttribute = assetType.GetAttributeDefinition("LastVersion");
                    asset.SetAttributeValue(lastVersionAttribute, sdr["LastVersion"].ToString());

                    IAttributeDefinition originalEstimateAttribute = assetType.GetAttributeDefinition("OriginalEstimate");
                    asset.SetAttributeValue(originalEstimateAttribute, sdr["OriginalEstimate"].ToString());

                    IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
                    asset.SetAttributeValue(requestedByAttribute, sdr["RequestedBy"].ToString());

                    IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
                    asset.SetAttributeValue(valueAttribute, sdr["Value"].ToString());

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Projects"));

                    IAttributeDefinition riskAttribute = assetType.GetAttributeDefinition("Risk");
                    asset.SetAttributeValue(riskAttribute, GetNewListTypeAssetOIDFromDB(sdr["Risk"].ToString()));

                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    if (String.IsNullOrEmpty(_config.V1Configurations.SourceListTypeValue) == false)
                        asset.SetAttributeValue(sourceAttribute, _config.V1Configurations.SourceListTypeValue);
                    else
                        asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));

                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //HACK: For Rally import, needs to be refactored.
                    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("StoryStatus", sdr["Status"].ToString()));

                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));

                    //Themes
                    IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
                    asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Parent"].ToString()));

                    if (String.IsNullOrEmpty(sdr["Requests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Requests", sdr["Requests"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["BlockingIssues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "BlockingIssues", sdr["BlockingIssues"].ToString());
                        //_logger.Info("Asset is {0}", assetType.DisplayName);
                    }

                    if (String.IsNullOrEmpty(sdr["Issues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Issues", sdr["Issues"].ToString());
                    }

                    IAttributeDefinition benefitsAttribute = assetType.GetAttributeDefinition("Benefits");
                    asset.SetAttributeValue(benefitsAttribute, sdr["Benefits"].ToString());

                    _dataAPI.Save(asset);


                    if (sdr["AssetState"].ToString() == "Template")
                    {
                        ExecuteOperationInV1("Story.MakeTemplate", asset.Oid);
                    }

                    string newAssetNumber = GetAssetNumberV1("Story", asset.Oid.Momentless.ToString());

                    UpdateAssetRecordWithNumber("Stories", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber, ImportStatuses.IMPORTED, "Story imported.");
                    importCount++;

                    //_logger.Info("Story is {0} and the Count is {1}", newAssetNumber, importCount);

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        string error = ex.Message.Replace("'", ":");
                        UpdateImportStatus("Stories", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
                        //_logger.Info("Story is {0} ", sdr["AssetOID"].ToString());

                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            sdr.Close();
            SetStoryDependencies();
            return importCount;
        }

        private void SetStoryDependencies()
        {
            _logger.Info("*****Setting Story Dependencies");

            SqlDataReader sdr = GetImportDataFromDBTable("Stories");
            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Story");
                Asset asset = null;

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
                    AddMultiValueRelation(assetType, asset, "Stories", "Dependencies", sdr["Dependencies"].ToString());
                }

                if (String.IsNullOrEmpty(sdr["Dependants"].ToString()) == false)
                {
                    AddMultiValueRelation(assetType, asset, "Stories", "Dependants", sdr["Dependants"].ToString());
                }
                _dataAPI.Save(asset);
            }
            sdr.Close();
        }

        public int CloseStories()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Stories");
            int assetCount = 0;
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
                
                ExecuteOperationInV1("Story.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
