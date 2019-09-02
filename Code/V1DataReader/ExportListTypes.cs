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
        private string listTypeName = string.Empty;
        public ExportListTypes(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations) : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            int listTypeCount = 0;
            IAttributeDefinition teamAttribute = null;
            //string listTypeName = string.Empty;


            foreach (MigrationConfiguration.ListTypeInfo listType in _config.ListTypesToMigrate)
            {
                if (listType.Enabled == true)
                {

                    listTypeName = listType.Name;
                    IAssetType assetType = _metaAPI.GetAssetType(listType.Name);
                    Query query = new Query(assetType);

                    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                    query.Selection.Add(nameAttribute);

                    IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
                    query.Selection.Add(assetStateAttribute);

                    //Supports Team Process Status Changes
                    if (listType.Name.Equals("StoryStatus"))
                    {
                        teamAttribute = assetType.GetAttributeDefinition("Team");
                        query.Selection.Add(teamAttribute);
                    }

                    IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
                    query.Selection.Add(descriptionAttribute);

                    QueryResult result = _dataAPI.Retrieve(query);

                    string SQL = BuildListTypeInsertStatement();

                    foreach (Asset asset in result.Assets)
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = _sqlConn;
                            cmd.CommandText = SQL;
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                            if (listType.Name == "Custom_CrossProductsImpacted")
                            {
                                cmd.Parameters.AddWithValue("@AssetType", "Custom_Cross_Products_Impacted");
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@AssetType", listType.Name);
                            }
                            cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                            cmd.Parameters.AddWithValue("@Description", GetScalerValue(asset.GetAttribute(descriptionAttribute)));
                            if (listType.Name.Equals("StoryStatus"))
                            {
                                cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                            }
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
            if (listTypeName.Equals("StoryStatus"))
            {
                sb.Append("Team,");
            }
            sb.Append("Name) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetType,");
            sb.Append("@AssetState,");
            sb.Append("@Description,");
            if (listTypeName.Equals("StoryStatus"))
            {
                sb.Append("@Team,");
            }
            sb.Append("@Name);");
            return sb.ToString();
        }

    }
}
