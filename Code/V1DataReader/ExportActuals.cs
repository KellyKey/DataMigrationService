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
    public class ExportActuals : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ExportActuals(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations) 
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Actual");
            Query query = new Query(assetType);

            IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
            query.Selection.Add(valueAttribute);

            IAttributeDefinition dateAttribute = assetType.GetAttributeDefinition("Date");
            query.Selection.Add(dateAttribute);

            IAttributeDefinition timeboxAttribute = assetType.GetAttributeDefinition("Timebox");
            query.Selection.Add(timeboxAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition memberAttribute = assetType.GetAttributeDefinition("Member");
            query.Selection.Add(memberAttribute);

            IAttributeDefinition workitemAttribute = assetType.GetAttributeDefinition("Workitem");
            query.Selection.Add(workitemAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildActualInsertStatement();

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
                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@Value", GetScalerValue(asset.GetAttribute(valueAttribute)));
                        cmd.Parameters.AddWithValue("@Date", GetScalerValue(asset.GetAttribute(dateAttribute)));
                        cmd.Parameters.AddWithValue("@Timebox", GetSingleRelationValue(asset.GetAttribute(timeboxAttribute)));
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Member", GetSingleRelationValue(asset.GetAttribute(memberAttribute)));
                        cmd.Parameters.AddWithValue("@Workitem", GetSingleRelationValue(asset.GetAttribute(workitemAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                    _logger.Info("Actual: added - Count = {0}", assetCounter);
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);
            return assetCounter;
        }

        private string BuildActualInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO ACTUALS (");
            sb.Append("AssetOID,");
            sb.Append("Value,");
            sb.Append("Date,");
            sb.Append("Timebox,");
            sb.Append("Scope,");
            sb.Append("Member,");
            sb.Append("Workitem,");
            sb.Append("Team) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@Value,");
            sb.Append("@Date,");
            sb.Append("@Timebox,");
            sb.Append("@Scope,");
            sb.Append("@Member,");
            sb.Append("@Workitem,");
            sb.Append("@Team);");
            return sb.ToString();
        }

    }
}
