using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataReader
{
    public class ExportEpics : IExportAssets
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        
        public ExportEpics(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            if (true)
            {
                return ExportEpicsFromEpics();
            }
            else
            {
                //Moved the control of versions less than 12 to ExportStories based on GRASP Pattern:Information Expert 
                //return ExportEpicsFromStories();
                _logger.Info("Source Version is less than 12 and will be exported with Stories");
                return 0;
            }
        }

        private int ExportEpicsFromEpics()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Epic");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners.ID");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition goalsAttribute = assetType.GetAttributeDefinition("Goals.ID");
            query.Selection.Add(goalsAttribute);

            IAttributeDefinition superAttribute = assetType.GetAttributeDefinition("Super");
            query.Selection.Add(superAttribute);

            IAttributeDefinition riskAttribute = assetType.GetAttributeDefinition("Risk");
            query.Selection.Add(riskAttribute);

            IAttributeDefinition requestsAttribute = assetType.GetAttributeDefinition("Requests.ID");
            query.Selection.Add(requestsAttribute);

            IAttributeDefinition descriptionAttribute = assetType.GetAttributeDefinition("Description");
            query.Selection.Add(descriptionAttribute);

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);

            IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
            query.Selection.Add(referenceAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition swagAttribute = assetType.GetAttributeDefinition("Swag");
            query.Selection.Add(swagAttribute);

            IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
            query.Selection.Add(requestedByAttribute);

            IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
            query.Selection.Add(valueAttribute);

            IAttributeDefinition orderAttribute = assetType.GetAttributeDefinition("Order");
            query.Selection.Add(orderAttribute);

            IAttributeDefinition blockingIssuesAttribute = assetType.GetAttributeDefinition("BlockingIssues.ID");
            query.Selection.Add(blockingIssuesAttribute);

            IAttributeDefinition issuesAttribute = assetType.GetAttributeDefinition("Issues.ID");
            query.Selection.Add(issuesAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);

            //IAttributeDefinition plannedStartAttribute = assetType.GetAttributeDefinition("PlannedStart");
            //query.Selection.Add(plannedStartAttribute);

            //IAttributeDefinition plannedEndAttribute = assetType.GetAttributeDefinition("PlannedEnd");
            //query.Selection.Add(plannedEndAttribute);

            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition taggedWithAttribute = assetType.GetAttributeDefinition("TaggedWith");
            query.Selection.Add(taggedWithAttribute);

            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildEpicInsertStatement();

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

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", CheckEpicState(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@Goals", GetMultiRelationValues(asset.GetAttribute(goalsAttribute)));
                        cmd.Parameters.AddWithValue("@Super", GetSingleRelationValue(asset.GetAttribute(superAttribute)));
                        cmd.Parameters.AddWithValue("@Risk", GetScalerValue(asset.GetAttribute(riskAttribute)));
                        cmd.Parameters.AddWithValue("@Requests", GetMultiRelationValues(asset.GetAttribute(requestsAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Swag", GetScalerValue(asset.GetAttribute(swagAttribute)));
                        cmd.Parameters.AddWithValue("@RequestedBy", requestedBy);
                        cmd.Parameters.AddWithValue("@Value", GetScalerValue(asset.GetAttribute(valueAttribute)));
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@TaggedWith", GetMultiRelationValues(asset.GetAttribute(taggedWithAttribute)));
                        cmd.Parameters.AddWithValue("@BlockingIssues", GetMultiRelationValues(asset.GetAttribute(blockingIssuesAttribute)));
                        cmd.Parameters.AddWithValue("@Issues", GetMultiRelationValues(asset.GetAttribute(issuesAttribute)));
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        //cmd.Parameters.AddWithValue("@PlannedStart", GetScalerValue(asset.GetAttribute(plannedStartAttribute)));
                        //cmd.Parameters.AddWithValue("@PlannedEnd", GetScalerValue(asset.GetAttribute(plannedEndAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);

            if (_config.V1Configurations.MigrateTemplates == false)
            {
                DeleteEpicTemplates();
            }

            return assetCounter;
        }

        private object CheckEpicState(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null)
                if (attribute.Value.ToString() == "200")
                    return "Template";
                else
                    return attribute.Value.ToString();
            else
                return DBNull.Value;
        }

        private void DeleteEpicTemplates()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "DELETE FROM Epics WHERE AssetState = 'Template';";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }



        private string BuildEpicInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO EPICS (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");
            sb.Append("Owners,");
            sb.Append("Goals,");
            sb.Append("Super,");
            sb.Append("Risk,");
            sb.Append("Requests,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("Scope,");
            sb.Append("Status,");
            sb.Append("Swag,");
            sb.Append("RequestedBy,");
            sb.Append("Value,");
            sb.Append("[Order],");
            sb.Append("TaggedWith,");
            sb.Append("BlockingIssues,");
            sb.Append("Issues,");
            sb.Append("Category,");
            sb.Append("Source,");
            //sb.Append("PlannedStart,");
            //sb.Append("PlannedEnd,");
            sb.Append("Priority) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");
            sb.Append("@Owners,");
            sb.Append("@Goals,");
            sb.Append("@Super,");
            sb.Append("@Risk,");
            sb.Append("@Requests,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@Scope,");
            sb.Append("@Status,");
            sb.Append("@Swag,");
            sb.Append("@RequestedBy,");
            sb.Append("@Value,");
            sb.Append("@Order,");
            sb.Append("@TaggedWith,");
            sb.Append("@BlockingIssues,");
            sb.Append("@Issues,");
            sb.Append("@Category,");
            sb.Append("@Source,");
            //sb.Append("@PlannedStart,");
            //sb.Append("@PlannedEnd,");
            sb.Append("@Priority);");
            return sb.ToString();
        }

        //Moved the control of versions less than 12 to ExportStories based on GRASP Pattern:Information Expert 
        //private int ExportEpicsFromStories()
        //{
        //    int recordCount = 0;
        //    using (SqlCommand cmd = new SqlCommand())
        //    {
        //        cmd.Connection = _sqlConn;
        //        cmd.CommandText = "SELECT * FROM Stories WHERE AssetState = '208';";
        //        cmd.CommandType = CommandType.Text;
        //        SqlDataReader sdr = cmd.ExecuteReader();

        //        string SQL = BuildEpicInsertStatement();
        //        while (sdr.Read())
        //        {
        //            using (SqlCommand cmdInsert = new SqlCommand())
        //            {
        //                cmdInsert.Connection = _sqlConn;
        //                cmdInsert.CommandText = SQL;
        //                cmdInsert.CommandType = CommandType.Text;
        //                cmdInsert.Parameters.AddWithValue("@AssetOID", sdr["AssetOID"]);
        //                cmdInsert.Parameters.AddWithValue("@AssetState", sdr["SubState"]);
        //                cmdInsert.Parameters.AddWithValue("@AssetNumber", sdr["AssetNumber"]);
        //                cmdInsert.Parameters.AddWithValue("@Owners", sdr["Owners"]);
        //                cmdInsert.Parameters.AddWithValue("@Goals", sdr["Goals"]);
        //                cmdInsert.Parameters.AddWithValue("@Super", sdr["Super"]);
        //                cmdInsert.Parameters.AddWithValue("@Risk", DBNull.Value); //Does not mean the same when going from story to epic.
        //                cmdInsert.Parameters.AddWithValue("@Requests", sdr["Requests"]);

        //                string dependency = string.Empty;
        //                string dependant = string.Empty;
                        
        //                if(sdr["Dependencies"].ToString() != string.Empty)
        //                {
        //                    string[] depList = sdr["Dependencies"].ToString().Split(';');

        //                    foreach (string dl in depList)
        //                    {
        //                        string nextValue = GetStoryNumber(dl);
        //                        dependency += nextValue + ", ";
        //                    }
        //                }

        //                if (sdr["Dependants"].ToString() != string.Empty)
        //                {
        //                    string[] depList = sdr["Dependants"].ToString().Split(';');

        //                    foreach (string dl in depList)
        //                    {
        //                        string nextValue = GetStoryNumber(dl);
        //                        dependant += nextValue + ", ";
        //                    }
        //                }

        //                //cmdInsert.Parameters.AddWithValue("@Description", sdr["Description"]);
        //                cmdInsert.Parameters.AddWithValue("@Description", sdr["Description"].ToString() + " UpStream Dependencies - " + dependency + " DownStream Dependants - " + dependant);
        //                cmdInsert.Parameters.AddWithValue("@Name", sdr["Name"]);
        //                cmdInsert.Parameters.AddWithValue("@Reference", sdr["Reference"]);
        //                cmdInsert.Parameters.AddWithValue("@Scope", sdr["Scope"]);
        //                cmdInsert.Parameters.AddWithValue("@Status", sdr["Status"]);
        //                cmdInsert.Parameters.AddWithValue("@Swag", sdr["Estimate"]);
        //                cmdInsert.Parameters.AddWithValue("@RequestedBy", sdr["RequestedBy"]);
        //                cmdInsert.Parameters.AddWithValue("@Value", sdr["Value"]);
        //                cmdInsert.Parameters.AddWithValue("@Order", sdr["Order"]);
        //                cmdInsert.Parameters.AddWithValue("@BlockingIssues", sdr["BlockingIssues"]);
        //                cmdInsert.Parameters.AddWithValue("@Issues", sdr["Issues"]);
        //                cmdInsert.Parameters.AddWithValue("@Category", sdr["Category"]);
        //                cmdInsert.Parameters.AddWithValue("@Source", sdr["Source"]);
        //                cmdInsert.Parameters.AddWithValue("@Priority", sdr["Priority"]);
        //                cmdInsert.ExecuteNonQuery();
        //                recordCount++;
        //            }
        //        }
        //    }
        //    DeleteEpicStories();
        //    return recordCount; ;
        //}

        //private void DeleteEpicStories()
        //{
        //    using (SqlCommand cmd = new SqlCommand())
        //    {
        //        cmd.Connection = _sqlConn;
        //        cmd.CommandText = "DELETE FROM Stories WHERE AssetState = '208';";
        //        cmd.CommandType = System.Data.CommandType.Text;
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        //private string GetStoryNumber(string depOid)
        //{
        //    IAssetType assetType = _metaAPI.GetAssetType("Story");
        //    Query query = new Query(assetType);

        //    IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
        //    query.Selection.Add(numberAttribute);

        //    //Filter on Story OID
        //    IAttributeDefinition assetIDAttribute = assetType.GetAttributeDefinition("ID");
        //    FilterTerm term = new FilterTerm(assetIDAttribute);
        //    term.Equal(depOid);
        //    query.Filter = term;

        //    QueryResult result = _dataAPI.Retrieve(query);

        //    Asset asset = result.Assets[0];
        //    string attributeName = GetScalerValue(asset.GetAttribute(numberAttribute)).ToString();
        //    return attributeName;
        
        //}
    }
}
