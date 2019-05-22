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
    public class CleanupRegressionTests : ICleanup
    {
        public CleanupRegressionTests(SqlConnection sqlConn, IMetaModel MetaAPI, Services DataAPI, MigrationConfiguration Configurations)
            : base(sqlConn, MetaAPI, DataAPI, Configurations) { }

        public override void Cleanup()
        {
            OpenAllRegressionTests();
        }

        //Sets all RegressionTests found in staging DB to open in target instance.
        private void OpenAllRegressionTests()
        {
            string SQL = "SELECT * FROM TESTS WHERE NewAssetOID LIKE 'RegressionTest%'";
            SqlCommand cmd = new SqlCommand(SQL, _sqlConn);
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                Asset asset = GetAssetFromV1(sdr["NewAssetOID"].ToString());
                ExecuteOperationInV1("RegressionTest.Reactivate", asset.Oid);
                Console.WriteLine("Updated regression test {0}.", asset.Oid.ToString());
            }
            sdr.Close();
        }
    
    }
}
