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
    public class ExportIssues : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportIssues(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Issue");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownerAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition identifiedByAttribute = assetType.GetAttributeDefinition("IdentifiedBy");
            query.Selection.Add(identifiedByAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition targetDateAttribute = assetType.GetAttributeDefinition("TargetDate");
            query.Selection.Add(targetDateAttribute);

            IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
            query.Selection.Add(resolutionAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
            query.Selection.Add(resolutionReasonAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);
            
            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition requestsAttribute = assetType.GetAttributeDefinition("Requests.ID");
            query.Selection.Add(requestsAttribute);

            IAttributeDefinition blockedPrimaryWorkitemsAttribute = assetType.GetAttributeDefinition("BlockedPrimaryWorkitems.ID");
            query.Selection.Add(blockedPrimaryWorkitemsAttribute);

            IAttributeDefinition primaryWorkitemsAttribute = assetType.GetAttributeDefinition("PrimaryWorkitems.ID");
            query.Selection.Add(primaryWorkitemsAttribute);

            IAttributeDefinition retrospectivesAttribute = null;
            IAttributeDefinition categoryAttribute = null;
            IAttributeDefinition blockedEpicsAttribute = null;
            IAttributeDefinition epicsAttribute = null;
            
            if (true)
            {
                retrospectivesAttribute = assetType.GetAttributeDefinition("Retrospectives.ID");
                query.Selection.Add(retrospectivesAttribute);
                
                categoryAttribute = assetType.GetAttributeDefinition("Category");
                query.Selection.Add(categoryAttribute);

                blockedEpicsAttribute = assetType.GetAttributeDefinition("BlockedEpics.ID");
                query.Selection.Add(blockedEpicsAttribute);

                epicsAttribute = assetType.GetAttributeDefinition("Epics.ID");
                query.Selection.Add(epicsAttribute);
            }

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildIssueInsertStatement();

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

                        //IDENTIFIED BY NPI MASK:
                        object identifiedBy = GetScalerValue(asset.GetAttribute(identifiedByAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && identifiedBy != DBNull.Value)
                        {
                            identifiedBy = ExportUtils.RemoveNPI(identifiedBy.ToString());
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
                        cmd.Parameters.AddWithValue("@Retrospectives", retrospectivesAttribute == null ? DBNull.Value : GetMultiRelationValues(asset.GetAttribute(retrospectivesAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Owner", GetSingleRelationValue(asset.GetAttribute(ownerAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@IdentifiedBy", identifiedBy);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@TargetDate", GetScalerValue(asset.GetAttribute(targetDateAttribute)));
                        cmd.Parameters.AddWithValue("@Resolution", resolution);
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@ResolutionReason", GetSingleRelationValue(asset.GetAttribute(resolutionReasonAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.Parameters.AddWithValue("@Category", categoryAttribute == null ? DBNull.Value : GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Requests", GetMultiRelationValues(asset.GetAttribute(requestsAttribute)));
                        cmd.Parameters.AddWithValue("@BlockedPrimaryWorkitems", GetMultiRelationValues(asset.GetAttribute(blockedPrimaryWorkitemsAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@PrimaryWorkitems", GetMultiRelationValues(asset.GetAttribute(primaryWorkitemsAttribute)));
                        cmd.Parameters.AddWithValue("@BlockedEpics", blockedEpicsAttribute == null ? DBNull.Value : GetMultiRelationValues(asset.GetAttribute(blockedEpicsAttribute)));
                        cmd.Parameters.AddWithValue("@Epics", epicsAttribute == null ? DBNull.Value : GetMultiRelationValues(asset.GetAttribute(epicsAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Issue: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildIssueInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ISSUES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Retrospectives,");
            sb.Append("Team,");
            sb.Append("Scope,");
            sb.Append("Owner,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("IdentifiedBy,");
            sb.Append("Reference,");
            sb.Append("TargetDate,");
            sb.Append("Resolution,");
            sb.Append("[Order],");
            sb.Append("ResolutionReason,");
            sb.Append("Source,");
            sb.Append("Priority,");
            sb.Append("Category,");
            sb.Append("Requests,");
            sb.Append("TaggedWith,");
            sb.Append("BlockedPrimaryWorkitems,");
            sb.Append("PrimaryWorkitems,");
            sb.Append("BlockedEpics,");
            sb.Append("Epics) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Retrospectives,");
            sb.Append("@Team,");
            sb.Append("@Scope,");
            sb.Append("@Owner,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@IdentifiedBy,");
            sb.Append("@Reference,");
            sb.Append("@TargetDate,");
            sb.Append("@Resolution,");
            sb.Append("@Order,");
            sb.Append("@ResolutionReason,");
            sb.Append("@Source,");
            sb.Append("@Priority,");
            sb.Append("@Category,");
            sb.Append("@Requests,");
            sb.Append("@TaggedWith,");
            sb.Append("@BlockedPrimaryWorkitems,");
            sb.Append("@PrimaryWorkitems,");
            sb.Append("@BlockedEpics,");
            sb.Append("@Epics);");
            return sb.ToString();
        }

    }
}
