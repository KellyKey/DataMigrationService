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
    public class ExportDefects : IExportAssets
    {
        public ExportDefects(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Defect");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition timeboxAttribute = assetType.GetAttributeDefinition("Timebox");
            query.Selection.Add(timeboxAttribute);

            IAttributeDefinition verifiedByAttribute = assetType.GetAttributeDefinition("VerifiedBy");
            query.Selection.Add(verifiedByAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners.ID");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition duplicateOfAttribute = assetType.GetAttributeDefinition("DuplicateOf");
            query.Selection.Add(duplicateOfAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            IAttributeDefinition versionsAttribute = assetType.GetAttributeDefinition("Versions.ID");
            query.Selection.Add(versionsAttribute);

            IAttributeDefinition goalsAttribute = assetType.GetAttributeDefinition("Goals.ID");
            query.Selection.Add(goalsAttribute);

            IAttributeDefinition affectedPrimaryWorkitemsAttribute = assetType.GetAttributeDefinition("AffectedPrimaryWorkitems.ID");
            query.Selection.Add(affectedPrimaryWorkitemsAttribute);

            IAttributeDefinition affectedByDefectsAttribute = assetType.GetAttributeDefinition("AffectedByDefects.ID");
            query.Selection.Add(affectedByDefectsAttribute);

            IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
            query.Selection.Add(superAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition toDoAttribute = assetType.GetAttributeDefinition("ToDo");
            query.Selection.Add(toDoAttribute);

            IAttributeDefinition detailEstimateAttribute = assetType.GetAttributeDefinition("DetailEstimate");
            query.Selection.Add(detailEstimateAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition estimateAttribute = assetType.GetAttributeDefinition("Estimate");
            query.Selection.Add(estimateAttribute);

            IAttributeDefinition foundInBuildAttribute = assetType.GetAttributeDefinition("FoundInBuild");
            query.Selection.Add(foundInBuildAttribute);

            IAttributeDefinition environmentAttribute = assetType.GetAttributeDefinition("Environment");
            query.Selection.Add(environmentAttribute);

            IAttributeDefinition resolutionAttribute = assetType.GetAttributeDefinition("Resolution");
            query.Selection.Add(resolutionAttribute);

            IAttributeDefinition versionAffectedAttribute = assetType.GetAttributeDefinition("VersionAffected");
            query.Selection.Add(versionAffectedAttribute);

            IAttributeDefinition fixedInBuildAttribute = assetType.GetAttributeDefinition("FixedInBuild");
            query.Selection.Add(fixedInBuildAttribute);

            IAttributeDefinition foundByAttribute = assetType.GetAttributeDefinition("FoundBy");
            query.Selection.Add(foundByAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition typeAttribute = assetType.GetAttributeDefinition("Type");
            query.Selection.Add(typeAttribute);

            IAttributeDefinition resolutionReasonAttribute = assetType.GetAttributeDefinition("ResolutionReason");
            query.Selection.Add(resolutionReasonAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);

            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
            query.Selection.Add(parentAttribute);

            IAttributeDefinition requestsAttribute = assetType.GetAttributeDefinition("Requests.ID");
            query.Selection.Add(requestsAttribute);

            IAttributeDefinition blockingIssuesAttribute = assetType.GetAttributeDefinition("BlockingIssues.ID");
            query.Selection.Add(blockingIssuesAttribute);

            IAttributeDefinition issuesAttribute = assetType.GetAttributeDefinition("Issues.ID");
            query.Selection.Add(issuesAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildDefectInsertStatement();

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

                        //FOUND IN BUILD NPI MASK:
                        object foundInBuild = GetScalerValue(asset.GetAttribute(foundInBuildAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && foundInBuild != DBNull.Value)
                        {
                            foundInBuild = ExportUtils.RemoveNPI(foundInBuild.ToString());
                        }

                        //ENVIRONMENT NPI MASK:
                        object environment = GetScalerValue(asset.GetAttribute(environmentAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && environment != DBNull.Value)
                        {
                            environment = ExportUtils.RemoveNPI(environment.ToString());
                        }

                        //RESOLUTION NPI MASK:
                        object resolution = GetScalerValue(asset.GetAttribute(resolutionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && resolution != DBNull.Value)
                        {
                            resolution = ExportUtils.RemoveNPI(resolution.ToString());
                        }

                        //VERSION AFFECTED NPI MASK:
                        object versionAffected = GetScalerValue(asset.GetAttribute(versionAffectedAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && versionAffected != DBNull.Value)
                        {
                            versionAffected = ExportUtils.RemoveNPI(versionAffected.ToString());
                        }

                        //FIXED IN BUILD NPI MASK:
                        object fixedInBuild = GetScalerValue(asset.GetAttribute(fixedInBuildAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && fixedInBuild != DBNull.Value)
                        {
                            fixedInBuild = ExportUtils.RemoveNPI(fixedInBuild.ToString());
                        }

                        //FOUND BY NPI MASK:
                        object foundBy = GetScalerValue(asset.GetAttribute(foundByAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && foundBy != DBNull.Value)
                        {
                            foundBy = ExportUtils.RemoveNPI(foundBy.ToString());
                        }

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", CheckDefectState(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Timebox", GetSingleRelationValue(asset.GetAttribute(timeboxAttribute)));
                        cmd.Parameters.AddWithValue("@VerifiedBy", GetSingleRelationValue(asset.GetAttribute(verifiedByAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@DuplicateOf", GetSingleRelationValue(asset.GetAttribute(duplicateOfAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.Parameters.AddWithValue("@Versions", GetMultiRelationValues(asset.GetAttribute(versionsAttribute)));
                        cmd.Parameters.AddWithValue("@Goals", GetMultiRelationValues(asset.GetAttribute(goalsAttribute)));
                        cmd.Parameters.AddWithValue("@AffectedPrimaryWorkitems", GetMultiRelationValues(asset.GetAttribute(affectedPrimaryWorkitemsAttribute)));
                        cmd.Parameters.AddWithValue("@AffectedByDefects", GetMultiRelationValues(asset.GetAttribute(affectedByDefectsAttribute)));
                        cmd.Parameters.AddWithValue("@Super", GetSingleRelationValue(asset.GetAttribute(superAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@ToDo", GetScalerValue(asset.GetAttribute(toDoAttribute)));
                        cmd.Parameters.AddWithValue("@DetailEstimate", GetScalerValue(asset.GetAttribute(detailEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@Estimate", GetScalerValue(asset.GetAttribute(estimateAttribute)));
                        cmd.Parameters.AddWithValue("@FoundInBuild", foundInBuild);
                        cmd.Parameters.AddWithValue("@Environment", environment);
                        cmd.Parameters.AddWithValue("@Resolution", resolution);
                        cmd.Parameters.AddWithValue("@VersionAffected", versionAffected);
                        cmd.Parameters.AddWithValue("@FixedInBuild", fixedInBuild);
                        cmd.Parameters.AddWithValue("@FoundBy", foundBy);
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Type", GetSingleRelationValue(asset.GetAttribute(typeAttribute)));
                        cmd.Parameters.AddWithValue("@ResolutionReason", GetSingleRelationValue(asset.GetAttribute(resolutionReasonAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.Parameters.AddWithValue("@Parent", GetSingleRelationValue(asset.GetAttribute(parentAttribute)));
                        cmd.Parameters.AddWithValue("@Requests", GetMultiRelationValues(asset.GetAttribute(requestsAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@BlockingIssues", GetMultiRelationValues(asset.GetAttribute(blockingIssuesAttribute)));
                        cmd.Parameters.AddWithValue("@Issues", GetMultiRelationValues(asset.GetAttribute(issuesAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);

            if (_config.V1Configurations.MigrateTemplates == false)
            {
                DeleteDefectTemplates();
            }

            return assetCounter;
        }

        private object CheckDefectState(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null)
                if (attribute.Value.ToString() == "200")
                    return "Template";
                else
                    return attribute.Value.ToString();
            else
                return DBNull.Value;
        }

        private void DeleteDefectTemplates()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "DELETE FROM Defects WHERE AssetState = 'Template';";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private string BuildDefectInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO DEFECTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Timebox,");
            sb.Append("VerifiedBy,");
            sb.Append("Owners,");
            sb.Append("DuplicateOf,");
            sb.Append("Team,");
            sb.Append("Versions,");
            sb.Append("Goals,");
            sb.Append("AffectedPrimaryWorkitems,");
            sb.Append("AffectedByDefects,");
            sb.Append("Super,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("ToDo,");
            sb.Append("DetailEstimate,");
            sb.Append("[Order],");
            sb.Append("Estimate,");
            sb.Append("FoundInBuild,");
            sb.Append("Environment,");
            sb.Append("Resolution,");
            sb.Append("VersionAffected,");
            sb.Append("FixedInBuild,");
            sb.Append("FoundBy,");
            sb.Append("Scope,");
            sb.Append("Status,");
            sb.Append("Type,");
            sb.Append("ResolutionReason,");
            sb.Append("Source,");
            sb.Append("Priority,");
            sb.Append("Parent,");
            sb.Append("Requests,");
            sb.Append("TaggedWith,");
            sb.Append("BlockingIssues,");
            sb.Append("Issues) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Timebox,");
            sb.Append("@VerifiedBy,");
            sb.Append("@Owners,");
            sb.Append("@DuplicateOf,");
            sb.Append("@Team,");
            sb.Append("@Versions,");
            sb.Append("@Goals,");
            sb.Append("@AffectedPrimaryWorkitems,");
            sb.Append("@AffectedByDefects,");
            sb.Append("@Super,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@ToDo,");
            sb.Append("@DetailEstimate,");
            sb.Append("@Order,");
            sb.Append("@Estimate,");
            sb.Append("@FoundInBuild,");
            sb.Append("@Environment,");
            sb.Append("@Resolution,");
            sb.Append("@VersionAffected,");
            sb.Append("@FixedInBuild,");
            sb.Append("@FoundBy,");
            sb.Append("@Scope,");
            sb.Append("@Status,");
            sb.Append("@Type,");
            sb.Append("@ResolutionReason,");
            sb.Append("@Source,");
            sb.Append("@Priority,");
            sb.Append("@Parent,");
            sb.Append("@Requests,");
            sb.Append("@TaggedWith,");
            sb.Append("@BlockingIssues,");
            sb.Append("@Issues);");
            return sb.ToString();
        }

    }
}
