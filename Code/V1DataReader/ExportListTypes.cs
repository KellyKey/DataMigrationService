using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataReader
{
    public class ExportListTypes : IExportAssets
    {
        public ExportListTypes(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations) : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            int listTypeCount = 0;
            string SQL = BuildListTypeInsertStatement();

            foreach (MigrationConfiguration.ListTypeInfo listType in _config.ListTypesToMigrate)
            {
                if (listType.Enabled == true)
                {
                    IAssetType assetType = _metaAPI.GetAssetType(listType.Name);
                    Query query = new Query(assetType);

                    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                    query.Selection.Add(nameAttribute);

                    IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
                    query.Selection.Add(assetStateAttribute);

                    IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
                    query.Selection.Add(descriptionAttribute);

                    QueryResult result = _dataAPI.Retrieve(query);

                    foreach (Asset asset in result.Assets)
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = _sqlConn;
                            cmd.CommandText = SQL;
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                            cmd.Parameters.AddWithValue("@AssetType", listType.Name);
                            cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                            cmd.Parameters.AddWithValue("@Description", GetScalerValue(asset.GetAttribute(descriptionAttribute)));
                            cmd.Parameters.AddWithValue("@Name", GetScalerValue(asset.GetAttribute(nameAttribute)));
                            cmd.ExecuteNonQuery();
                        }
                        listTypeCount++;
                    }
                }
            }
            return listTypeCount;
        }

        private string BuildListTypeInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO LISTTYPES (");
            sb.Append("AssetOID,");
            sb.Append("AssetType,");
            sb.Append("AssetState,");
            sb.Append("Description,");
            sb.Append("Name) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetType,");
            sb.Append("@AssetState,");
            sb.Append("@Description,");
            sb.Append("@Name);");
            return sb.ToString();
        }

    }
}
