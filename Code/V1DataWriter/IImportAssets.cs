using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using VersionOne.SDK.APIClient;
using V1DataCore;
using NLog;

namespace V1DataWriter
{
    abstract public class IImportAssets
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        protected IMetaModel _metaAPI;
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

        public IImportAssets(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
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
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) order by Asset;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus = 'FAILED';";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus = 'Waiting';";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus is null;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where Hash is not null;";
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
                //cmd.CommandType = CommandType.Text;
                //cmd.CommandText = "Select * from EmbeddedImages where Asset is null";
                //cmd.CommandText = "Select * from Attachments where Importstatus = 'FAILED'";
                sdr = cmd.ExecuteReader();
            }
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableWithOrder(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) ORDER BY AssetOID ASC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus = 'FAILED' ORDER BY [Order] ASC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus is null ORDER BY [Order] ASC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) where ImportStatus = 'Waiting' and Description like '%<img src=%' ORDER BY [Order] ASC;";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();
            return sdr;
        }

        protected SqlDataReader GetImportDataFromDBTableForClosing(string TableName)
        {
            string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE AssetState = 'Closed' AND ImportStatus <> 'FAILED' ORDER BY AssetOID DESC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE AssetState = 'Closed' AND ImportStatus = 'FAILED' ORDER BY AssetOID DESC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) ORDER BY AssetOID DESC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE ImportStatus = 'Failed' AND Description like '%<img src=%' ORDER BY AssetOID DESC;";
            //string SQL = "SELECT * FROM " + TableName + " WITH (NOLOCK) WHERE AssetState = 'Closed' AND Description like '%<img src=%' ORDER BY AssetOID DESC;";
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
            sb.Append("WHERE a.FieldName = '" + FieldName + "' ");
            sb.Append("AND b.ImportStatus <> 'FAILED';");
            //sb.Append("AND b.ImportStatus = 'FAILED';");
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


        protected string CheckForDuplicateListTypesInV1(string AssetType, string AttributeName, string AttributeValue, string Attribute2Name, string Attribute2Value)
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

            IAttributeDefinition value2Attribute = assetType.GetAttributeDefinition(Attribute2Name);
            query.Selection.Add(value2Attribute);
            FilterTerm idFilter2 = new FilterTerm(value2Attribute);
            idFilter2.Equal(Attribute2Value);
            query.Filter = new AndFilterTerm(idFilter, idFilter2);

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
                return (value == null) ? null : value.ToString();
            }
            else
            {
                return null;
            }
        }

        protected string GetNewAssetOIDFromDB(string CurrentAssetOID, string AssetType)
        {

            string assetName = GetTableNameForAsset(CurrentAssetOID);

            //var value;
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewAssetOID FROM " + assetName + " WHERE AssetOID = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                return (value == null) ? null : value.ToString();
            }
            else
            {
                return null;
            }
        }

        protected string GetNewAssetOIDFromDBUsingHash(string hashCode, string AssetType)
        {

            string assetName = GetTableNameForAsset(AssetType);

            //var value;
            if (hashCode.Length > 0)
            {
                string SQL = "SELECT NewAssetOID FROM " + assetName + " WHERE Hash = " + hashCode + ";";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                return (value == null) ? null : value.ToString();
            }
            else
            {
                return null;
            }
        }


        protected string GetNewConversationOIDFromDB(string CurrentAssetOID, string AssetType)
        {

            
            if (String.IsNullOrEmpty(CurrentAssetOID) == false)
            {
                string SQL = "SELECT NewConversationOID FROM " + AssetType + " WHERE Conversation = '" + CurrentAssetOID + "';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                var value = cmd.ExecuteScalar();
                return (value == null) ? null : value.ToString();
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
                return (value == null) ? null : value.ToString();
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

        protected void UpdateNewConversationOIDInDB(string Table, string AssetOID, string NewAssetOID)
        {
            string SQL = "UPDATE " + Table + " SET NewConversationOID = '" + NewAssetOID + "' WHERE AssetOID = '" + AssetOID + "';";

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
            _logger.Info("Status: {0} Message is {1}", AssetOID, ImportDetail);
            
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE " + Table + " ");
            sb.Append("SET ImportStatus = '" + ImportStatus.ToString() + "', ");
            sb.Append("ImportDetails = '" + ImportDetail + "' ");
            sb.Append("WHERE AssetOID = '" + AssetOID + "' ");
            sb.Append("OPTION (OPTIMIZE FOR UNKNOWN);");
            string sql = sb.ToString();

            using (SqlCommand cmd = new SqlCommand(sql, _sqlConn))
            {
                cmd.CommandTimeout = 120;
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

            //_logger.Info("Multi Value AttributeName is {0} ", attributeName);

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
            else if (CurrentAssetOID.Contains("Attachment"))
                tableName = "Attachments";
            else if (CurrentAssetOID.Contains("EmbeddedImage"))
                tableName = "EmbeddedImages";
            else
                tableName = "ListTypes";

            return tableName;
        }

        protected void AddMultiText(IAssetType assetType, Asset asset, IAttributeDefinition multiAttribute, string valueList)
        {
            string[] values = null;

            values = valueList.Split(';');

            foreach (string value in values)
            {
                if (String.IsNullOrEmpty(value)) continue;

                asset.AddAttributeValue(multiAttribute, value);
            }
        }

        protected SqlDataReader GetDataFromDB(string lookupTable, string assetOID)
        {
            if (String.IsNullOrEmpty(assetOID) == false)
            {
                string SQL = "SELECT * FROM " + lookupTable + " WHERE Asset = '" + assetOID + "' and ImportStatus = '';";
                SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
                return cmd.ExecuteReader();
            }
            else
            {
                return null;
            }
        }

        protected string [] GetMultiRelationValues(string sourceFieldName, string fieldValue)
        {
            string [] values = fieldValue.Split(';');
            string [] newValues = new string [values.Length];
            int iterator = 0;


            foreach(string smallFieldValue in values)
            {
                string listTypeOID = GetNewAssetOIDFromDB(smallFieldValue);

                newValues[iterator] += listTypeOID + ';';

                iterator++;
            }

            return newValues;
        }

        protected string SetEmbeddedImageContent(string currentDescription, string newURL)
        {
            //This method fixes 2 things in the Description Field
            //First, it re-sets the Old URL from the Source to the New URL from the Target
            //Second, it re-sets the OLD AssetOid to the New AssetOid

            //Get Any EmbeddedImages for the Description Field
            int imageCounter = 0;
            int textPosition = 0;
            string currentEmbeddedImageAssetOID = null;
            string currentEmbeddedImageHash = null;
            string newEmbeddedImageAssetOID = null;
            string newDescription = currentDescription;
            System.Diagnostics.Debug.WriteLine(currentDescription);

            while (true)
            {
                int imgIDLocation = 0;
                string firstDescriptionPart = null;
                string imgDescriptionPart = null;
                string imgPart = null;
                string secondDescriptionPart = null;
                string imgURLDescriptionPart = null;

                imgIDLocation = newDescription.IndexOf("<img src=", textPosition, newDescription.Length - textPosition);
                if (imgIDLocation == -1) break;
                firstDescriptionPart = newDescription.Substring(0, imgIDLocation + 9);
                textPosition = imgIDLocation + 9;

                imgIDLocation = newDescription.IndexOf("alt=", textPosition, newDescription.Length - textPosition);
                imgDescriptionPart = newDescription.Substring(textPosition,imgIDLocation - (textPosition + 1));

                secondDescriptionPart = newDescription.Substring(imgIDLocation, newDescription.Length - imgIDLocation);

                if (imgDescriptionPart.Contains("downloadblob.img"))
                {
                    imgIDLocation = imgDescriptionPart.IndexOf("downloadblob.img", 0, imgDescriptionPart.Length);
                    imgPart = imgDescriptionPart.Substring(imgIDLocation, imgDescriptionPart.Length - imgIDLocation);
                    imgURLDescriptionPart = newURL + "embedded.img/";

                    imgIDLocation = imgPart.IndexOf("img/", 0, imgPart.Length);
                    currentEmbeddedImageHash = imgPart.Substring(imgIDLocation + 4, imgPart.Length - (imgIDLocation + 4));
                    char[] charsToTrim = { '"' };
                    currentEmbeddedImageHash = currentEmbeddedImageHash.Trim(charsToTrim);
                    newEmbeddedImageAssetOID = GetNewAssetOIDFromDBUsingHash("0x" + currentEmbeddedImageHash, "EmbeddedImage");
                }
                else if(imgDescriptionPart.Contains("embedded.img"))
                {
                    imgIDLocation = imgDescriptionPart.IndexOf("embedded.img", 0, imgDescriptionPart.Length);
                    imgPart = imgDescriptionPart.Substring(imgIDLocation, imgDescriptionPart.Length - imgIDLocation);
                    imgURLDescriptionPart = newURL + "embedded.img/";

                    imgIDLocation = imgPart.IndexOf("img/", 0, imgPart.Length);
                    currentEmbeddedImageAssetOID = imgPart.Substring(imgIDLocation + 4, imgPart.Length - (imgIDLocation + 4));
                    char[] charsToTrim = { '"' };
                    currentEmbeddedImageAssetOID = currentEmbeddedImageAssetOID.Trim(charsToTrim);
                    newEmbeddedImageAssetOID = GetNewAssetOIDFromDB("EmbeddedImage:" + currentEmbeddedImageAssetOID);
                }
                else
                {
                    continue;
                }
                    
                imgIDLocation = newEmbeddedImageAssetOID.IndexOf(":", 0, newEmbeddedImageAssetOID.Length);
                newEmbeddedImageAssetOID = newEmbeddedImageAssetOID.Substring(imgIDLocation + 1, newEmbeddedImageAssetOID.Length - (imgIDLocation + 1));

                newDescription = firstDescriptionPart + "\"" + imgURLDescriptionPart + newEmbeddedImageAssetOID + "\" " + secondDescriptionPart;
                System.Diagnostics.Debug.WriteLine(newDescription);

                imageCounter++;
                textPosition = newDescription.Length - secondDescriptionPart.Length;
                _logger.Info("EmbeddedImages Added - Count: " + imageCounter);

               
            }

            return newDescription;
        }
    }
}
