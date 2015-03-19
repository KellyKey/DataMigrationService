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
    public class ExportRequests : IExportAssets
    {
        public ExportRequests(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Request");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownerAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition epicsAttribute = null;
            if (_metaAPI.Version.Major > 11)
            {
                epicsAttribute = assetType.GetAttributeDefinition("Epics.ID");
                query.Selection.Add(epicsAttribute);
            }

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
            query.Selection.Add(resolutionAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
            query.Selection.Add(requestedByAttribute);

            IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
            query.Selection.Add(resolutionReasonAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);

            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildRequestInsertStatement();

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

                        //REFERENCE NPI MASK:
                        object reference = GetScalerValue(asset.GetAttribute(referenceAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && reference != DBNull.Value)
                        {
                            reference = ExportUtils.RemoveNPI(reference.ToString());
                        }

                        //REQUESTED BY NPI MASK:
                        object requestedBy = GetScalerValue(asset.GetAttribute(requestedByAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && requestedBy != DBNull.Value)
                        {
                            requestedBy = ExportUtils.RemoveNPI(requestedBy.ToString());
                        }

                        //RESOLUTION NPI MASK:
                        object resolution = GetScalerValue(asset.GetAttribute(resolutionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && resolution != DBNull.Value)
                        {
                            resolution = ExportUtils.RemoveNPI(resolution.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", GetScalerValue(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Owner", GetSingleRelationValue(asset.GetAttribute(ownerAttribute)));
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Epics", epicsAttribute == null ? DBNull.Value : GetMultiRelationValues(asset.GetAttribute(epicsAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@Resolution", resolution);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@RequestedBy", requestedBy);
                        cmd.Parameters.AddWithValue("@ResolutionReason", GetSingleRelationValue(asset.GetAttribute(resolutionReasonAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildRequestInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO REQUESTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owner,");
            sb.Append("Scope,");
            sb.Append("Epics,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("[Order],");
            sb.Append("Resolution,");
            sb.Append("Reference,");
            sb.Append("RequestedBy,");
            sb.Append("ResolutionReason,");
            sb.Append("Source,");
            sb.Append("Priority,");
            sb.Append("Status,");
            sb.Append("Category) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owner,");
            sb.Append("@Scope,");
            sb.Append("@Epics,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Order,");
            sb.Append("@Resolution,");
            sb.Append("@Reference,");
            sb.Append("@RequestedBy,");
            sb.Append("@ResolutionReason,");
            sb.Append("@Source,");
            sb.Append("@Priority,");
            sb.Append("@Status,");
            sb.Append("@Category);");
            return sb.ToString();
        }

    }
}
