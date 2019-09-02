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
    public class ImportEpics : IImportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ImportEpics(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Import()
        {
            string customV1IDFieldName = GetV1IDCustomFieldName("Epic");
            SqlDataReader sdr = GetImportDataFromDBTableWithOrder("Epics");

            int importCount = 0;
            while (sdr.Read())
            {
                try
                {
                    //SPECIAL CASE: No assigned scope, fail to import.
                    if (String.IsNullOrEmpty(sdr["Scope"].ToString()))
                    {
                        UpdateImportStatus("Epics", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Epic has no scope.");
                        continue;
                    }

                    //SPECIAL CASE: Already has been processed in the DB 
                    //if (String.IsNullOrEmpty(sdr["NewAssetOID"].ToString()) == false)
                    //{
                    //    UpdateImportStatus("Epics", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, "Already Processed.");
                    //    continue;
                    //}

                    IAssetType assetType = _metaAPI.GetAssetType("Epic");
                    Asset asset = _dataAPI.New(assetType, null);

                    //string newAssetOid = GetNewAssetOIDFromDB(sdr["AssetOID"].ToString(), "Epic");
                    //Asset asset = GetAssetFromV1(newAssetOid);

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

                    if (String.IsNullOrEmpty(sdr["Owners"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Members", "Owners", sdr["Owners"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Goals"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Goals", sdr["Goals"].ToString());
                    }

                    IAttributeDefinition riskAttribute = assetType.GetAttributeDefinition("Risk");
                    asset.SetAttributeValue(riskAttribute, sdr["Risk"].ToString());

                    if (String.IsNullOrEmpty(sdr["Requests"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Requests", sdr["Requests"].ToString());
                    }

                    IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
                    asset.SetAttributeValue(referenceAttribute, sdr["Reference"].ToString());

                    IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Scope"));
                    ////asset.SetAttributeValue(scopeAttribute, _config.JiraConfiguration.ProjectName);

                    ////SPECIAL CASE: Need to account for epic conversion.
                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //if (sdr["AssetOID"].ToString().Contains("Story"))
                    //{
                    //    asset.SetAttributeValue(statusAttribute, GetNewEpicListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    //}
                    //else
                    //{
                    //    //HACK: For Rally import, needs to be refactored.
                    //    //asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("EpicStatus", sdr["Status"].ToString()));
                    //}

                    IAttributeDefinition swagAttribute = assetType.GetAttributeDefinition("Swag");
                    asset.SetAttributeValue(swagAttribute, sdr["Swag"].ToString());

                    IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
                    asset.SetAttributeValue(requestedByAttribute, sdr["RequestedBy"].ToString());

                    IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
                    asset.SetAttributeValue(valueAttribute, sdr["Value"].ToString());

                    if (String.IsNullOrEmpty(sdr["BlockingIssues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "BlockingIssues", sdr["BlockingIssues"].ToString());
                    }

                    if (String.IsNullOrEmpty(sdr["Issues"].ToString()) == false)
                    {
                        AddMultiValueRelation(assetType, asset, "Issues", sdr["Issues"].ToString());
                    }

                    ////SPECIAL CASE: Need to account for epic conversion.
                    IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
                    if (sdr["AssetOID"].ToString().Contains("Story"))
                    {
                        asset.SetAttributeValue(categoryAttribute, GetNewEpicListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    }
                    else
                    {
                        asset.SetAttributeValue(categoryAttribute, GetNewListTypeAssetOIDFromDB(sdr["Category"].ToString()));
                    }

                    IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
                    if (String.IsNullOrEmpty(_config.V1Configurations.SourceListTypeValue) == false)
                        asset.SetAttributeValue(sourceAttribute, _config.V1Configurations.SourceListTypeValue);
                    else
                        asset.SetAttributeValue(sourceAttribute, GetNewListTypeAssetOIDFromDB(sdr["Source"].ToString()));

                    IAttributeDefinition plannedStartAttribute = assetType.GetAttributeDefinition("PlannedStart");
                    asset.SetAttributeValue(plannedStartAttribute, sdr["PlannedStart"].ToString());

                    IAttributeDefinition plannedEndAttribute = assetType.GetAttributeDefinition("PlannedEnd");
                    asset.SetAttributeValue(plannedEndAttribute, sdr["PlannedEnd"].ToString());

                    if (String.IsNullOrEmpty(sdr["TaggedWith"].ToString()) == false)
                    {
                        IAttributeDefinition multiAttribute = assetType.GetAttributeDefinition("TaggedWith");

                        AddMultiText(assetType, asset, multiAttribute, sdr["TaggedWith"].ToString());

                    }

                    ////SPECIAL CASE: Need to account for epic conversion.
                    IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
                    if (sdr["AssetOID"].ToString().Contains("Story"))
                    {
                        asset.SetAttributeValue(priorityAttribute, GetNewEpicListTypeAssetOIDFromDB(sdr["Priority"].ToString()));
                    }
                    else
                    {
                        asset.SetAttributeValue(priorityAttribute, GetNewListTypeAssetOIDFromDB(sdr["Priority"].ToString()));
                    }

                    _dataAPI.Save(asset);

                    if (sdr["AssetState"].ToString() == "Template")
                    {
                        ExecuteOperationInV1("Epic.MakeTemplate", asset.Oid);
                    }

                    string newAssetNumber = GetAssetNumberV1("Epic", asset.Oid.Momentless.ToString());
                    UpdateNewAssetOIDAndNumberInDB("Epics", sdr["AssetOID"].ToString(), asset.Oid.Momentless.ToString(), newAssetNumber);
                    UpdateImportStatus("Epics", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "Epic imported.");
                    importCount++;

                }
                catch (Exception ex)
                {
                    if (_config.V1Configurations.LogExceptions == true)
                    {
                        UpdateImportStatus("Epics", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, ex.Message);
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            sdr.Close();
            _logger.Info("-> Completed Importing {0} Epics.", importCount);

            SetParentEpics();
            SetEpicDependencies();
            return importCount;
        }

        private void SetParentEpics()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Epics");
            int setParentCount = 0;

            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Epic");
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                string newSuperAssetOID = string.Empty;

                //SPECIAL CASE: Need to account for epic conversion.
                IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Super");
                if (sdr["AssetOID"].ToString().Contains("Story"))
                {
                    asset.SetAttributeValue(parentAttribute, GetNewEpicAssetOIDFromDB(sdr["Super"].ToString()));
                }
                else
                {
                    newSuperAssetOID = GetNewAssetOIDFromDB(sdr["Super"].ToString(), "Epics");
                    asset.SetAttributeValue(parentAttribute, newSuperAssetOID);
                }
                _dataAPI.Save(asset);
                //_logger.Info("-> Set Parent for {0} Epic to {1}.", sdr["NewAssetOID"].ToString(), newSuperAssetOID);
                setParentCount++;

            }
            sdr.Close();
            _logger.Info("-> {0} Parents have been Set", setParentCount);

        }

        private void SetEpicDependencies()
        {
            _logger.Info("*****Setting Epic Dependencies");

            SqlDataReader sdr = GetImportDataFromDBTable("Epics");
            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Epic");
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
                    AddMultiValueRelation(assetType, asset, "Epics", "Dependencies", sdr["Dependencies"].ToString());
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Dependencies Added ");
                }

                if (String.IsNullOrEmpty(sdr["Dependants"].ToString()) == false)
                {
                    AddMultiValueRelation(assetType, asset, "Epics", "Dependants", sdr["Dependants"].ToString());
                    _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Dependents Added ");
                }
                _dataAPI.Save(asset);

            }
            sdr.Close();
        }


        public int CloseEpics()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Epics");
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

                    ExecuteOperationInV1("Epic.Inactivate", asset.Oid);
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

        public int OpenEpics()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Epics");
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

                    ExecuteOperationInV1("Epic.Reactivate", asset.Oid);
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
