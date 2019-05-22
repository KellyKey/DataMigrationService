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
    public class ExportStories : IExportAssets
    {
        public ExportStories(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override int Export()
        {
            IAssetType assetType = _metaAPI.GetAssetType("Story");
            Query query = new Query(assetType);

            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            IAttributeDefinition assetNumberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(assetNumberAttribute);

            IAttributeDefinition timeboxAttribute = assetType.GetAttributeDefinition("Timebox");
            query.Selection.Add(timeboxAttribute);

            IAttributeDefinition customerAttribute = assetType.GetAttributeDefinition("Customer");
            query.Selection.Add(customerAttribute);

            IAttributeDefinition ownersAttribute = assetType.GetAttributeDefinition("Owners.ID");
            query.Selection.Add(ownersAttribute);

            IAttributeDefinition identifiedInAttribute = assetType.GetAttributeDefinition("IdentifiedIn");
            query.Selection.Add(identifiedInAttribute);

            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team");
            query.Selection.Add(teamAttribute);

            IAttributeDefinition goalsAttribute = assetType.GetAttributeDefinition("Goals.ID");
            query.Selection.Add(goalsAttribute);

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

            IAttributeDefinition lastVersionAttribute = assetType.GetAttributeDefinition("LastVersion");
            query.Selection.Add(lastVersionAttribute);

            IAttributeDefinition originalEstimateAttribute = assetType.GetAttributeDefinition("OriginalEstimate");
            query.Selection.Add(originalEstimateAttribute);

            IAttributeDefinition requestedByAttribute = assetType.GetAttributeDefinition("RequestedBy");
            query.Selection.Add(requestedByAttribute);

            IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition("Value");
            query.Selection.Add(valueAttribute);

            IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition("Scope");
            query.Selection.Add(scopeAttribute);

            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status");
            query.Selection.Add(statusAttribute);

            IAttributeDefinition categoryAttribute = assetType.GetAttributeDefinition("Category");
            query.Selection.Add(categoryAttribute);

            IAttributeDefinition riskAttribute = assetType.GetAttributeDefinition("Risk");
            query.Selection.Add(riskAttribute);

            IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
            query.Selection.Add(sourceAttribute);

            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority");
            query.Selection.Add(priorityAttribute);

            IAttributeDefinition dependenciesAttribute = assetType.GetAttributeDefinition("Dependencies.ID");
            query.Selection.Add(dependenciesAttribute);

            IAttributeDefinition dependantsAttribute = assetType.GetAttributeDefinition("Dependants.ID");
            query.Selection.Add(dependantsAttribute);

            IAttributeDefinition parentAttribute = assetType.GetAttributeDefinition("Parent");
            query.Selection.Add(parentAttribute);

            IAttributeDefinition requestsAttribute = assetType.GetAttributeDefinition("Requests.ID");
            query.Selection.Add(requestsAttribute);

            IAttributeDefinition blockingIssuesAttribute = assetType.GetAttributeDefinition("BlockingIssues.ID");
            query.Selection.Add(blockingIssuesAttribute);

            IAttributeDefinition issuesAttribute = assetType.GetAttributeDefinition("Issues.ID");
            query.Selection.Add(issuesAttribute);

            IAttributeDefinition benefitsAttribute = assetType.GetAttributeDefinition("Benefits");
            query.Selection.Add(benefitsAttribute);

            IAttributeDefinition subStateAttribute = null;
            if (false)
            {
                subStateAttribute = assetType.GetAttributeDefinition("SubState");
                query.Selection.Add(subStateAttribute);
            }

            //Filter on parent scope.
            IAttributeDefinition parentScopeAttribute = assetType.GetAttributeDefinition("Scope.ParentMeAndUp");
            FilterTerm term = new FilterTerm(parentScopeAttribute);
            term.Equal(_config.V1SourceConnection.Project);
            query.Filter = term;

            string SQL = BuildStoryInsertStatement();

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

                        //LAST VERSION NPI MASK:
                        object lastVersion = GetScalerValue(asset.GetAttribute(lastVersionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && lastVersion != DBNull.Value)
                        {
                            lastVersion = ExportUtils.RemoveNPI(lastVersion.ToString());
                        }

                        //BENEFITS NPI MASK:
                        object benefits = GetScalerValue(asset.GetAttribute(lastVersionAttribute));
                        if (_config.V1Configurations.UseNPIMasking == true && benefits != DBNull.Value)
                        {
                            benefits = ExportUtils.RemoveNPI(benefits.ToString());
                        }
                        

                        cmd.Connection = _sqlConn;
                        cmd.CommandText = SQL;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@AssetOID", asset.Oid.ToString());
                        cmd.Parameters.AddWithValue("@AssetState", CheckStoryState(asset.GetAttribute(assetStateAttribute)));
                        cmd.Parameters.AddWithValue("@AssetNumber", GetScalerValue(asset.GetAttribute(assetNumberAttribute)));

                        if (false)
                            cmd.Parameters.AddWithValue("@SubState", GetScalerValue(asset.GetAttribute(subStateAttribute)));

                        cmd.Parameters.AddWithValue("@Timebox", GetSingleRelationValue(asset.GetAttribute(timeboxAttribute)));
                        cmd.Parameters.AddWithValue("@Customer", GetSingleRelationValue(asset.GetAttribute(customerAttribute)));
                        cmd.Parameters.AddWithValue("@Owners", GetMultiRelationValues(asset.GetAttribute(ownersAttribute)));
                        cmd.Parameters.AddWithValue("@IdentifiedIn", GetSingleRelationValue(asset.GetAttribute(identifiedInAttribute)));
                        cmd.Parameters.AddWithValue("@Team", GetSingleRelationValue(asset.GetAttribute(teamAttribute)));
                        cmd.Parameters.AddWithValue("@Goals", GetMultiRelationValues(asset.GetAttribute(goalsAttribute)));
                        cmd.Parameters.AddWithValue("@AffectedByDefects", GetMultiRelationValues(asset.GetAttribute(affectedByDefectsAttribute)));
                        cmd.Parameters.AddWithValue("@Super", GetSingleRelationValue(asset.GetAttribute(superAttribute)));
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Reference", reference);
                        cmd.Parameters.AddWithValue("@ToDo", GetScalerValue(asset.GetAttribute(toDoAttribute)));
                        cmd.Parameters.AddWithValue("@DetailEstimate", GetScalerValue(asset.GetAttribute(detailEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@Order", GetScalerValue(asset.GetAttribute(orderAttribute)));
                        cmd.Parameters.AddWithValue("@Estimate", GetScalerValue(asset.GetAttribute(estimateAttribute)));
                        cmd.Parameters.AddWithValue("@LastVersion", lastVersion);
                        cmd.Parameters.AddWithValue("@OriginalEstimate", GetScalerValue(asset.GetAttribute(originalEstimateAttribute)));
                        cmd.Parameters.AddWithValue("@RequestedBy", requestedBy);
                        cmd.Parameters.AddWithValue("@Value", GetScalerValue(asset.GetAttribute(valueAttribute)));
                        cmd.Parameters.AddWithValue("@Scope", GetSingleRelationValue(asset.GetAttribute(scopeAttribute)));
                        cmd.Parameters.AddWithValue("@Status", GetSingleRelationValue(asset.GetAttribute(statusAttribute)));
                        cmd.Parameters.AddWithValue("@Category", GetSingleRelationValue(asset.GetAttribute(categoryAttribute)));
                        cmd.Parameters.AddWithValue("@Risk", GetSingleRelationValue(asset.GetAttribute(riskAttribute)));
                        cmd.Parameters.AddWithValue("@Source", GetSingleRelationValue(asset.GetAttribute(sourceAttribute)));
                        cmd.Parameters.AddWithValue("@Priority", GetSingleRelationValue(asset.GetAttribute(priorityAttribute)));
                        cmd.Parameters.AddWithValue("@Dependencies", GetMultiRelationValues(asset.GetAttribute(dependenciesAttribute)));
                        cmd.Parameters.AddWithValue("@Dependants", GetMultiRelationValues(asset.GetAttribute(dependantsAttribute)));
                        cmd.Parameters.AddWithValue("@Parent", GetSingleRelationValue(asset.GetAttribute(parentAttribute)));
                        cmd.Parameters.AddWithValue("@Requests", GetMultiRelationValues(asset.GetAttribute(requestsAttribute)));
                        cmd.Parameters.AddWithValue("@BlockingIssues", GetMultiRelationValues(asset.GetAttribute(blockingIssuesAttribute)));
                        cmd.Parameters.AddWithValue("@Issues", GetMultiRelationValues(asset.GetAttribute(issuesAttribute)));
                        cmd.Parameters.AddWithValue("@Benefits", benefits);
                        cmd.ExecuteNonQuery();
                    }
                    assetCounter++;
                }
                query.Paging.Start = assetCounter;
            } while (assetCounter != assetTotal);

            if (_config.V1Configurations.MigrateTemplates == false)
            {
                DeleteStoryTemplates();
            }

            //Check for Epics as Stories from V1 Major < 12
            //ExportEpics epicStory = new ExportEpics(_sqlConn, _metaAPI, _dataAPI, _config);
            //epicStory.Export();
            int rc = ExportEpicsFromStories();
            
            return assetCounter;
        }

        private object CheckStoryState(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null)
                if (attribute.Value.ToString() == "200")
                    return "Template";
                else
                    return attribute.Value.ToString();
            else
                return DBNull.Value;
        }

        private void DeleteStoryTemplates()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "DELETE FROM Stories WHERE AssetState = 'Template';";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private string BuildStoryInsertStatement()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO STORIES (");
            sb.Append("AssetOID,");
            sb.Append("AssetState,");
            sb.Append("AssetNumber,");

            if (false)
                sb.Append("SubState,");

            sb.Append("Timebox,");
            sb.Append("Customer,");
            sb.Append("Owners,");
            sb.Append("IdentifiedIn,");
            sb.Append("Team,");
            sb.Append("Goals,");
            sb.Append("AffectedByDefects,");
            sb.Append("Super,");
            sb.Append("Description,");
            sb.Append("Name,");
            sb.Append("Reference,");
            sb.Append("ToDo,");
            sb.Append("DetailEstimate,");
            sb.Append("[Order],");
            sb.Append("Estimate,");
            sb.Append("LastVersion,");
            sb.Append("OriginalEstimate,");
            sb.Append("RequestedBy,");
            sb.Append("Value,");
            sb.Append("Scope,");
            sb.Append("Status,");
            sb.Append("Category,");
            sb.Append("Risk,");
            sb.Append("Source,");
            sb.Append("Priority,");
            sb.Append("Dependencies,");
            sb.Append("Dependants,");
            sb.Append("Parent,");
            sb.Append("Requests,");
            sb.Append("BlockingIssues,");
            sb.Append("Issues,");
            sb.Append("Benefits) ");
            sb.Append("VALUES (");
            sb.Append("@AssetOID,");
            sb.Append("@AssetState,");
            sb.Append("@AssetNumber,");

            if (false)
                sb.Append("@SubState,");

            sb.Append("@Timebox,");
            sb.Append("@Customer,");
            sb.Append("@Owners,");
            sb.Append("@IdentifiedIn,");
            sb.Append("@Team,");
            sb.Append("@Goals,");
            sb.Append("@AffectedByDefects,");
            sb.Append("@Super,");
            sb.Append("@Description,");
            sb.Append("@Name,");
            sb.Append("@Reference,");
            sb.Append("@ToDo,");
            sb.Append("@DetailEstimate,");
            sb.Append("@Order,");
            sb.Append("@Estimate,");
            sb.Append("@LastVersion,");
            sb.Append("@OriginalEstimate,");
            sb.Append("@RequestedBy,");
            sb.Append("@Value,");
            sb.Append("@Scope,");
            sb.Append("@Status,");
            sb.Append("@Category,");
            sb.Append("@Risk,");
            sb.Append("@Source,");
            sb.Append("@Priority,");
            sb.Append("@Dependencies,");
            sb.Append("@Dependants,");
            sb.Append("@Parent,");
            sb.Append("@Requests,");
            sb.Append("@BlockingIssues,");
            sb.Append("@Issues,");
            sb.Append("@Benefits);");
            return sb.ToString();
        }

        private int ExportEpicsFromStories()
        {
            int recordCount = 0;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "SELECT * FROM Stories WHERE AssetState = '208';";
                cmd.CommandType = CommandType.Text;
                SqlDataReader sdr = cmd.ExecuteReader();

                string SQL = BuildEpicInsertStatement();
                while (sdr.Read())
                {
                    using (SqlCommand cmdInsert = new SqlCommand())
                    {
                        cmdInsert.Connection = _sqlConn;
                        cmdInsert.CommandText = SQL;
                        cmdInsert.CommandType = CommandType.Text;
                        cmdInsert.Parameters.AddWithValue("@AssetOID", sdr["AssetOID"]);
                        cmdInsert.Parameters.AddWithValue("@AssetState", sdr["SubState"]);
                        cmdInsert.Parameters.AddWithValue("@AssetNumber", sdr["AssetNumber"]);
                        cmdInsert.Parameters.AddWithValue("@Owners", sdr["Owners"]);
                        cmdInsert.Parameters.AddWithValue("@Goals", sdr["Goals"]);
                        cmdInsert.Parameters.AddWithValue("@Super", sdr["Super"]);
                        cmdInsert.Parameters.AddWithValue("@Risk", DBNull.Value); //Does not mean the same when going from story to epic.
                        cmdInsert.Parameters.AddWithValue("@Requests", sdr["Requests"]);

                        string dependency = string.Empty;
                        string dependant = string.Empty;
                        
                        if(sdr["Dependencies"].ToString() != string.Empty)
                        {
                            string[] depList = sdr["Dependencies"].ToString().Split(';');

                            foreach (string dl in depList)
                            {
                                string nextValue = GetStoryNumber(dl);
                                dependency += nextValue + ", ";
                            }
                        }

                        if (sdr["Dependants"].ToString() != string.Empty)
                        {
                            string[] depList = sdr["Dependants"].ToString().Split(';');

                            foreach (string dl in depList)
                            {
                                string nextValue = GetStoryNumber(dl);
                                dependant += nextValue + ", ";
                            }
                        }

                        //cmdInsert.Parameters.AddWithValue("@Description", sdr["Description"]);
                        cmdInsert.Parameters.AddWithValue("@Description", sdr["Description"].ToString() + " UpStream Dependencies - " + dependency + " DownStream Dependants - " + dependant);
                        cmdInsert.Parameters.AddWithValue("@Name", sdr["Name"]);
                        cmdInsert.Parameters.AddWithValue("@Reference", sdr["Reference"]);
                        cmdInsert.Parameters.AddWithValue("@Scope", sdr["Scope"]);
                        cmdInsert.Parameters.AddWithValue("@Status", sdr["Status"]);
                        cmdInsert.Parameters.AddWithValue("@Swag", sdr["Estimate"]);
                        cmdInsert.Parameters.AddWithValue("@RequestedBy", sdr["RequestedBy"]);
                        cmdInsert.Parameters.AddWithValue("@Value", sdr["Value"]);
                        cmdInsert.Parameters.AddWithValue("@Order", sdr["Order"]);
                        cmdInsert.Parameters.AddWithValue("@BlockingIssues", sdr["BlockingIssues"]);
                        cmdInsert.Parameters.AddWithValue("@Issues", sdr["Issues"]);
                        cmdInsert.Parameters.AddWithValue("@Category", sdr["Category"]);
                        cmdInsert.Parameters.AddWithValue("@Source", sdr["Source"]);
                        cmdInsert.Parameters.AddWithValue("@Priority", sdr["Priority"]);
                        cmdInsert.ExecuteNonQuery();
                        recordCount++;
                    }
                }
            }
            DeleteEpicStories();
            return recordCount; ;
        }

        private void DeleteEpicStories()
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = "DELETE FROM Stories WHERE AssetState = '208';";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private string GetStoryNumber(string depOid)
        {
            IAssetType assetType = _metaAPI.GetAssetType("Story");
            Query query = new Query(assetType);

            IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(numberAttribute);

            //Filter on Story OID
            IAttributeDefinition assetIDAttribute = assetType.GetAttributeDefinition("ID");
            FilterTerm term = new FilterTerm(assetIDAttribute);
            term.Equal(depOid);
            query.Filter = term;

            QueryResult result = _dataAPI.Retrieve(query);

            Asset asset = result.Assets[0];
            string attributeName = GetScalerValue(asset.GetAttribute(numberAttribute)).ToString();
            return attributeName;
        
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
            sb.Append("BlockingIssues,");
            sb.Append("Issues,");
            sb.Append("Category,");
            sb.Append("Source,");
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
            sb.Append("@BlockingIssues,");
            sb.Append("@Issues,");
            sb.Append("@Category,");
            sb.Append("@Source,");
            sb.Append("@Priority);");
            return sb.ToString();
        }
    }
}
