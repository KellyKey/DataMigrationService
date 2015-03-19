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
    public class ImportEpics : IImportAssets
    {
        public ImportEpics(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
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

                    IAssetType assetType = _metaAPI.GetAssetType("Epic");
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
                    asset.SetAttributeValue(scopeAttribute, GetNewAssetOIDFromDB(sdr["Scope"].ToString(), "Projects"));

                    //SPECIAL CASE: Need to account for epic conversion.
                    IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
                    if (sdr["AssetOID"].ToString().Contains("Story"))
                    {
                        asset.SetAttributeValue(statusAttribute, GetNewEpicListTypeAssetOIDFromDB(sdr["Status"].ToString()));
                    }
                    else
                    {
                        //HACK: For Rally import, needs to be refactored.
                        asset.SetAttributeValue(statusAttribute, GetNewListTypeAssetOIDFromDB("EpicStatus", sdr["Status"].ToString()));
                    }

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

                    //SPECIAL CASE: Need to account for epic conversion.
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

                    //SPECIAL CASE: Need to account for epic conversion.
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
            SetParentEpics();
            return importCount;
        }

        private void SetParentEpics()
        {
            SqlDataReader sdr = GetImportDataFromDBTable("Epics");
            while (sdr.Read())
            {
                IAssetType assetType = _metaAPI.GetAssetType("Epic");
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());

                //SPECIAL CASE: Need to account for epic conversion.
                IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Super");
                if (sdr["AssetOID"].ToString().Contains("Story"))
                {
                    asset.SetAttributeValue(parentAttribute, GetNewEpicAssetOIDFromDB(sdr["Super"].ToString()));
                }
                else
                {
                    asset.SetAttributeValue(parentAttribute, GetNewAssetOIDFromDB(sdr["Super"].ToString(), "Epics"));
                }
                _dataAPI.Save(asset);
            }
            sdr.Close();
        }

        public int CloseEpics()
        {
            SqlDataReader sdr = GetImportDataFromDBTableForClosing("Epics");
            int assetCount = 0;
            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("Epic.Inactivate", asset.Oid);
                assetCount++;
            }
            sdr.Close();
            return assetCount;
        }

    }
}
