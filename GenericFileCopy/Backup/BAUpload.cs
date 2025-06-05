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

namespace PSUpload
{
  [ThreadUsage]
  internal class BaUpload : EthosProcess<DataRow, DataRow, DataRow>
  {
    private const string ModuleName = "ProductionSiteUpload";
    private readonly Database _db;
    private DataSet _dsPrefissuffix;

    public BaUpload()
      : base(typeof (BaUpload))
    {
      try
      {
        this._db = (Database) new SqlDatabase(Driver.ConnectionString);
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", nameof (BaUpload), ex.Message);
      }
    }

    protected virtual IEnumerable<DataRow> GetProcessItems()
    {
      try
      {
        this._dsPrefissuffix = this.GetPrefixsuffixconfig();
        DataSet customerDs = this.GetCustomerDs();
        if (customerDs != null)
        {
          if (customerDs.Tables.Count > 0)
            return (IEnumerable<DataRow>) customerDs.Tables[0].Select();
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", nameof (GetProcessItems), ex.Message);
      }
      return (IEnumerable<DataRow>) null;
    }

    protected virtual IEnumerable<DataRow> GetProcessItems(DataRow parent)
    {
      try
      {
        DataSet projects = this.GetProjects(Convert.ToInt32(parent["CustID"]), Convert.ToInt32(parent["ProcessID"]), Convert.ToInt32(parent["SubprocessID"]));
        if (projects != null)
        {
          if (projects.Tables.Count > 0)
            return (IEnumerable<DataRow>) projects.Tables[0].Select();
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", "GetProcessItems(DataRow)", ex.Message);
      }
      return (IEnumerable<DataRow>) null;
    }

    protected virtual IEnumerable<DataRow> GetProcessItems(
      DataRow parent,
      DataRow child)
    {
      try
      {
        int int32_1 = Convert.ToInt32(parent["CustID"]);
        int int32_2 = Convert.ToInt32(child["ProjID"]);
        int int32_3 = Convert.ToInt32(parent["ProcessID"]);
        int int32_4 = Convert.ToInt32(parent["SubprocessID"]);
        this.UpdateWipStatus(int32_1, int32_2, int32_3, int32_4);
        DataSet readyDocuments = this.GetReadyDocuments(int32_1, int32_2, int32_3, int32_4);
        if (readyDocuments != null)
        {
          if (readyDocuments.Tables.Count > 0)
            return (IEnumerable<DataRow>) readyDocuments.Tables[0].Select();
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", "GetProcessItems(DataRow)", ex.Message);
      }
      return (IEnumerable<DataRow>) null;
    }

    protected virtual void ProcessItem(DataRow parent, DataRow child, DataRow drBatch)
    {
      int int32_1 = Convert.ToInt32(parent["CustID"]);
      int int32_2 = Convert.ToInt32(child["ProjID"]);
      string sReason = string.Empty;
      try
      {
        int int32_3 = Convert.ToInt32(parent["ProcessID"]);
        int int32_4 = Convert.ToInt32(parent["SubprocessID"]);
        string sProjName = Convert.ToString(child["ProjName"]);
        string str1 = Convert.ToString(drBatch["BATCHID"]);
        string str2 = Convert.ToString(drBatch["INDIAFILEURL"]);
        string sScanDate = Convert.ToString(drBatch["SCANDATE"]);
        string str3 = Convert.ToString(drBatch["BARECVDATE"]);
        int int32_5 = Convert.ToInt32(drBatch["FormId"]);
        string sFormName = Convert.ToString(drBatch["FormName"]);
        string sCustName = Convert.ToString(parent["CustName"]);
        string str4 = Convert.ToString(drBatch["SendUrl"]);
        string empty = string.Empty;
        bool flag = false;
        if (!this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "READY", "WIP"))
          return;
        this.ShowGrid(parent, child, drBatch, "WIP", "");
        this.GetRootPath(int32_1, int32_2, str1, sScanDate, ref empty, sCustName, sProjName, sFormName, drBatch);
        try
        {
          if (str4 == string.Empty)
          {
            sReason = "Send Url is empty:" + str1;
            this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "ERROR");
            LogFile.WriteErrorLog("ProductionSiteUpload.exe", nameof (ProcessItem), sReason, "DATA", int32_1, int32_2);
          }
          if (!Directory.Exists(str4))
            Directory.CreateDirectory(str4);
        }
        catch (Exception ex)
        {
          this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "ERROR");
          string reason = ex.Message + ":" + str1;
          LogFile.WriteErrorLog("ProductionSiteUpload.exe", nameof (ProcessItem), reason, "DATA", int32_1, int32_2);
          this.ShowGrid(parent, child, drBatch, "ERROR", reason);
          return;
        }
        if (this.CreateDir(int32_1, int32_2, str2, str1))
        {
          DataRow[] drPrefixsuffix = this._dsPrefissuffix.Tables[0].Select("CustId=" + (object) int32_1 + " And ProjId=" + (object) int32_2 + "  And TransmitalMode<>'FILE'");
          DataRow[] drPrefixsuffixFile = this._dsPrefissuffix.Tables[0].Select("CustId=" + (object) int32_1 + " And ProjId=" + (object) int32_2 + "  And TransmitalMode='FILE'  And (isnull(FilePrefix,'')<>'' or isnull(FileSuffix,'')<>'' or  isnull(FileExtension,'')<>'' or isnull(PickUpFolder,'')<>'' or  isnull(DoneFolder,'')<>'')");
          DataSet readyImages = this.GetReadyImages(int32_1, int32_2, int32_3, int32_4, str1);
          if (readyImages == null || readyImages.Tables[0].Rows.Count == 0)
          {
            this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "READY");
            LogFile.WriteErrorLog("ProductionSiteUpload.exe", nameof (ProcessItem), "Images selection Failed:" + str1, "DATA", int32_1, int32_2);
            return;
          }
          if (this.CopyImages(int32_1, int32_2, readyImages, Path.Combine(str2, str1), ref sReason, drPrefixsuffixFile))
          {
            DataTable table = readyImages.Tables[0].DefaultView.ToTable(true, "Dcn");
            if (this.CopySupportFiles(int32_1, int32_2, (IEnumerable<DataRow>) drPrefixsuffix, str2, str1, table, ref sReason))
            {
              if (str3 != string.Empty)
                this.CreateFile(int32_1, int32_2, Path.Combine(Path.Combine(str2, str1), "priority.sem"));
              this.CreateFile(int32_1, int32_2, Path.Combine(Path.Combine(str2, str1), str1 + ".sem"));
              if (this.MakeZip(int32_1, int32_2, str1, Path.Combine(str2, str1), str4, empty) && this.CreateFile(int32_1, int32_2, Path.Combine(str4, str1 + ".sem")))
                flag = true;
            }
          }
          this.DeleteZipFile(int32_1, int32_2, str2, str1);
        }
        if (flag)
        {
          if (!this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "COMPLETED"))
            this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "READY");
          else
            this.ShowGrid(parent, child, drBatch, "COMPLETED", sReason);
        }
        else if (sReason == string.Empty)
        {
          this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "READY");
          this.ShowGrid(parent, child, drBatch, "READY", sReason);
        }
        else
        {
          this.UpdateBatchStatus(int32_1, int32_2, int32_5, int32_3, int32_4, str1, "WIP", "ERROR");
          this.ShowGrid(parent, child, drBatch, "ERROR", sReason);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.CreateDir", ex.Message, "Application", int32_1, int32_2);
      }
    }

    private bool CreateDir(int iCustId, int iProjId, string sIndiaFileUrl, string sBatchId)
    {
      try
      {
        if (Directory.Exists(sIndiaFileUrl + "\\" + sBatchId))
          Directory.Delete(sIndiaFileUrl + "\\" + sBatchId, true);
        Directory.CreateDirectory(Path.Combine(sIndiaFileUrl, sBatchId));
        return true;
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.CreateDir", ex.Message, "Application", iCustId, iProjId);
        return false;
      }
    }

    private bool CreateFile(int iCustId, int iProjId, string path)
    {
      try
      {
        if (File.Exists(path))
          File.Delete(path);
        using (new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
          ;
        return true;
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.CreateFile", ex.Message, "Application", iCustId, iProjId);
        return false;
      }
    }

    private void GetRootPath(
      int iCustId,
      int iProjId,
      string sBatchId,
      string sScanDate,
      ref string sRootPath,
      string sCustName,
      string sProjName,
      string sFormName,
      DataRow drDcn)
    {
      try
      {
        string str1 = string.Empty;
        string str2 = drDcn["IncludeSubDirectory"].ToString();
        string str3 = drDcn["SubDirName"].ToString();
        if (str2.ToUpper() != "Y")
        {
          sRootPath = sBatchId;
        }
        else
        {
          if (str3 == string.Empty)
            str3 = "CP";
          for (int startIndex = 0; startIndex <= str3.Length - 1; ++startIndex)
          {
            string str4 = str3.Substring(startIndex, 1);
            if (str4.ToUpper() == "C")
              str1 = str1 + "\\" + sCustName;
            else if (str4.ToUpper() == "P")
              str1 = str1 + "\\" + sProjName;
            else if (str4.ToUpper() == "F")
            {
              str1 = str1 + "\\" + sFormName;
            }
            else
            {
              str1 = sCustName + "\\" + sProjName;
              break;
            }
          }
          sRootPath = "indata\\" + str1 + "\\" + sScanDate + "\\" + sBatchId;
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.GetRootPath", ex.Message, "Application", iCustId, iProjId);
      }
    }

    private void DeleteZipFile(int iCustId, int iProjId, string sIndiaFileUrl, string sBatchId)
    {
      try
      {
        if (!(sBatchId != string.Empty))
          return;
        if (Directory.Exists(sIndiaFileUrl + "\\" + sBatchId))
          Directory.Delete(sIndiaFileUrl + "\\" + sBatchId, true);
        if (!File.Exists(sIndiaFileUrl + "\\" + sBatchId + ".zip"))
          return;
        File.Delete(sIndiaFileUrl + "\\" + sBatchId + ".zip");
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.GetRootPath", ex.Message, "Application", iCustId, iProjId);
      }
    }

    private bool MakeZip(
      int iCustId,
      int iProjId,
      string sBatchId,
      string sSourceLoc,
      string sDescLoc,
      string sRootPath)
    {
      try
      {
        using (ZipFile zipFile = new ZipFile())
        {
          zipFile.AddDirectory(sSourceLoc, sRootPath);
          zipFile.Save(Path.Combine(sDescLoc, sBatchId + ".zip"));
        }
        return true;
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "starter.MakeZip", ex.Message, "Application", iCustId, iProjId);
        return false;
      }
    }

    private bool CopySupportFiles(
      int iCustId,
      int iProjId,
      IEnumerable<DataRow> drPrefixsuffix,
      string sIndiaFileUrl,
      string sBatchId,
      DataTable dtDcn,
      ref string sReason)
    {
      try
      {
        string empty = string.Empty;
        foreach (DataRow dataRow in drPrefixsuffix)
        {
          string str1 = Convert.ToString(dataRow["FileExtension"]);
          string str2 = Convert.ToString(dataRow["MandatoryFlag"]);
          string str3 = Convert.ToString(dataRow["TransmitalMode"]);
          string str4 = Convert.ToString(dataRow["FilePrefix"]);
          string str5 = Convert.ToString(dataRow["FileSuffix"]);
          string path2_1 = Convert.ToString(dataRow["PickUpFolder"]);
          string path2_2 = Convert.ToString(dataRow["DoneFolder"]);
          sIndiaFileUrl = Path.Combine(sIndiaFileUrl, path2_1);
          string path = Path.Combine(sIndiaFileUrl + "\\" + sBatchId, path2_2);
          string str6 = path2_2;
          if (path2_2 != string.Empty && path2_2 != str6)
            Directory.CreateDirectory(path);
          if (str3.ToUpper() == "BATCH")
          {
            string str7;
            if (str1 != string.Empty)
              str7 = str4 + sBatchId + str5 + "." + str1;
            else
              str7 = str4 + sBatchId + str5;
            if (File.Exists(sIndiaFileUrl + "\\" + str7))
              File.Copy(sIndiaFileUrl + "\\" + str7, path + "\\" + str7, true);
            else if (str2.ToUpper() == "Y")
            {
              sReason = "File " + sIndiaFileUrl + "\\" + str7 + " not found";
              LogFile.WriteErrorLog("ProductionSiteUpload.exe", "BaUpload.CopySupportFiles", sReason, "Application", iCustId, iProjId);
              return false;
            }
          }
          else if (str3.ToUpper() == "DCN")
          {
            foreach (DataRow row in (InternalDataCollectionBase) dtDcn.Rows)
            {
              string str8 = Convert.ToString(row["DCN"]);
              string str9;
              if (str1 != string.Empty)
                str9 = str4 + str8 + str5 + "." + str1;
              else
                str9 = str4 + str8 + str5;
              if (File.Exists(sIndiaFileUrl + "\\" + str9))
                File.Copy(sIndiaFileUrl + "\\" + str9, path + "\\" + str9, true);
              else if (str2.ToUpper() == "Y")
              {
                sReason = "File " + sIndiaFileUrl + "\\" + str9 + " not found";
                LogFile.WriteErrorLog("ProductionSiteUpload.exe", "BAUpload.CopySupportFiles", sReason, "Application", iCustId, iProjId);
                return false;
              }
            }
          }
          else if (str3.ToUpper() == "STATIC")
          {
            string str10 = !(str1 != string.Empty) ? str4 : str4 + "." + str1;
            if (File.Exists(sIndiaFileUrl + "\\" + str10))
              File.Copy(sIndiaFileUrl + "\\" + str10, path + "\\" + str10, true);
            else if (str2.ToUpper() == "Y")
            {
              sReason = "File " + sIndiaFileUrl + "\\" + str10 + " not found";
              LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.CopySupportFiles", sReason, "Application", iCustId, iProjId);
              return false;
            }
          }
          else if (str3.ToUpper() == "NEWBATCH")
          {
            string path2_3;
            if (str1 != string.Empty)
              path2_3 = str4 + sBatchId + str5 + "." + str1;
            else
              path2_3 = str4 + sBatchId + str5;
            if (File.Exists(sIndiaFileUrl + "\\" + path2_3))
            {
              if (!File.Exists(sIndiaFileUrl + "\\" + sBatchId + "\\" + path2_3))
                File.Copy(sIndiaFileUrl + "\\" + path2_3, path + "\\" + path2_3, true);
            }
            else
              this.CreateFile(iCustId, iProjId, Path.Combine(Path.Combine(sIndiaFileUrl, sBatchId), path2_3));
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "Starter.CopySupportFiles", ex.Message, "Application", iCustId, iProjId);
        return false;
      }
    }

    private bool CopyImages(
      int icustId,
      int iProjId,
      DataSet dsImages,
      string sDestPath,
      ref string sReason,
      DataRow[] drPrefixsuffixFile)
    {
      try
      {
        string str1 = string.Empty;
        foreach (DataRow row in (InternalDataCollectionBase) dsImages.Tables[0].Rows)
        {
          string path2_1 = Convert.ToString(row["FileName"]);
          string path1 = Convert.ToString(row["IndiaFileUrl"]);
          if (File.Exists(Path.Combine(path1, path2_1)))
          {
            File.Copy(Path.Combine(path1, path2_1), Path.Combine(sDestPath, path2_1), true);
            foreach (DataRow dataRow in drPrefixsuffixFile)
            {
              string str2 = Convert.ToString(dataRow["FileExtension"]);
              string upper = Convert.ToString(dataRow["MandatoryFlag"]).ToUpper();
              string str3 = Convert.ToString(dataRow["FilePrefix"]);
              string str4 = Convert.ToString(dataRow["FileSuffix"]);
              string path2_2 = Convert.ToString(dataRow["PickUpFolder"]);
              string path2_3 = Convert.ToString(dataRow["DoneFolder"]);
              path1 = Path.Combine(path1, path2_2);
              string empty1 = string.Empty;
              string str5 = Path.Combine(sDestPath, path2_3);
              if (path2_3 != string.Empty && path2_3 != str1)
                Directory.CreateDirectory(str5);
              str1 = path2_3;
              string empty2 = string.Empty;
              string path2_4;
              if (str2 != string.Empty)
                path2_4 = str3 + path2_1 + str4 + "." + str2;
              else
                path2_4 = str3 + path2_1 + str4;
              if (path2_1.ToLower() != path2_4.ToLower() || path2_2 != string.Empty)
              {
                if (File.Exists(Path.Combine(path1, path2_4)))
                  File.Copy(Path.Combine(path1, path2_4), Path.Combine(str5, path2_4), true);
                else if (upper != "Y")
                {
                  sReason = "File " + Path.Combine(path1, path2_4) + " not found";
                  LogFile.WriteErrorLog("ProductionSiteUpload.exe", "starter.CopyImages", sReason, "DATA", icustId, iProjId);
                  return false;
                }
              }
            }
          }
          else
          {
            sReason = "File " + Path.Combine(path1, path2_1) + " not found";
            LogFile.WriteErrorLog("ProductionSiteUpload.exe", "starter.CopyImages", sReason, "DATA", icustId, iProjId);
            return false;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "starter.CopyImages", ex.Message, "Application", icustId, iProjId);
        return false;
      }
    }

    private void ShowGrid(
      DataRow parentrow,
      DataRow childrow,
      DataRow drBatchRow,
      string status,
      string reason)
    {
      int int32_1 = Convert.ToInt32(parentrow["CustID"]);
      int int32_2 = Convert.ToInt32(childrow["ProjID"]);
      string str = Convert.ToString(drBatchRow["BatchId"]);
      try
      {
        ((EthosProcessBase<DataRow, DataRow, DataRow>) this).OnStatusUpdate(new ProcessEventArgs<DataRow, DataRow, DataRow>()
        {
          Level1Data = parentrow,
          Level2Data = childrow,
          Level3Data = drBatchRow,
          Status = status,
          ErrorDescription = reason
        });
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", "clsTiffcreation.ShowGrid", ex.Message + ":" + str, "APPLICATION", int32_1, int32_2);
      }
    }

    public DataSet GetPrefixsuffixconfig()
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("Usp_BAUpload_GetPrefixsuffixconfig"))
          return this._db.ExecuteDataSet(storedProcCommand);
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("TiffCreation", "GetCustomerDs", ex.Message);
        return (DataSet) null;
      }
    }

    public DataSet GetCustomerDs()
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("usp_BAUpload_GetSubprocess"))
        {
          this._db.AddInParameter(storedProcCommand, "@SubprocessName", DbType.String, (object) "BAUPLOAD");
          return this._db.ExecuteDataSet(storedProcCommand);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("TiffCreation", nameof (GetCustomerDs), ex.Message);
        return (DataSet) null;
      }
    }

    private DataSet GetProjects(int custId, int processId, int subprocessId)
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("usp_BAUpload_GetProject"))
        {
          this._db.AddInParameter(storedProcCommand, "@CustID", DbType.Int32, (object) custId);
          this._db.AddInParameter(storedProcCommand, "@ProcessID", DbType.Int32, (object) processId);
          this._db.AddInParameter(storedProcCommand, "@SubprocessID", DbType.Int32, (object) subprocessId);
          return this._db.ExecuteDataSet(storedProcCommand);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", "GetReadyDocuments", ex.Message, "ERROR", custId, 0);
        return (DataSet) null;
      }
    }

    private void UpdateWipStatus(int icustId, int iProjId, int iProcessId, int iSubProcessId)
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("usp_Subprocess_UpdateWiptoReady"))
        {
          this._db.AddInParameter(storedProcCommand, "@CustId", DbType.Int32, (object) icustId);
          this._db.AddInParameter(storedProcCommand, "@ProjId", DbType.Int32, (object) iProjId);
          this._db.AddInParameter(storedProcCommand, "@ProcessId", DbType.Int32, (object) iProcessId);
          this._db.AddInParameter(storedProcCommand, "@SubProcessId", DbType.Int32, (object) iSubProcessId);
          this._db.ExecuteNonQuery(storedProcCommand);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "clsStarter.ShowGrid", ex.Message, "APPLICATION", icustId, iProjId);
      }
    }

    private DataSet GetReadyDocuments(
      int custId,
      int projId,
      int processId,
      int subprocessId)
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("usp_BaUpload_GetReadyDocuments"))
        {
          this._db.AddInParameter(storedProcCommand, "@CustID", DbType.Int32, (object) custId);
          this._db.AddInParameter(storedProcCommand, "@ProjId", DbType.Int32, (object) projId);
          this._db.AddInParameter(storedProcCommand, "@ProcessID", DbType.Int32, (object) processId);
          this._db.AddInParameter(storedProcCommand, "@SubprocessID", DbType.Int32, (object) subprocessId);
          return this._db.ExecuteDataSet(storedProcCommand);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", nameof (GetReadyDocuments), ex.Message, "ERROR", custId, projId);
        return (DataSet) null;
      }
    }

    private DataSet GetReadyImages(
      int custId,
      int projId,
      int processId,
      int subprocessId,
      string sBatchId)
    {
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("usp_BAUpload_GetImages"))
        {
          this._db.AddInParameter(storedProcCommand, "@CustID", DbType.Int32, (object) custId);
          this._db.AddInParameter(storedProcCommand, "@ProjId", DbType.Int32, (object) projId);
          this._db.AddInParameter(storedProcCommand, "@ProcessID", DbType.Int32, (object) processId);
          this._db.AddInParameter(storedProcCommand, "@SubprocessID", DbType.Int32, (object) subprocessId);
          this._db.AddInParameter(storedProcCommand, "@BatchId", DbType.String, (object) sBatchId);
          return this._db.ExecuteDataSet(storedProcCommand);
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload", "GetReadyDocuments", ex.Message, "ERROR", custId, projId);
        return (DataSet) null;
      }
    }

    private bool UpdateBatchStatus(
      int iCustId,
      int iProjId,
      int iFormId,
      int iProcessId,
      int iSubProcessId,
      string sBatchId,
      string sCurrentStatus,
      string sChangeStatus)
    {
      bool flag = false;
      try
      {
        using (DbCommand storedProcCommand = this._db.GetStoredProcCommand("Usp_PSUpload_UpdateBatchStatus"))
        {
          this._db.AddInParameter(storedProcCommand, "@CustId", DbType.Int32, (object) iCustId);
          this._db.AddInParameter(storedProcCommand, "@ProjId", DbType.Int32, (object) iProjId);
          this._db.AddInParameter(storedProcCommand, "@FormId", DbType.Int32, (object) iFormId);
          this._db.AddInParameter(storedProcCommand, "@ProcessId", DbType.Int32, (object) iProcessId);
          this._db.AddInParameter(storedProcCommand, "@SubProcessId", DbType.Int32, (object) iSubProcessId);
          this._db.AddInParameter(storedProcCommand, "@BatchId", DbType.String, (object) sBatchId);
          this._db.AddInParameter(storedProcCommand, "@CurrentStatus", DbType.String, (object) sCurrentStatus);
          this._db.AddInParameter(storedProcCommand, "@ChangeStatus", DbType.String, (object) sChangeStatus);
          this._db.AddOutParameter(storedProcCommand, "@iRowAff", DbType.Int32, 0);
          this._db.ExecuteNonQuery(storedProcCommand);
          if (Convert.ToInt32(this._db.GetParameterValue(storedProcCommand, "@iRowAff")) > 0)
            flag = true;
        }
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("ProductionSiteUpload.exe", "clsStarter.UpdateBatchStatus", ex.Message, "APPLICATION", iCustId, iProjId);
      }
      return flag;
    }
  }
}
