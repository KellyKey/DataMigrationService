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
            if (_assetType.Contains("PrimaryWorkitem"))
            {
                string [] assetTypeName = null;
                assetTypeName = _assetType.Split(':');
                _assetType = assetTypeName[0];
                assetTypeInternalName = assetTypeName[1];
            }
            else
            {
                //This code doesn't work and needs to be fixed...For Now, just use the _assetType variable passed in
                //MigrationConfiguration.AssetInfo assetInfo = _config.AssetsToMigrate.Find(i => i.Name == _assetType);
                //assetTypeInternalName = assetInfo.InternalName;
                assetTypeInternalName = _assetType;

                if (_assetType == "Story")
                {
                    _tableName = "Stories";
                }
                else
                {
                    _tableName = _assetType + "s";
                }
            }


            List<MigrationConfiguration.CustomFieldInfo> fields = _config.CustomFieldsToMigrate.FindAll(i => i.AssetType == assetTypeInternalName);

            int importCount = 0;
            foreach (MigrationConfiguration.CustomFieldInfo field in fields)
            {
                
                
                SqlDataReader sdr = GetImportDataFromDBTableForCustomFields(_tableName, field.SourceName);
                while (sdr.Read())
                {
                 
                    try
                    {
                        //Get the asset from V1.
                        IAssetType assetType = _metaAPI.GetAssetType(assetTypeInternalName);
                        Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                        _logger.Info("Asset: " + sdr["NewAssetOID"].ToString() + " Working... ");

                        //Set the custom field value and save it.
                        IAttributeDefinition customFieldAttribute = assetType.GetAttributeDefinition(field.TargetName);
                        if (field.DataType == "Relation")
                        {
                            string listTypeOID = GetCustomListTypeAssetOIDFromV1(field.RelationName, sdr["FieldValue"].ToString());
                            if (String.IsNullOrEmpty(listTypeOID) == false)
                            {
                                asset.SetAttributeValue(customFieldAttribute, listTypeOID);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            asset.SetAttributeValue(customFieldAttribute, sdr["FieldValue"].ToString());
                        }
                        _dataAPI.Save(asset);
                    
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
