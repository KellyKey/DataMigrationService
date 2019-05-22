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
    public class ExportIterations : IExportAssets
    {
        public ExportIterations(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Timebox");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownerAttribute);

            IAttributeDefinition scheduleAttribute = assetType.GetAttributeDefinition("Schedule");
            query.Selection.Add(scheduleAttribute);
            
            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition targetEstimateAttribute = assetType.GetAttributeDefinition("TargetEstimate");
            query.Selection.Add(targetEstimateAttribute);

            IAttributeDefinition endDateAttribute = assetType.GetAttributeDefinition("EndDate");
            query.Selection.Add(endDateAttribute);

            IAttributeDefinition beginDateAttribute = assetType.GetAttributeDefinition("BeginDate");
            query.Selection.Add(beginDateAttribute);

            IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("State");
            query.Selection.Add(stateAttribute);

            string SQL = BuildIterationInsertStatement();

            if (_config.V1Configurations.PageSize != 0)
            {
                query.Paging.Start = 0;
                query.Paging.PageSize = _config.V1Configurations.PageSize;
            }

            int assetCounter = 0;
            int assetTotal = 0;

            do
            {
                QueryResult result = _dataAPI.Retrieve(query);
                assetTotal = result.TotalAvaliable;

                foreach (Asset asset in result.Assets)
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        //NAME NPI MASK:
                        object name = GetScalerValue(asset.GetAttribute(nameAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && name != DBNull.Value)
                        {
                            name = ExportUtils.RemoveNPI(name.ToString());
                        }

                        //DESCRIPTION NPI MASK:
                        object description = GetScalerValue(asset.GetAttribute(descriptionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && description != DBNull.Value)
                        {
                            description = ExportUtils.RemoveNPI(description.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@Owner", GetSingleRelationValue(asset.GetAttribute(ownerAttribute)));
                        cmd.Parameters.AddWithValue("@Schedule", GetSingleRelationValue(asset.GetAttribute(scheduleAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@TargetEstimate", GetScalerValue(asset.GetAttribute(targetEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@EndDate", GetScalerValue(asset.GetAttribute(endDateAttribute)));
                        cmd.Parameters.AddWithValue("@BeginDate", GetScalerValue(asset.GetAttribute(beginDateAttribute)));
                        cmd.Parameters.AddWithValue("@State", GetStateRelationValue(asset.GetAttribute(stateAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private object GetStateRelationValue(VersionOne.SDK.APIClient.Attribute attribute)
        {
            switch (attribute.Value.ToString())
            {
                case "State:100":
                    return "Future";
                case "State:101":
                    return "Active";
                case "State:102":
                    return "Closed";
                default:
                    return DBNull.Value;
            }
        }

        private string BuildIterationInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ITERATIONS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Owner,");
            sb.Append("Schedule,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("TargetEstimate,");
            sb.Append("EndDate,");
            sb.Append("BeginDate,");
            sb.Append("State) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Owner,");
            sb.Append("@Schedule,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@TargetEstimate,");
            sb.Append("@EndDate,");
            sb.Append("@BeginDate,");
            sb.Append("@State);");
            return sb.ToString();
        }

    }
}
