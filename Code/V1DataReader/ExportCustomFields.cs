 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataReader
{
    public class ExportCustomFields : IExportAssets
    {

        private string _InternalAssetType;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportCustomFields(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations, string InternalAssetType)
            : base(sqlConn, MetaAPI, DataAPI, Configurations)
        {
            _InternalAssetType = InternalAssetType;
        }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("AttributeDefinition");
            Query query = new Query(assetType);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition isBasicAttribute = assetType.GetAttributeDefinition("IsBasic");
            query.Selection.Add(isBasicAttribute);

            IAttributeDefinition nativeValueAttribute = assetType.GetAttributeDefinition("NativeValue");
            query.Selection.Add(nativeValueAttribute);

            IAttributeDefinition isCustomAttribute = assetType.GetAttributeDefinition("IsCustom");
            query.Selection.Add(isCustomAttribute);

            IAttributeDefinition isReadOnlyAttribute = assetType.GetAttributeDefinition("IsReadOnly");
            query.Selection.Add(isReadOnlyAttribute);

            IAttributeDefinition isRequiredAttribute = assetType.GetAttributeDefinition("IsRequired");
            query.Selection.Add(isRequiredAttribute);

            IAttributeDefinition attributeTypeAttribute = assetType.GetAttributeDefinition("AttributeType");
            query.Selection.Add(attributeTypeAttribute);

            IAttributeDefinition assetNameAttribute = assetType.GetAttributeDefinition("Asset.Name");
            query.Selection.Add(assetNameAttribute);

            //Filter on asset type and if attribute definition is custom.
            FilterTerm assetName = new FilterTerm(assetNameAttribute);
            assetName.Equal(_InternalAssetType);
            FilterTerm isCustom = new FilterTerm(isCustomAttribute);
            isCustom.Equal("true");
            query.Filter = new AndFilterTerm(assetName, isCustom);

            QueryResult result = _dataAPI.Retrieve(query);

            int customFieldCount = 0;
            foreach (Asset asset in result.Assets)
            {
                string attributeName = GetScalerValue(asset.GetAttribute(nameAttribute)).ToString();
                string attributeType = GetScalerValue(asset.GetAttribute(attributeTypeAttribute)).ToString();
                if (attributeName.StartsWith("Custom_"))
                {
                    _logger.Info("The CustomField is {0} and Type is {1}", attributeName, attributeType);
                    customFieldCount += GetCustomFields(attributeName, attributeType);
                }
            }
            return customFieldCount;
        }

        private int GetCustomFields(string attributeName, string attributeType)
        {
            IAssetType assetType = _metaAPI.GetAssetType(_InternalAssetType);
            Query query = new Query(assetType);
            int assetCount = 0;

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition(attributeName);
            query.Selection.Add(nameAttribute);

            //Test for LongText Fields because they will not pull from the API with an Empty String Filter
            switch (attributeType)
            {
                case "LongText":
                    break;
                default:
                    //Filter to ensure that we have a value.
                    FilterTerm filter = new FilterTerm(nameAttribute);
                    filter.NotEqual(String.Empty);
                    query.Filter = filter;
                    break;
            }
            

            QueryResult result = _dataAPI.Retrieve(query);
            string SQL = BuildCustomFieldInsertStatement();

            foreach (Asset asset in result.Assets)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    
                    cmd.Connection = _sqlConn;
                    cmd.CommandText = SQL;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                    cmd.Parameters.AddWithValue("@FieldName", attributeName);
                    cmd.Parameters.AddWithValue("@FieldType", attributeType);

                    if (attributeType == "Relation")
                    {
                        cmd.Parameters.AddWithValue("@FieldValue", GetMultiRelationValues(asset.GetAttribute(nameAttribute)));
                    }
                    else
                    {
                        //Remove NULL Records from LongText Fields
                        Object fieldValue = GetScalerValue(asset.GetAttribute(nameAttribute));
                        if (fieldValue.Equals(DBNull.Value))
                        {
                            continue;
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@FieldValue", fieldValue);
                        }
                    }
                  

                    cmd.ExecuteNonQuery();
                    assetCount++;
                }
            }
            return assetCount;
        }

        private string BuildCustomFieldInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO CUSTOMFIELDS (");
            sb.Append("AssetOID,");
            sb.Append("FieldName,");
            sb.Append("FieldType,");
            sb.Append("FieldValue) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@FieldName,");
            sb.Append("@FieldType,");
            sb.Append("@FieldValue);");
            return sb.ToString();
        }


    }
}
