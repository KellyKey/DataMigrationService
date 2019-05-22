using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace V1DataCore
{
    public static class MigrationStats
    {
        public static void WriteStat(SqlConnection sqlConn, string StatName, string StatValue)
        {
            string SQL = "INSERT INTO MigrationStats VALUES ('" + StatName + "', '" + StatValue + "', GETDATE());";

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = sqlConn;
                cmd.CommandText = SQL;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        public static string ReadStat(SqlConnection sqlConn, string StatName)
        {
            string SQL = "SELECT * FROM MigrationStats WHERE StatName = '" + StatName + "';";
            SqlCommand cmd = new SqlCommand(SQL, sqlConn);
            return (string)cmd.ExecuteScalar();
        }
    }
}
