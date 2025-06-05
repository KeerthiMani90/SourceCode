using Ethos.Core;
using Generic.Util;
using Generic.Zip;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Xml;
using System.Collections;
using System.Drawing.Imaging;
//using IDRSTiffZipCreation.com.documentdna.demo;

namespace IDRSTiffZipCreationConversion
{
    [ThreadUsage(Level1MethodResult = ThreadMethodResult.MultipleItems,
       Level2MethodResult = ThreadMethodResult.MultipleItems
        , DedicatedLevel = DedicatedThreadLevel.NoDedicatedLevel)]
    internal class IDRSTiffZipCreation : EthosProcess<DataRow, DataRow, object>
    {
        private SqlDatabase _db = null;
        private readonly DbLayer objdb = new DbLayer();
        private const string MODULE_NAME = "IDRSTiffZipCreation";
        

        public IDRSTiffZipCreation()
            : base(typeof(IDRSTiffZipCreation))
        {
            //_db = new SqlDatabase(Generic.Connection.Driver.ConnectionString);
        }

        protected override IEnumerable<DataRow> GetProcessItems()
        {
            try
            {
                DataSet ds = this.objdb.GetCustomerDs();
                if (ds != null)
                {                    
                    if (ds.Tables[0].Rows.Count > 0)
                        return (IEnumerable<DataRow>)ds.Tables[0].Select();
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("IDRSTiffZipCreation", nameof(GetProcessItems), ex.Message);
            }
            return (IEnumerable<DataRow>)null;
        }
        protected override IEnumerable<DataRow> GetProcessItems(DataRow parent)
        {
            try
            {
                int CustID = Convert.ToInt32(parent["custid"]);
                int ProjID = Convert.ToInt32(parent["projid"]);
                int SubProcessID = Convert.ToInt32(parent["subprocessid"]);
                objdb.UpdateShippedDcn(CustID, ProjID, SubProcessID);
                DataSet ds = this.objdb.GetReadyBatchID(CustID, ProjID, SubProcessID);
                if (ds != null)
                {
                    if (ds.Tables.Count > 0)
                        return (IEnumerable<DataRow>)ds.Tables[0].Select();
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("IDRSTiffZipCreation", "GetProcessItems(DataRow)", ex.Message);
            }
            return (IEnumerable<DataRow>)null;
        }
        protected override void ProcessItem(DataRow parent,DataRow child)
        {
            string sReason = "";
            int CustID = Convert.ToInt32(parent["custid"]);
            int ProjID = Convert.ToInt32(parent["projid"]);
            int SubprocessID = Convert.ToInt32(parent["subprocessid"]);
            string ErrorPath = Convert.ToString(parent["errorurl"]);
            string WorkPath = Convert.ToString(parent["workurl"]);
            string DonePath = Convert.ToString(parent["doneurl"]);
            ////bool isZip = Convert.ToBoolean(child["ISZIP"]);
            string Batchid = Convert.ToString(child["batchid"]);
            string OutputFileName = Convert.ToString(child["outputfilename"]);
            string Filext = Convert.ToString(child["Filext"]);
            DataTable DcnTable = new DataTable();
            DataTable ErrorTable = new DataTable();
            string strpreviousdcn = string.Empty;

            bool Status = true;
            bool val = true;
            ArrayList strFiles = new ArrayList();          
            string errorMessage = string.Empty;
        
            try
            {
                string workurl = Path.Combine(WorkPath, OutputFileName);
                if (!Directory.Exists(workurl))
                {
                    Directory.CreateDirectory(workurl);
                }
                DataSet DcnData = this.objdb.GetReadyDCNDetails(CustID, ProjID, SubprocessID,Batchid);
                if (DcnData == null || DcnData.Tables.Count == 0)
                    return;
                DcnTable.Clear();
                DcnTable = DcnData.Tables[0].DefaultView.ToTable(true, "custid", "projid", "dcn");
                if (DcnData.Tables[0].Rows.Count > 0)
                {
                    string srcpath = string.Empty; string destfilename = string.Empty; string srcfilename=string.Empty;
                    string Dcn = string.Empty;
                    foreach (DataRow dataRow in DcnData.Tables[0].Rows)
                    {
                        Dcn = Convert.ToString(dataRow["dcn"]);
                        srcpath = Convert.ToString(dataRow["srcpath"]);
                        destfilename = Convert.ToString(dataRow["destfilename"]);
                        srcfilename = Convert.ToString(dataRow["srcfilename"]);
                        if (File.Exists(srcpath + "\\" + srcfilename))
                        {
                            File.Copy((srcpath + "\\" + srcfilename),(workurl + "\\" + destfilename));
                            strFiles.Add((object)(srcpath + "\\" + srcfilename));
                        }
                        else
                        {
                            val = objdb.UpdateDcnStatus(CustID, ProjID, SubprocessID, Dcn, Batchid, "ERROR");
                            LogFile.WriteErrorLog("IDRSTiffZipCreation", "Process", "Image not found: " + Dcn);
                        }
                    }
                        
                                        
                    if (strFiles.Count > 0)
                    {
                        if (MakeZip(WorkPath, OutputFileName))
                        {
                            string outputfilename = OutputFileName + Filext;
                            if (MoveFiles(WorkPath, DonePath, outputfilename))
                            {
                                if (objdb.InsertShipDcnSummary(CustID,ProjID, DonePath, outputfilename, strFiles.Count, DcnTable))
                                {
                                    val = objdb.UpdateDcnStatus(CustID, ProjID, SubprocessID, "", Batchid, "COMPLETED");
                                }
                                else
                                {
                                    Status = false; sReason = "ShipSummary insert fails";
                                }
                            }
                            else { Status = false; sReason = "Error in moving files to Done";
                                //LogFile.WriteErrorLog("IDRSTiffZipCreation", "Process", "Error in moving files to Done: " + Batchid);
                            }
                        }
                        else { Status = false; sReason = "Error in Zip";
                           // LogFile.WriteErrorLog("IDRSTiffZipCreation", "Process", "Error in Zip: " + Batchid);
                        }
                    }
                    else { Status = false; sReason = "No files";
                        //LogFile.WriteErrorLog("IDRSTiffZipCreation", "Process", "No files: " + Batchid);
                    }
                    //if (DcnTable.Rows.Count > 0)
                    //{
                    //   this.objdb.Updatestatus(DcnTable, "WIP", "COMPLETED");                       
                    //}
                    //if(ErrorTable.Rows.Count > 0)
                    //{                       
                    //    this.objdb.Updatestatus(ErrorTable, "WIP", "ERROR");                       
                    //}
                    if (Status && val)
                    {
                        this.ShowGrid(parent, child, "COMPLETED", "");
                        if (Directory.Exists(Path.Combine(WorkPath, OutputFileName)))
                        {
                            Directory.Delete(Path.Combine(WorkPath, OutputFileName), true);
                        }
                    }
                    else
                    {
                        this.ShowGrid(parent, child, "ERROR", sReason);
                        LogFile.WriteErrorLog("IDRSTiffZipCreation", "Process", sReason + Batchid);
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(MODULE_NAME, "ProcessItem", ex.ToString());
                return;
            }

        }

        private bool MakeZip(string FileLocation, string outputfilename)
        {
            bool success = false;
            try
            {
                ZipFile zip = new ZipFile(outputfilename + "." + "zip");
                zip.AddDirectory(FileLocation + @"\" + outputfilename);
                zip.Save(FileLocation + @"\" + outputfilename + "." + "zip");
                zip.Dispose();
                success = true;
                return success;
            }
            catch (Exception ex)
            {

                LogFile.WriteErrorLog("", "MakeZip", ex.Message + ":" + outputfilename);
                return success;
            }
        }

        private bool MoveFiles(string workLocation,string doneLocation, string outputfilename)
        {
            bool success = false;
            try
            {
                if (File.Exists(Path.Combine(doneLocation, outputfilename)))
                    File.Delete(Path.Combine(doneLocation, outputfilename));
                File.Move(Path.Combine(workLocation, outputfilename), Path.Combine(doneLocation, outputfilename));
                success = true;
                return success;
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("", "MoveFiles", ex.Message + ":" + outputfilename);
                return success;
            }
        }

        private void ShowGrid(
         DataRow parentrow,
         DataRow drBatchRow,
         string status,
         string reason)
        {
            int int32_1 = Convert.ToInt32(parentrow["CustID"]);
            int int32_2 = Convert.ToInt32(parentrow["ProjID"]);
            string str = Convert.ToString(drBatchRow["OutputFileName"]);
            try
            {
                //((EthosProcessBase<DataRow, DataRow, DataRow>) this).
                OnStatusUpdate(new ProcessEventArgs<DataRow, DataRow, object>()
                {
                    Level1Data = parentrow,
                    Level2Data = drBatchRow,
                    Status = status,
                    ErrorDescription = reason
                });
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("IDRSTiffZipCreation", "clsIDRSTiffZipCreation.ShowGrid", ex.Message + ":" + str, "APPLICATION", int32_1, int32_2);
            }
        }

    private bool MergeFiles(ArrayList strFiles, string sDesPath, string sTiffFileName, string dcn, DataRow parent, ref string errorMessage)
    {
        int int32_1 = Convert.ToInt32(parent["CustID"]);
        int int32_2 = Convert.ToInt32(parent["ProjID"]);
        string upper = Convert.ToString(parent["ConvertToBitonalTif"]).ToUpper();
        try
        {
            TiffManager tiff = new TiffManager();
            if (File.Exists(sDesPath + "\\" + sTiffFileName))
                File.Delete(sDesPath + "\\" + sTiffFileName);
            using (TiffManager tiffManager = new TiffManager())
            {
                tiffManager.JoinTiffImages(strFiles, sDesPath + "\\" + sTiffFileName, EncoderValue.CompressionCCITT4, upper);
            }
            if (File.Exists(sDesPath + "\\" + sTiffFileName))
            {
                tiff.Dispose();
                return true;
            }
            errorMessage = "File " + sTiffFileName + " not exists";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = dcn + ":" + ex.Message;
            LogFile.WriteErrorLog("IDRSTiffZipCreation", "clsIDRSTiffZipCreation.MergeFiles", errorMessage, "APPLICATION", int32_1, int32_2);
            return false;
        }
    }

    }
}
