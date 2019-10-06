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
    public class ImportCustomFields : IImportAssets
    {
        private string _assetType;
        private string _tableName;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ImportCustomFields(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations, string AssetType)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) 
        {
            _assetType = AssetType;
        }

        public override int Import()
        {

            string assetTypeInternalName = null;

            if (_assetType == "Story")
            {
                _tableName = "Stories";
            }
            else
            {
                _tableName = _assetType + "s";
            }

            MigrationConfiguration.AssetInfo assetInfo = _config.AssetsToMigrate.Find(i => i.Name == _tableName);
            assetTypeInternalName = assetInfo.InternalName;

            if (_assetType.Contains("PrimaryWorkitem"))
            {
                string[] assetTypeName = null;
                assetTypeName = _assetType.Split(':');
                _tableName = assetTypeName[1];
                assetTypeInternalName = assetTypeName[0];
            }

            List<MigrationConfiguration.CustomFieldInfo> fields = _config.CustomFieldsToMigrate.FindAll(i => i.AssetType == assetTypeInternalName);

            int importCount = 0;
            foreach (MigrationConfiguration.CustomFieldInfo field in fields)
            {

                if (field.SourceName != "Custom_Severity2")
                {
                    continue;
                }

                SqlDataReader sdr = GetImportDataFromDBTableForCustomFields(_tableName, field.SourceName);
                while (sdr.Read())
                {
                 
                    try
                    {
                        //Get the asset from V1.
                        IAssetType assetType = _metaAPI.GetAssetType(assetTypeInternalName);
                        Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                        //IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");

                        _logger.Info("---" + field.TargetName + " Working... ");

                        //Set the custom field value and save it.
                        IAttributeDefinition customFieldAttribute = assetType.GetAttributeDefinition(field.TargetName);

                        if (field.DataType == "Multi-Relation")
                        {

                            if (String.IsNullOrEmpty(sdr["FieldValue"].ToString()) == false)
                            {
                                AddMultiValueRelation(assetType, asset, _tableName, field.TargetName, sdr["FieldValue"].ToString());
                            }
                            else
                            {
                                continue;
                            }

                            //if (String.IsNullOrEmpty(sdr["FieldValue"].ToString()) == false)
                            //{ 

                            //    string[] listTypeOID = GetMultiRelationValues(field.SourceName, sdr["FieldValue"].ToString());
                            //    //string listTypeOID = GetNewAssetOIDFromDB(sdr["FieldValue"].ToString());
                            //    //string listTypeOID = GetCustomListTypeAssetOIDFromV1(field.RelationName, sdr["FieldValue"].ToString());
                            //    foreach (string listType in listTypeOID)
                            //    {
                            //            string newlistType = listType.TrimEnd(';');
                            //            string[] splitValue = newlistType.Split(':');
                            //            IAssetType assetTypeNew = _metaAPI.GetAssetType(splitValue[0]);
                            //            int attributeValue = Convert.ToInt32(splitValue[1]);

                            //            Oid assetOID = new Oid(assetTypeNew, attributeValue, 0);
                            //            //asset.SetAttributeValue(customFieldAttribute, GetMultiRelationValues(asset.GetAttribute(nameAttribute)));
                            //            asset.AddAttributeValue(customFieldAttribute, assetOID);
                            //    }
                            //}
                            //else
                            //{
                            //    //UpdateImportStatus("CustomFields", sdr["AssetOID"].ToString(), ImportStatuses.SKIPPED, "CustomField Skipped.");
                            //    continue;
                            //}

                        }
                        else if(field.DataType == "Relation")
                        {
                            string newValue = GetNewAssetOIDFromDB(sdr["FieldValue"].ToString(), assetTypeInternalName);
                            asset.SetAttributeValue(customFieldAttribute, newValue);
                        }
                        else
                        {
                            asset.SetAttributeValue(customFieldAttribute, sdr["FieldValue"].ToString());
                        }

                        _dataAPI.Save(asset);

                        //if (field.DataType == "Relation")
                        //    //if (field.TargetName == "Custom_CrossProductsImpacted")
                        //{
                        //    _logger.Info("Custom_ Relation Field Added");
                        //}
                        //else if(field.DataType == "Multi-Relation")
                        //{
                        //    _logger.Info("Custom_ Multi-Relation Field Added");

                        //}
                        //else
                        //{
                        //    _logger.Info("Custom Scaler Field Added");
                        //}

                        UpdateImportStatus("CustomFields", sdr["AssetOID"].ToString(), ImportStatuses.IMPORTED, "CustomField imported.");
                        importCount++;
                        _logger.Info("Asset: " + sdr["AssetOID"].ToString() + " Added - Count: " + importCount);

                    }
                    catch (Exception ex)
                    {
                        if (_config.V1Configurations.LogExceptions == true)
                        {
                            string error = ex.Message.Replace("'", ":");
                            UpdateImportStatus("CustomFields", sdr["AssetOID"].ToString(), ImportStatuses.FAILED, error);
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
            }
            return importCount;
        }

    }
}
