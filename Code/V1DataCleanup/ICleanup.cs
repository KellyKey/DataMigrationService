using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VersionOne.SDK.APIClient;
using V1DataCore;

namespace V1DataCleanup
{
    abstract public class ICleanup
    {
        protected MetaModel _metaAPI;
        protected Services _dataAPI;
        protected SqlConnection _sqlConn;
        protected MigrationConfiguration _config;

        public ICleanup(SqlConnection sqlConn, MetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
        {
            _sqlConn = sqlConn;
            _metaAPI = MetaAPI;
            _dataAPI = DataAPI;
            _config = Configurations;
        }

        /**************************************************************************************
         * Virtual method that must be implemented in derived classes.
         **************************************************************************************/
        public abstract void Cleanup();

        /**************************************************************************************
        * Protected methods used by derived classes.
        **************************************************************************************/
        protected Asset GetAssetFromV1(string AssetID)
        {
            Oid assetId = Oid.FromToken(AssetID, _metaAPI);
            Query query = new Query(assetId);

            IAssetType assetType = _metaAPI.GetAssetType("Epic");
            IAttributeDefinition assetStateAttribute = assetType.GetAttributeDefinition("AssetState");
            query.Selection.Add(assetStateAttribute);

            QueryResult result = _dataAPI.Retrieve(query);

            if (result.Assets.Count > 0)
                return result.Assets[0];
            else
                return null;
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

        protected Object GetScalerValue(VersionOne.SDK.APIClient.Attribute attribute)
        {
            if (attribute.Value != null)
                return attribute.Value.ToString();
            else
                return DBNull.Value;
        }

    }
}
