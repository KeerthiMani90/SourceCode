using System;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Generic.Util;
using Generic.Connection;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace IDRSTiffZipCreationConversion
{
    public class DbLayer
    {
        Database objdb;
        private const string MODULE_NAME = "IDRSTiffZipCreation:DBLayer";
        public DbLayer()
        {
            try
            {
                objdb = new SqlDatabase(Driver.ConnectionString);
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, "DBLayer", ex.Message);
            }
        }

        public DataSet GetCustomerDs()
        {
            try
            {
                using (var dbcmd = this.objdb.GetStoredProcCommand("Usp_IDRSTiffZipCreation_GetCustomer"))
                {
                    return this.objdb.ExecuteDataSet(dbcmd);
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, nameof(GetCustomerDs), ex.Message);
                return (DataSet)null;
            }
        }

        public bool UpdateShippedDcn(int CustId, int ProjId,  int SubprocessId)
        {
            try
            {
                using (DbCommand dbcmd = objdb.GetStoredProcCommand("Usp_IDRSTiffZipCreation_UpdateShippedDcn"))
                {
                    objdb.AddInParameter(dbcmd, "@CustID", DbType.Int32, CustId);
                    objdb.AddInParameter(dbcmd, "@ProjID", DbType.Int32, ProjId);
                    objdb.AddInParameter(dbcmd, "@SubProcessID", DbType.Int32, SubprocessId);
                    int val = objdb.ExecuteNonQuery(dbcmd);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, "UpdateShippedDCN", ex.Message );
                return false;
            }
        }

        public DataSet GetReadyBatchID(int CustID, int ProjID, int SubProcessId)
        {
            try
            {
                using (var dbcmd = this.objdb.GetStoredProcCommand("Usp_IDRSTiffZipCreation_GetBatchid"))
                {
                    objdb.AddInParameter(dbcmd, "@custid", DbType.Int32, CustID);
                    objdb.AddInParameter(dbcmd, "@projid", DbType.Int32, ProjID);
                    objdb.AddInParameter(dbcmd, "@subprocessid", DbType.Int32, SubProcessId);
                    //objdb.AddInParameter(dbcmd, "@FormId", DbType.Int32, FormId);
                    //objdb.AddInParameter(dbcmd, "@SubprocessId", DbType.Int32, SubprocessId);
                    dbcmd.CommandTimeout = 600;
                    return this.objdb.ExecuteDataSet(dbcmd);
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, nameof(GetReadyBatchID), ex.Message);
                return (DataSet)null;
            }
        }

        public DataSet GetReadyDCNDetails(int CustID, int ProjID, int SubprocessID,string Batchid)
        {
            try
            {
                using (var dbcmd = this.objdb.GetStoredProcCommand("Usp_IDRSTiffZipCreation_GetReadyDcns"))
                {
                    objdb.AddInParameter(dbcmd, "@CustId", DbType.Int32, CustID);
                    objdb.AddInParameter(dbcmd, "@ProjId", DbType.Int32, ProjID);
                    objdb.AddInParameter(dbcmd, "@SubprocessId", DbType.Int32, SubprocessID);
                    objdb.AddInParameter(dbcmd, "@Batchid", DbType.String, Batchid);
                    dbcmd.CommandTimeout = 600;
                    return this.objdb.ExecuteDataSet(dbcmd);
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, nameof(GetReadyDCNDetails), ex.Message);
                return (DataSet)null;
            }
        }

        public string GetOutputFilename(int CustID, int ProjID, int Input1, string Input2,string outputspname)
        {
            string output = null;
            try
            {
                using (var dbcmd = this.objdb.GetStoredProcCommand(outputspname))
                {
                    objdb.AddInParameter(dbcmd, "@CustId", DbType.Int32, CustID);
                    objdb.AddInParameter(dbcmd, "@ProjId", DbType.Int32, ProjID);
                    objdb.AddInParameter(dbcmd, "@Input1", DbType.Int32, Input1);
                    objdb.AddInParameter(dbcmd, "@Input2", DbType.String, Input2);
                    dbcmd.CommandTimeout = 600;
                    output = Convert.ToString (this.objdb.ExecuteScalar(dbcmd));
                    return output;
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, nameof(GetReadyDCNDetails), ex.Message);
                return null;
            }
        }

        public bool InsertShipDcnSummary(int custid, int projid, string FileLocation, string outputfilename, int DocCnt, DataTable shiptable)
        {
            bool flag = false;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(this.objdb.ConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand("Usp_IDRSTiffZipCreation_InsertShipSummary"))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.Connection.Open();
                        sqlCommand.Parameters.AddWithValue("@I_Custid", (object)custid);
                        sqlCommand.Parameters.AddWithValue("@I_ProjID", (object)projid);
                        sqlCommand.Parameters.AddWithValue("@I_DocCount", (object)DocCnt);
                        sqlCommand.Parameters.AddWithValue("@I_FileLocation", (object)FileLocation);
                        sqlCommand.Parameters.AddWithValue("@I_outputfilename", (object)outputfilename);
                        sqlCommand.Parameters.AddWithValue("@I_DTTable", (object)shiptable);
                        sqlCommand.ExecuteNonQuery();
                        flag = true;
                        sqlCommand.Connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("ImageShipment.HPSAGood", nameof(InsertShipDcnSummary), ex.Message);
                flag = false;
            }
            return flag;
        }


        public bool UpdateDcnStatus(int custId, int projId,  int subprocessId, string sDcn, string batchid, string status)
        {
            bool bolSuccess = false;
            try
            {
                using (DbCommand dbCmd = objdb.GetStoredProcCommand("Usp_IDRSTiffZipCreation_UpdateFileMaster"))
                {
                    objdb.AddInParameter(dbCmd, "@custId", DbType.Int32, custId);
                    objdb.AddInParameter(dbCmd, "@projId", DbType.Int32, projId);
                    objdb.AddInParameter(dbCmd, "@subprocessId", DbType.Int32, subprocessId);                    
                    objdb.AddInParameter(dbCmd, "@status", DbType.String, status);
                    objdb.AddInParameter(dbCmd, "@dcn", DbType.String, sDcn);
                    objdb.AddInParameter(dbCmd, "@batchid", DbType.String, batchid);
                    //_db.AddOutParameter(dbCmd, "@Flag", DbType.Int32, 50);
                    objdb.ExecuteNonQuery(dbCmd);
                    bolSuccess = true;
                }
            }
            catch (Exception ex)
            {
                bolSuccess = false;
                LogFile.WriteErrorLog(MODULE_NAME, "UpdateDcnStatus", ex.Message + " : " + sDcn);
            }
            return bolSuccess;
        }


    }
}