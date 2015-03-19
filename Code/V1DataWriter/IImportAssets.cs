using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{
    abstract public class IImportAssets
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        protected MetaModel _metaAPI;
        protected Services _dataAPI;
        protected SqlConnection _sqlConn;
        protected MigrationConfiguration _config;

        protected enum ImportStatuses
        {
            IMPORTED,
            SKIPPED,
            FAILED,
            UPDATED
        }

        public IImportAssets(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
        {
            _sqlConn = sqlConn;
            _metaAPI = MetaAPI;
            _dataAPI = DataAPI;
            _config = Configurations;
        }

        /**************************************************************************************
         * Virtual method that must be implemented in derived classes.
         **************************************************************************************/
        public abstract int Import();

        /**************************************************************************************
        * Protected methods used by derived classes.
         **************************************************************************************/
        protected SqlDataReader GetImportDataFromDBTable(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK);";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        //protected DataTableReader GetImportDataFromDBTableDTR(string TableName)
        //{
        //    DataTable dt = new DataTable();
        //    DataTableReader dtr;
        //    string SQL = "SELECT * FROM " + TableName + ";";
        //    using (SqlCommand cmd = new SqlCommand(SQL, _sqlConn))
        //    {
        //        SqlDataReader sdr = cmd.ExecuteReader();
        //        dt.Load(sdr, LoadOption.OverwriteChanges);
        //        dtr = dt.CreateDataReader();
        //        sdr.Close();
        //    }
        //    return dtr;
        //}

        protected SqlDataReader GetImportDataFromSproc(string SprocName)
        {
            SqlDataReader sdr;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = SprocName;
                sdr = cmd.ExecuteReader();
            }
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableWithOrder(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) ORDER BY [Order] ASC;";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableForClosing(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE AssetState = 'Closed' AND ImportStatus <> 'FAILED' ORDER BY AssetOID DESC;";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableForClosingNoSkipped(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE AssetState = 'Closed' AND ImportStatus <> 'FAILED' AND ImportStatus <> 'SKIPPED' ORDER BY AssetOID DESC;";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableForCustomFields(string AssetType, string FieldName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT a.AssetOID, b.NewAssetOID, a.FieldName, a.FieldType, a.FieldValue FROM CustomFields AS a ");
            sb.Append("INNER JOIN " + AssetType + " AS b ");
            sb.Append("ON a.AssetOID = b.AssetOID ");
            sb.Append("WHERE a.FieldName = '" + FieldName + "' " );
            sb.Append("AND b.ImportStatus <> 'FAILED';");

            SqlCommand cmd = new SqlCommand(sb.ToString(), _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected string CheckForDuplicateInV1(string AssetType, string AttributeName, string AttributeValue)
        {
            //if (AssetType.StartsWith("Custom_"))
            //{
            //    return null;
            //}

            IAssetType assetType = _metaAPI.GetAssetType(AssetType);
            //_logger.Info("CheckForDuplicateInV1: assetType is {0} amd value is {1} ", AssetType, AttributeValue);

            Query query = new Query(assetType);

            IAttributeDefinition valueAttribute = assetType.GetAttributeDefinition(AttributeName);
            query.Selection.Add(valueAttribute);
            FilterTerm idFilter = new FilterTerm(valueAttribute);
            idFilter.Equal(AttributeValue);
            query.Filter = idFilter;
            QueryResult result = _dataAPI.Retrieve(query);

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token.ToString();
            else
                return null;
        }

        protected string CheckForDuplicateIterationByName(string ScheduleOID, string Name)
        {
            IAssetType assetType = _metaAPI.GetAssetType("Timebox");
            Query query = new Query(assetType);

            IAttributeDefinition scheduleAttribute = assetType.GetAttributeDefinition("Schedule");
            query.Selection.Add(scheduleAttribute);
            FilterTerm scheduleFilter = new FilterTerm(scheduleAttribute);
            scheduleFilter.Equal(GetNewAssetOIDFromDB(ScheduleOID));

            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            query.Selection.Add(nameAttribute);
            FilterTerm nameFilter = new FilterTerm(nameAttribute);
            nameFilter.Equal(Name);

            query.Filter = new AndFilterTerm(scheduleFilter, nameFilter);
            QueryResult result = _dataAPI.Retrieve(query);

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token.ToString();
            else
                return null;
        }

        protected string CheckForDuplicateIterationByDate(string ScheduleOID, string BeginDate)
        {
            IAssetType assetType = _metaAPI.GetAssetType("Timebox");
            Query query = new Query(assetType);

            IAttributeDefinition scheduleAttribute = assetType.GetAttributeDefinition("Schedule");
            query.Selection.Add(scheduleAttribute);
            FilterTerm scheduleFilter = new FilterTerm(scheduleAttribute);
            scheduleFilter.Equal(GetNewAssetOIDFromDB(ScheduleOID));

            IAttributeDefinition beginDateAttribute = assetType.GetAttributeDefinition("BeginDate");
            query.Selection.Add(beginDateAttribute);
            FilterTerm beginDateFilter = new FilterTerm(beginDateAttribute);
            beginDateFilter.Equal(BeginDate);

            //IAttributeDefinition endDateAttribute = assetType.GetAttributeDefinition("EndDate");
            //query.Selection.Add(endDateAttribute);
            //FilterTerm endDateFilter = new FilterTerm(endDateAttribute);
            //endDateFilter.Equal(EndDate);

            query.Filter = new AndFilterTerm(scheduleFilter, beginDateFilter);
            QueryResult result = _dataAPI.Retrieve(query);

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token.ToString();
            else
                return null;
        }

        protected string GetNewAssetOIDFromDB(string CurrentAssetOID)
        {
            string tableName = GetTableNameForAsset(CurrentAssetOID);

            if(string.IsNullOrEmpty(tableName) == false)
            {
                string SQL = "SELECT NewAssetOID FROM " + tableName + " WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                string result = (value == null) ? null : value.ToString();

                //SPECIAL CASE: If assetOID was not found and table is "Stories", check the Epics table.
                if (String.IsNullOrEmpty(result) && tableName == "Stories")
                {
                    SQL = "SELECT NewAssetOID FROM Epics WHERE AssetOID = '" + CurrentAssetOID + "';";
                    cmd = new SqlCommand(SQL, _sqlConn);
                    value = cmd.ExecuteScalar();
                    result = (value == null) ? null : value.ToString();
                }
                return result;
            }
            else
            {
                return null;
            }
        }

        protected string GetNewAssetOIDFromDB(string CurrentAssetOID, string AssetType)
        {
            
            //var value;
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewAssetOID FROM " + AssetType + " WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                string result = (value == null) ? null : value.ToString();
                return result;
            }
            else
            {
                return null;
            }
        }

        protected string GetNewEpicAssetOIDFromDB(string CurrentAssetOID)
        {
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewAssetOID FROM Epics WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                string result = (value == null) ? null : value.ToString();
                return result;
            }
            else
            {
                return null;
            }
        }

        protected string GetNewListTypeAssetOIDFromDB(string CurrentAssetOID)
        {
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewAssetOID FROM ListTypes WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                return (string)cmd.ExecuteScalar();
            }
            else
            {
                return null;
            }
        }

        protected string GetNewListTypeAssetOIDFromDB(string ListType, string ListTypeValue)
        {
            if (String.IsNullOrEmpty(ListTypeValue) == false)
            {
                string SQL = "SELECT NewAssetOID FROM ListTypes WHERE AssetOID = '" + ListType + ":" + ListTypeValue.Replace(" ", "").Replace("'", "") + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                return (string)cmd.ExecuteScalar();
            }
            else
            {
                return null;
            }
        }

        protected string GetNewEpicListTypeAssetOIDFromDB(string CurrentAssetOID)
        {
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewEpicAssetOID FROM ListTypes WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                return (string)cmd.ExecuteScalar();
            }
            else
            {
                return null;
            }
        }

        protected string GetCustomListTypeAssetOIDFromV1(string AssetType, string AssetValue)
        {
            IAssetType assetType = _metaAPI.GetAssetType(AssetType);
            Query query = new Query(assetType);
            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
            FilterTerm term = new FilterTerm(nameAttribute);
            term.Equal(AssetValue);
            query.Filter = term;

            QueryResult result;
            try
            {
                result = _dataAPI.Retrieve(query);
            }
            catch
            {
                return null;
            }

            if (result.TotalAvaliable > 0)
                return result.Assets[0].Oid.Token.ToString();
            else
                return null;
        }

        protected void UpdateNewAssetOIDInDB(string Table, string OldAssetOID, string NewAssetOID)
        {
            string SQL = "UPDATE " + Table + " SET NewAssetOID = '" + NewAssetOID + "' WHERE AssetOID = '" + OldAssetOID + "';";

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateNewEpicAssetOIDInDB(string Table, string OldAssetOID, string NewAssetOID)
        {
            string SQL = "UPDATE " + Table + " SET NewEpicAssetOID = '" + NewAssetOID + "' WHERE AssetOID = '" + OldAssetOID + "';";

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateNewAssetNumberInDB(string Table, string OldAssetOID, string NewAssetNumber)
        {
            string SQL = "UPDATE " + Table + " SET NewAssetNumber = '" + NewAssetNumber + "' WHERE AssetOID = '" + OldAssetOID + "';";

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = _sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateNewAssetOIDAndNumberInDB(string Table, string OldAssetOID, string NewAssetOID, string NewAssetNumber)
        {
            StringBuilder sb = new StringBuilder(); 
            sb.Append("UPDATE " + Table + " ");
            sb.Append("SET NewAssetOID = '" + NewAssetOID + "', ");
            sb.Append("NewAssetNumber = '" + NewAssetNumber + "' ");
            sb.Append("WHERE AssetOID = '" + OldAssetOID + "';");

            using (SqlCommand cmd = new SqlCommand(sb.ToString(), _sqlConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateImportStatus(string Table, string AssetOID, ImportStatuses ImportStatus, string ImportDetail)
        {
            //_logger.Info("Status: {0} Message is {1}", AssetOID, ImportDetail);
            
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE " + Table + " ");
            sb.Append("SET ImportStatus = '" + ImportStatus.ToString() + "', ");
            sb.Append("ImportDetails = '" + ImportDetail + "' ");
            sb.Append("WHERE AssetOID = '" + AssetOID + "';");

            using (SqlCommand cmd = new SqlCommand(sb.ToString(), _sqlConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateNewAssetOIDAndStatus(string Table, string OldAssetOID, string NewAssetOID, ImportStatuses ImportStatus, string ImportDetail)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE " + Table + " ");
            sb.Append("SET NewAssetOID = '" + NewAssetOID + "', ");
            sb.Append("ImportStatus = '" + ImportStatus.ToString() + "', ");
            sb.Append("ImportDetails = '" + ImportDetail + "' ");
            sb.Append("WHERE AssetOID = '" + OldAssetOID + "';");

            using (SqlCommand cmd = new SqlCommand(sb.ToString(), _sqlConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        protected void UpdateAssetRecordWithNumber(string Table, string OldAssetOID, string NewAssetOID, string NewAssetNumber, ImportStatuses ImportStatus, string ImportDetail)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE " + Table + " ");
            sb.Append("SET NewAssetOID = '" + NewAssetOID + "', ");
            sb.Append("NewAssetNumber = '" + NewAssetNumber + "', ");
            sb.Append("ImportStatus = '" + ImportStatus.ToString() + "', ");
            sb.Append("ImportDetails = '" + ImportDetail + "' ");
            sb.Append("WHERE AssetOID = '" + OldAssetOID + "';");

            using (SqlCommand cmd = new SqlCommand(sb.ToString(), _sqlConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        protected void ExecuteOperationInV1(string Operation, Oid AssetOID)
        {
            try
            {
                IOperation operation = _metaAPI.GetOperation(Operation);
                Oid oid = _dataAPI.ExecuteOperation(operation, AssetOID);
            }
            catch (APIException ex)
            {
                return;
            }
        }

        protected Asset GetAssetFromV1(string AssetID)
        {
            Oid assetId = Oid.FromToken(AssetID, _metaAPI);
            Query query = new Query(assetId);
            QueryResult result = _dataAPI.Retrieve(query);
            return result.Assets[0];
        }

        protected string GetAssetNumberV1(string AssetType, string AssetID)
        {
            IAssetType assetType = _metaAPI.GetAssetType(AssetType);
            Oid assetId = Oid.FromToken(AssetID, _metaAPI);
            Query query = new Query(assetId);
            IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
            query.Selection.Add(numberAttribute);
            QueryResult result = _dataAPI.Retrieve(query);
            return result.Assets[0].GetAttribute(numberAttribute).Value.ToString();
        }

        protected void AddMultiValueRelation(IAssetType assetType, Asset asset, string attributeName, string valueList)
        {
            IAttributeDefinition customerAttribute = assetType.GetAttributeDefinition(attributeName);
            string[] values = valueList.Split(';');
            foreach (string value in values)
            {
                //SPECIAL CASE: Skip "Member:20" for Scope.Members attribute.
                if (assetType.Token == "Scope" && attributeName == "Members" && value == "Member:20") continue;

                //SPECIAL CASE:  Change the Attribute name to match the correct table for lookup in V1 Staging
                if (attributeName == "Owners")
                {
                    attributeName = "Members";
                }
                else if(attributeName == "BlockingIssues")
                {
                    attributeName = "Issues";
                }

                string newAssetOID = GetNewAssetOIDFromDB(value);
                //_logger.Info("Name is {0} and newAssetOId is {1}", value, newAssetOID);

                if (String.IsNullOrEmpty(newAssetOID) == false)
                {
                    //SPECIAL CASE: Epic conversion issue. If setting story dependants or dependencies, ensure that we do not set for Epic values.
                    if ((attributeName == "Dependants" || attributeName == "Dependencies") && newAssetOID.Contains("Epic"))
                    {
                        continue;
                    }
                    else
                    {
                        asset.AddAttributeValue(customerAttribute, newAssetOID);
                        //_logger.Info("Adding {0} ", newAssetOID);
                    }
                }
                else
                {
                    if (attributeName == "Mentions" || attributeName == "BaseAssets")
                        asset.AddAttributeValue(customerAttribute, "Member:20");
                    //_logger.Info("Adding {0} ", "Admin");
                }
            }
        }

        protected void AddRallyMentionsValue(IAssetType assetType, Asset asset, string baseAssetType, string assetOID)
        {
            IAttributeDefinition mentionsAttribute = assetType.GetAttributeDefinition("Mentions");
            string newAssetOID = String.Empty;

            if (baseAssetType == "Story")
            {
                //First try stories table.
                newAssetOID = GetNewAssetOIDFromDB(assetOID, "Stories");

                //If not found, try epics table.
                if (String.IsNullOrEmpty(newAssetOID) == true)
                {
                    newAssetOID = GetNewAssetOIDFromDB(assetOID, "Epics");
                }
            }
            else if (baseAssetType == "Defect")
            {
                newAssetOID = GetNewAssetOIDFromDB(assetOID, "Defects");
            }
            else if (baseAssetType == "Task")
            {
                newAssetOID = GetNewAssetOIDFromDB(assetOID, "Tasks");
            }
            else if (baseAssetType == "Test")
            {
                //First try tests table.
                newAssetOID = GetNewAssetOIDFromDB(assetOID, "Tests");

                //If not found, try regression tests table.
                if (String.IsNullOrEmpty(newAssetOID) == true)
                {
                    newAssetOID = GetNewAssetOIDFromDB(assetOID, "RegressionTests");
                }
            }

            if (String.IsNullOrEmpty(newAssetOID) == false)
                asset.AddAttributeValue(mentionsAttribute, newAssetOID);

        }

        protected void AddMultiValueRelation(IAssetType assetType, Asset asset, string TableName, string attributeName, string valueList)
        {
            IAttributeDefinition customerAttribute = assetType.GetAttributeDefinition(attributeName);
            string[] values = valueList.Split(';');
            foreach (string value in values)
            {
                //SPECIAL CASE: Skip "Member:20" for Scope.Members attribute.
                if (assetType.Token == "Scope" && attributeName == "Members" && value == "Member:20") continue;

                if (String.IsNullOrEmpty(value)) continue;

                string newAssetOID = GetNewAssetOIDFromDB(value, TableName);

                if (String.IsNullOrEmpty(newAssetOID) == false)

                    //SPECIAL CASE: Epic conversion issue. If setting story dependants or dependencies, ensure that we do not set for Epic values.
                    if ((attributeName == "Dependants" || attributeName == "Dependencies") && newAssetOID.Contains("Epic"))
                    {
                        continue;
                    }
                    else
                    {
                        asset.AddAttributeValue(customerAttribute, newAssetOID);
                    }
            }
        }

        protected string AddV1IDToTitle(string Title, string V1ID)
        {
            if (_config.V1Configurations.AddV1IDToTitles == true)
            {
                //if (V1ID.Contains('-'))
                //{
                //    string[] assetNumber = V1ID.Split('-');
                //    return Title + " (" + assetNumber[1] + ")";
                //}
                //else
                //{
                    return Title + " (" + V1ID + ")";
                //}
            }
            else
            {
                return Title;
            }
        }

        protected string GetV1IDCustomFieldName(string InternalAssetTypeName)
        {
            if (String.IsNullOrEmpty(_config.V1Configurations.CustomV1IDField) == false)
            {
                string customFieldName = String.Empty;

                IAssetType assetType = _metaAPI.GetAssetType("AttributeDefinition");
                Query query = new Query(assetType);

                IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
                query.Selection.Add(nameAttribute);

                IAttributeDefinition isCustomAttribute = assetType.GetAttributeDefinition("IsCustom");
                query.Selection.Add(isCustomAttribute);

                IAttributeDefinition assetNameAttribute = assetType.GetAttributeDefinition("Asset.Name");
                query.Selection.Add(assetNameAttribute);

                FilterTerm assetName = new FilterTerm(assetNameAttribute);
                assetName.Equal(InternalAssetTypeName);
                //assetName.Equal("PrimaryWorkitem");
                FilterTerm isCustom = new FilterTerm(isCustomAttribute);
                isCustom.Equal("true");
                query.Filter = new AndFilterTerm(assetName, isCustom);

                QueryResult result = _dataAPI.Retrieve(query);
                
                foreach (Asset asset in result.Assets)
                {
                    string attributeValue = asset.GetAttribute(nameAttribute).Value.ToString();
                    if (attributeValue.StartsWith(_config.V1Configurations.CustomV1IDField) == true)
                    {
                        customFieldName = attributeValue;
                        break;
                    }
                }
                return customFieldName;
            }
            else
            {
                return null;
            }
        }

        protected string GetTableNameForAsset(string CurrentAssetOID)
        {
            string tableName = String.Empty;
            if (CurrentAssetOID.Contains("Member"))
                tableName = "Members";
            else if (CurrentAssetOID.Contains("MemberLabel"))
                tableName = "MemberGroups";
            else if (CurrentAssetOID.Contains("Team"))
                tableName = "Teams";
            else if (CurrentAssetOID.Contains("Schedule"))
                tableName = "Schedules";
            else if (CurrentAssetOID.Contains("Scope"))
                tableName = "Projects";
            else if (CurrentAssetOID.Contains("ScopeLabel"))
                tableName = "Programs";
            else if (CurrentAssetOID.Contains("Timebox"))
                tableName = "Iterations";
            else if (CurrentAssetOID.Contains("Goal"))
                tableName = "Goals";
            else if (CurrentAssetOID.Contains("Theme"))
                tableName = "FeatureGroups";
            else if (CurrentAssetOID.Contains("Request"))
                tableName = "Requests";
            else if (CurrentAssetOID.Contains("Issue"))
                tableName = "Issues";
            else if (CurrentAssetOID.Contains("Epic"))
                tableName = "Epics";
            else if (CurrentAssetOID.Contains("Story"))
                tableName = "Stories";
            else if (CurrentAssetOID.Contains("Defect"))
                tableName = "Defects";
            else if (CurrentAssetOID.Contains("Task"))
                tableName = "Tasks";
            else if (CurrentAssetOID.Contains("Test"))
                tableName = "Tests";
            else if (CurrentAssetOID.Contains("Link"))
                tableName = "Links";
            else if (CurrentAssetOID.Contains("Expression"))
                tableName = "Conversations";
            else if (CurrentAssetOID.Contains("Actual"))
                tableName = "Actuals";
            else if(CurrentAssetOID.Contains("Attachment"))
                tableName = "Attachments";

            return tableName;
        }

    }
}
