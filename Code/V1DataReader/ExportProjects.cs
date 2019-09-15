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
    public class ExportProjects : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ExportProjects(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations) 
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Scope");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition scheduleAttribute = assetType.GetAttributeDefinition("Schedule");
            query.Selection.Add(scheduleAttribute);

            IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
            query.Selection.Add(parentAttribute);

            IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owner");
            query.Selection.Add(ownerAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition endDateAttribute = assetType.GetAttributeDefinition("EndDate");
            query.Selection.Add(endDateAttribute);

            IAttributeDefinition beginDateAttribute = assetType.GetAttributeDefinition("BeginDate");
            query.Selection.Add(beginDateAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition membersAttribute = null;
            if (_config.V1Configurations.MigrateProjectMembership == true)
            {
                membersAttribute = assetType.GetAttributeDefinition("Members.ID");
                query.Selection.Add(membersAttribute);
            }

            IAttributeDefinition schemeAttribute = assetType.GetAttributeDefinition("Scheme");
            query.Selection.Add(schemeAttribute);

            //SPECIAL CASE: Reference attribute only exists in V1 12 and later.
            IAttributeDefinition referenceAttribute = null;
            if (true)
            {
                referenceAttribute = assetType.GetAttributeDefinition("Reference");
                query.Selection.Add(referenceAttribute);
            }

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildProjectInsertStatement();

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
                
                _logger.Info("assetTotal is {0} ", assetTotal);

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
                        cmd.Parameters.AddWithValue("@Schedule", GetSingleRelationValue(asset.GetAttribute(scheduleAttribute)));
                        cmd.Parameters.AddWithValue("@Parent", GetSingleRelationValue(asset.GetAttribute(parentAttribute)));
                        cmd.Parameters.AddWithValue("@Owner", GetSingleRelationValue(asset.GetAttribute(ownerAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@EndDate", GetScalerValue(asset.GetAttribute(endDateAttribute)));
                        cmd.Parameters.AddWithValue("@BeginDate", GetScalerValue(asset.GetAttribute(beginDateAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Members", membersAttribute == null ? DBNull.Value : GetMultiRelationValues(asset.GetAttribute(membersAttribute)));
                        cmd.Parameters.AddWithValue("@Reference", referenceAttribute == null ? DBNull.Value : GetScalerValue(asset.GetAttribute(referenceAttribute)));
                        cmd.ExecuteNonQuery();
                        
                        
                        
                       
                    }
                    assetCounter++;
                    _logger.Info("Project: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildProjectInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO PROJECTS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("Schedule,");
            sb.Append("Parent,");
            sb.Append("Owner,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("EndDate,");
            sb.Append("BeginDate,");
            sb.Append("TaggedWith,");
            sb.Append("Status,");
            sb.Append("Members,");
            sb.Append("Reference) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@Schedule,");
            sb.Append("@Parent,");
            sb.Append("@Owner,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@EndDate,");
            sb.Append("@BeginDate,");
            sb.Append("@TaggedWith,");
            sb.Append("@Status,");
            sb.Append("@Members,");
            sb.Append("@Reference);");
            return sb.ToString();
        }

    }
}
