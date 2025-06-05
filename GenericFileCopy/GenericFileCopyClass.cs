// Decompiled with JetBrains decompiler
// Type: PSUpload.BaUpload
// Assembly: PSUpload, Version=2.0.6828.36913, Culture=neutral, PublicKeyToken=null
// MVID: 5FA42BE7-0A14-4E52-BFFB-5F5B65841F7D
// Assembly location: C:\Users\maniamar\Downloads\PSUpload\PSUpload.exe

using Ethos.Core;
using Generic.Connection;
using Generic.Util;
using Generic.Zip;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Xml;
using System.Linq;
using System.Data.SqlClient;



namespace PSUpload
{
    //[ThreadUsage(DedicatedLevel = DedicatedThreadLevel.NoDedicatedLevel, Level1MethodResult = ThreadMethodResult.MultipleItems, Level2MethodResult = ThreadMethodResult.MultipleItems)]
    [ThreadUsage(Level1MethodResult = ThreadMethodResult.MultipleItems,    DedicatedLevel = DedicatedThreadLevel.NoDedicatedLevel)]
    internal class FileCopyRename : EthosProcess<DataRow, object, object>
    {
        private const string ModuleName = "GenericFileCopy";
        private readonly Database _db;
        private DataSet _dscustomerDs;
        //private DataSet _readyDocuments;


        public FileCopyRename()
          : base(typeof(FileCopyRename))
        {
            try
            {
                this._db = (Database)new SqlDatabase(Driver.ConnectionString);
                //this._db = new SqlDatabase(Generic.Connection.Driver.ConnectionString);
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFileCopy", nameof(FileCopyRename), ex.Message);
            }
        }

        protected override IEnumerable<DataRow> GetProcessItems()
        {
            try
            {
                this._dscustomerDs = this.GetLocationDs();
                if (this._dscustomerDs != null)
                {
                    if (this._dscustomerDs.Tables.Count > 0)
                        return (IEnumerable<DataRow>)this._dscustomerDs.Tables[0].Select();
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFileCopy", nameof(GetProcessItems), ex.Message);
            }
            return (IEnumerable<DataRow>)null;
        }


        protected override void ProcessItem(DataRow parent)
        {
            int CustId = Convert.ToInt32(parent["Custid"]);
            int ProjId = Convert.ToInt32(parent["Projid"]);
            int FormId = Convert.ToInt32(parent["Formid"]);
            int ProcessID = Convert.ToInt32(parent["ProcessID"]);
            int SubprocessID = Convert.ToInt32(parent["SubprocessID"]);
            string IsSubprocessmode = Convert.ToString(parent["IsSubprocessmode"]);
            string PickupURL = Convert.ToString(parent["PickupUrl"]);
            string WorkURL = Convert.ToString(parent["WorkUrl"]);
            string ErrorURL = Convert.ToString(parent["ErrorUrl"]);
            string DoneURL = Convert.ToString(parent["DoneUrl"]);
            string ExtractURL = Convert.ToString(parent["ExtractUrl"]);
            string Prefix = Convert.ToString(parent["FilePrefix"]);
            string Suffix = Convert.ToString(parent["FileSuffix"]);
            string FileExtension = Convert.ToString(parent["FileExtension"]);
            string IsDoneIndiaFileurl = Convert.ToString(parent["IsDoneIndiaFileurl"]);
            string IsPickupIndiafileurl = Convert.ToString(parent["IsPickupIndiafileurl"]);
            string IsOutfileRename = Convert.ToString(parent["IsOutfileRename"]);
            string GetReadySP = Convert.ToString(parent["GetReadySP"]);
            string IsDeleteinSource = Convert.ToString(parent["IsDeleteinSource"]);
            string IsFMInsert = Convert.ToString(parent["IsFMInsert"]);
            string Insertposition = Convert.ToString(parent["Insertposition"]);
            string Mode  = Convert.ToString(parent["Mode"]);
            string IsSem = Convert.ToString(parent["IsSem"]);
            DataSet dsReadyFiles = null;
            string PickupLocation  = string.Empty;
            string DoneLocation = string.Empty;
            string FileName = string.Empty;
            bool flag = false;

            try
            {
                if (!string.IsNullOrEmpty(WorkURL) && !Directory.Exists(WorkURL))
                    Directory.CreateDirectory(WorkURL);
                if (!string.IsNullOrEmpty(ErrorURL) && !Directory.Exists(ErrorURL))
                    Directory.CreateDirectory(ErrorURL);
                if (!string.IsNullOrEmpty(DoneURL) && !Directory.Exists(DoneURL))
                    Directory.CreateDirectory(DoneURL);
           
                dsReadyFiles = GetReadyFiles(CustId, ProjId, FormId,Prefix, Suffix, FileExtension, GetReadySP,IsSubprocessmode,SubprocessID, ProcessID);
                if (dsReadyFiles != null && dsReadyFiles.Tables.Count > 0 && dsReadyFiles.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow file in dsReadyFiles.Tables[0].Rows)
                    {
                        string iPickupurl = string.Empty; string iDoneurl = string.Empty; string iOutputFileName = string.Empty; string iDCN = string.Empty;
                        string iSourceFilename = String.Empty; string iSemFilename = string.Empty;
                        iPickupurl = Convert.ToString(file["pickupurl"]);
                        iDoneurl = Convert.ToString(file["doneurl"]);
                        iOutputFileName = Convert.ToString(file["outputfilename"]);
                        iDCN = Convert.ToString(file["DCN"]);
                        iSourceFilename = Convert.ToString(file["sourcefilename"]);
                        iSemFilename = Path.GetFileNameWithoutExtension(Convert.ToString(file["outputfilename"]))+".sem";

                        PickupLocation = (IsPickupIndiafileurl=="Y") ? iPickupurl : PickupURL;
                        DoneLocation = (IsDoneIndiaFileurl == "Y") ? iDoneurl : DoneURL;
                        
                        DirectoryInfo dir = new DirectoryInfo(PickupLocation);
                        //FileInfo[] Files = dir.GetFiles("*" + iFileName + "*.*");
                        FileInfo[] Files = dir.GetFiles("*" + iSourceFilename + "*");
                        if (IsSubprocessmode == "Y")
                        {
                            if((dir.Exists) && (Files.Length >0))
                            {
                                foreach (FileInfo filein in Files)
                                {
                                    FileName = filein.Name;
                                    if (!Directory.Exists(DoneLocation))
                                    {
                                        if (UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "ERROR",Mode))
                                            this.ShowGrid(parent, DoneLocation, "ERROR", DoneLocation + " Directory not found");
                                    }
                                    else
                                    {
                                        if (File.Exists(Path.Combine(DoneLocation, iOutputFileName)))
                                            File.Delete(Path.Combine(DoneLocation, iOutputFileName));
                                        if (File.Exists(Path.Combine(PickupLocation, FileName)))
                                        {
                                            File.Copy(Path.Combine(PickupLocation, FileName), Path.Combine(DoneLocation, iOutputFileName));
                                            if (IsDeleteinSource == "Y")
                                                File.Delete(Path.Combine(PickupLocation, FileName));
                                            if (IsFMInsert == "Y")
                                            {
                                                flag = InsertDcnInFileMaster(CustId, ProjId, SubprocessID, iDCN, Insertposition, iDCN);
                                                if (flag)
                                                {
                                                    if (UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "COMPLETED", Mode))
                                                        this.ShowGrid(parent, iOutputFileName, "COMPLETED", "");
                                                }
                                                else
                                                {
                                                    if (UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "ERROR", Mode))
                                                        this.ShowGrid(parent, iOutputFileName, "ERROR", "Insert Filemaster Failed");
                                                }
                                            }
                                            else
                                            {
                                                if (UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "COMPLETED", Mode))
                                                    this.ShowGrid(parent, iOutputFileName, "COMPLETED", "");
                                            }
                                            if(IsSem == "Y")
                                            {
                                                CreateSem(DoneLocation, iSemFilename );
                                            }
                                        }
                                        else
                                        {
                                            UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "ERROR", Mode);
                                            this.ShowGrid(parent, FileName, "ERROR", "File not found");
                                        }
                                    }
                                }
                            }
                            else 
                            {
                                UpdateDcnStatus(CustId, ProjId, SubprocessID, iDCN, "ERROR",Mode);
                                this.ShowGrid(parent, FileName, "ERROR", "Directory or file not found");
                            }
                        }
                        if (IsSubprocessmode == "N")
                        {
                            if ((dir.Exists) && (Files.Length > 0))
                            {
                                foreach (FileInfo filein in Files)
                                {
                                    FileName = filein.Name;

                                    if (File.Exists(Path.Combine(DoneLocation, iOutputFileName)))
                                        File.Delete(Path.Combine(DoneLocation, iOutputFileName));
                                    if (File.Exists(Path.Combine(PickupLocation, FileName)))
                                    {
                                        File.Copy(Path.Combine(PickupLocation, FileName), Path.Combine(DoneLocation, iOutputFileName));
                                        if (IsDeleteinSource == "Y")
                                            File.Delete(Path.Combine(PickupLocation, FileName));
                                            this.ShowGrid(parent, FileName, "COMPLETED", "");
                                    }
                                    else
                                    {
                                        this.ShowGrid(parent, FileName, "ERROR", "File not found");
                                    }
                                }
                            }
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                 this.ShowGrid(parent, FileName, "ERROR", ex.Message);
                LogFile.WriteErrorLog("GenericFileCopy.exe", "ProcessItem", ex.Message, "Application", CustId, ProjId);
                return;
            }
     
           }

        public bool CreateSem(string workurl, string SemTxtFileName)
        {
            bool ret = false;
            try
            {
                string txtfilepath = Path.Combine(workurl, SemTxtFileName);
                if (File.Exists(txtfilepath))
                    File.Delete(txtfilepath);
                using (File.Create(txtfilepath)) { }
                ret = true;                   
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFilecopy", "Error in sem txt file creation " + ex, SemTxtFileName);
                if (Directory.Exists(workurl)) Directory.Delete(workurl, recursive: true);
                ret = false;
                //break;
            }
            return ret;
        }
        public bool UpdateDcnStatus(int custId, int projId, int subprocessId, string sDcn, string status,string mode)
        {
            bool bolSuccess = false;
            try
            {
                using (DbCommand dbCmd = this._db.GetStoredProcCommand("Usp_GenericFileCopy_UpdateFileMaster"))
                {
                    _db.AddInParameter(dbCmd, "@custId", DbType.Int32, custId);
                    _db.AddInParameter(dbCmd, "@projId", DbType.Int32, projId);
                    _db.AddInParameter(dbCmd, "@subprocessId", DbType.Int32, subprocessId);
                    _db.AddInParameter(dbCmd, "@status", DbType.String, status);
                    _db.AddInParameter(dbCmd, "@dcn", DbType.String, sDcn);
                    _db.AddInParameter(dbCmd, "@mode", DbType.String, mode);
                    //_db.AddOutParameter(dbCmd, "@Flag", DbType.Int32, 50);
                    int cal= _db.ExecuteNonQuery(dbCmd);
                    if(cal>0)
                        bolSuccess = true;
                    else
                        bolSuccess = false;
                }
            }
            catch (Exception ex)
            {
                bolSuccess = false;
                LogFile.WriteErrorLog("GenericFileCopy", "UpdateDcnStatus", ex.Message + " : " + sDcn);
            }
            return bolSuccess;
        }

        public bool InsertDcnInFileMaster(int custId, int projId, int subprocessId, string sDcn, string position,string imagename)
        {
            bool bolSuccess = false;
            try
            {
                using (DbCommand dbCmd = this._db.GetStoredProcCommand("Usp_GenericFileCopy_InsertFileMaster"))
                {
                    _db.AddInParameter(dbCmd, "@custId", DbType.Int32, custId);
                    _db.AddInParameter(dbCmd, "@projId", DbType.Int32, projId);
                    _db.AddInParameter(dbCmd, "@subprocessId", DbType.Int32, subprocessId);
                    _db.AddInParameter(dbCmd, "@position", DbType.String, position);
                    _db.AddInParameter(dbCmd, "@dcn", DbType.String, sDcn);
                    _db.AddInParameter(dbCmd, "@imagename", DbType.String, imagename);
                    _db.ExecuteNonQuery(dbCmd);
                    bolSuccess = true;
                }
            }
            catch (Exception ex)
            {
                bolSuccess = false;
                LogFile.WriteErrorLog("GenericFileCopy", "InsertDcnInFileMaster", ex.Message + " : " + sDcn);
            }
            return bolSuccess;
        }
        private bool CreateDirectory(string sPath)
        {
            try
            {
                if (!Directory.Exists(sPath))
                    Directory.CreateDirectory(sPath);
                return true;
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog(ModuleName, "ProcessItem", ex.Message);
                return false;
            }
        }

        private void ShowGrid(
          DataRow parentrow,
          string filename,
          string status,
          string reason)
        {
            string int32_1 = Convert.ToString(parentrow["CustName"]);
            string int32_2 = Convert.ToString(parentrow["ProjName"]);
            string outfilename = filename;
            //string str = Convert.ToString(parentrow["outputfilename"]);
            try
            {
                //((EthosProcessBase<DataRow, DataRow, DataRow>) this).
                OnStatusUpdate(new ProcessEventArgs<DataRow, object, object>()
                {
                    Level1Data = parentrow,
                    //Level2Data = childrow,
                   // Level2Data = drBatchRow,
                   AddlInfo1= outfilename,
                    Status = status,
                    ErrorDescription = reason
                });
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFileCopy", "clsTiffcreation.ShowGrid", ex.Message + ":" , "APPLICATION", 0, 0);
            }
        }
        public DataSet GetLocationDs()
        {
            try
            {
                using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("Usp_GenericFileCopy_GetLocationConfig"))
                {
                    return this._db.ExecuteDataSet(storedProcCommand);
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFileCopy", nameof(GetLocationDs), ex.Message);
                return (DataSet)null;
            }
        }
  
        public DataSet GetReadyFiles(int CustId, int ProjId,int formid, string prefix,string suffix, string Extension,string SPName
            ,string IsSubprocessmode, int subprocessid,int processid)
        {
            //string RenameFile = string.Empty;
            DataSet dataSet = null;
            try
            {
                using (DbCommand storedProcCommand = this._db.GetStoredProcCommand(SPName))
                {
                    _db.AddInParameter(storedProcCommand, "@CustId", DbType.Int32, (object)CustId);
                    _db.AddInParameter(storedProcCommand, "@ProjId", DbType.Int32, (object)ProjId);
                    _db.AddInParameter(storedProcCommand, "@SubprocessId", DbType.Int32, (object)subprocessid);
                    _db.AddInParameter(storedProcCommand, "@FormId", DbType.Int32, (object)formid);
                    _db.AddInParameter(storedProcCommand, "@prefix", DbType.String, (object)prefix);
                    _db.AddInParameter(storedProcCommand, "@suffix", DbType.String, (object)suffix);
                    _db.AddInParameter(storedProcCommand, "@FileExt", DbType.String, (object)Extension);
                    _db.AddInParameter(storedProcCommand, "@IsSubprocessmode", DbType.String, (object)IsSubprocessmode);
                    _db.AddInParameter(storedProcCommand, "@ProcessId", DbType.Int32, (object)processid);
                    dataSet = this._db.ExecuteDataSet(storedProcCommand);
                    //if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                    //{
                    //    DataRow row = dataSet.Tables[0].Rows[0];
                    //    RenameFileDoneUrl = row["RenameFileDoneUrl"].ToString();
                    //    RenameFile = row["RenameFile"].ToString();
                        return dataSet;
                   // }
                    //RenameFile = Convert.ToString(this._db.ExecuteScalar(storedProcCommand));
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("GenericFileCopy", nameof(GetLocationDs), ex.Message);
                return null;
            }
            
        }
    }

}
