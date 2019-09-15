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
    public class ExportSchedules : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportSchedules(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Schedule");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition timeboxGapAttribute = assetType.GetAttributeDefinition("TimeboxGap");
            query.Selection.Add(timeboxGapAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition timeboxLengthAttribute = assetType.GetAttributeDefinition("TimeboxLength");
            query.Selection.Add(timeboxLengthAttribute);

            string SQL = BuildScheduleInsertStatement();

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
                        cmd.Parameters.AddWithValue("@AssetState", asset.GetAttribute(assetStateAttribute).Value.ToString());
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@TimeboxGap", GetScalerValue(asset.GetAttribute(timeboxGapAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@TimeboxLength", asset.GetAttribute(timeboxLengthAttribute).Value.ToString());
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Schedule: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter; ;
        }

        private string BuildScheduleInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO SCHEDULES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("TimeboxGap,");
            sb.Append("TaggedWith,");
            sb.Append("TimeboxLength) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@TimeboxGap,");
            sb.Append("@TaggedWith,");
            sb.Append("@TimeboxLength);");
            return sb.ToString();
        }

    }
}
