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
using DOCDNAExtractImageWS.com.documentdna.demo;
//using DOCDNAExtractImageWS.com.documentdna.demo;

namespace DOCDNAExtractImageWSConversion
{
    [ThreadUsage(Level1MethodResult = ThreadMethodResult.MultipleItems,
       Level2MethodResult = ThreadMethodResult.MultipleItems
        , DedicatedLevel = DedicatedThreadLevel.NoDedicatedLevel)]
    internal class DOCDNAExtractImageWS : EthosProcess<DataRow, DataRow, object>
    {
        private SqlDatabase _db = null;
        private readonly DbLayer objdb = new DbLayer();
        private const string MODULE_NAME = "DOCDNAExtractImageWS";
        private DataTable dt = null;

        public DOCDNAExtractImageWS()
            : base(typeof(DOCDNAExtractImageWS))
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
                LogFile.WriteErrorLog("DOCDNAExtractImageWS", nameof(GetProcessItems), ex.Message);
            }
            return (IEnumerable<DataRow>)null;
        }
        protected override IEnumerable<DataRow> GetProcessItems(DataRow parent)
        {
            try
            {
                int CustID = Convert.ToInt32(parent["CustID"]);
                int ProjID = Convert.ToInt32(parent["ProjID"]);
                int ProcessID = Convert.ToInt32(parent["processid"]); ;
                DataSet ds = this.objdb.GetReadyTableConfig(CustID, ProjID, ProcessID);
                if (ds != null)
                {
                    if (ds.Tables.Count > 0)
                        return (IEnumerable<DataRow>)ds.Tables[0].Select();
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("DOCDNAExtractImageWS", "GetProcessItems(DataRow)", ex.Message);
            }
            return (IEnumerable<DataRow>)null;
        }
        protected override void ProcessItem(DataRow parent, DataRow child)
        {
            string sReason = "";
            int CustID = Convert.ToInt32(parent["CustID"]);
            int ProjID = Convert.ToInt32(parent["ProjID"]);
            string spName = Convert.ToString(child["SPNAME"]);
            bool isIndiaFileUrl = Convert.ToBoolean(child["ISINDIAFILEURL"]);
            string PickupPath = Convert.ToString(child["PickupPath"]);
            string WorkPath = Convert.ToString(child["WorkPath"]);
            string DonePath = Convert.ToString(child["DonePath"]);
            bool isZip = Convert.ToBoolean(child["ISZIP"]);
            string OutputFileNameSP = Convert.ToString(child["OutputFileNameSP"]);
            string Filext = Convert.ToString(child["Filext"]);
            DataTable DcnTable = new DataTable();
            DataTable ErrorTable = new DataTable();
            XmlDocument xd = new XmlDocument();
            string index;
            string OutputFileName=null;
            bool val=false;
            FileStream Fs = null;
            String Filename;
            string rptid1=string.Empty; string rptid2=string.Empty;
            bool Status = true;

            string UserName = Convert.ToString(parent["UserName"]);
            string PassWord = Convert.ToString(parent["UserPassWord"]);
            string RptName = Convert.ToString(parent["RptName"]);
            string WebURL = Convert.ToString(parent["WebURL"]);
            //Imagepath = FilePath;            
            try
            {
                DataSet DcnData = this.objdb.GetReadyDCNDetails(CustID, ProjID, 2, spName);
                if (DcnData == null || DcnData.Tables.Count == 0)
                    return;
                if (DcnData.Tables[0].Rows.Count > 0 && DcnData.Tables[1].Rows.Count > 0)
                {
                    OutputFileName = Convert.ToString(DcnData.Tables[1].Rows[0]["OutputFileName"]);
                    string OutputUrl = Path.Combine(WorkPath, OutputFileName);

                    if (!Directory.Exists(OutputUrl))
                    {
                        Directory.CreateDirectory(OutputUrl);
                    }

                    DcnTable.Columns.Add("CUSTID");
                    DcnTable.Columns.Add("PROJID");
                    DcnTable.Columns.Add("DCN");
                    DataRow dr = null;
                    ErrorTable.Columns.Add("CUSTID");
                    ErrorTable.Columns.Add("PROJID");
                    ErrorTable.Columns.Add("DCN");
                    DataRow er = null;

                    #region Old
                    DocDNAWSMTOMSOAP docWebService = new DocDNAWSMTOMSOAP();
                    docWebService.Url = WebURL.Trim();

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    UserInfo userInfo = new UserInfo();
                    userInfo.user = Convert.ToString(UserName).Trim();
                    userInfo.pass = Convert.ToString(PassWord).Trim();

                    Query dnaQuery = new Query();

                    if (RptName.Contains("|"))
                    {
                        string[] RPTIDs = RptName.Split('|');
                        rptid1 = RPTIDs[0];
                        rptid2 = RPTIDs[1];
                    }

                    foreach (DataRow row in DcnData.Tables[0].Rows)
                    {
                        string Dcn = Convert.ToString(row["InputValue"]);
                        string InputReqXML = Convert.ToString(parent["XmlText"]).Replace("$DCN", Dcn);

                        dr = DcnTable.NewRow(); // have new row on each iteration
                        dr["CUSTID"] = CustID;
                        dr["PROJID"] = ProjID;
                        dr["DCN"] = Dcn;
                        er = ErrorTable.NewRow(); // have new row on each iteration
                        er["CUSTID"] = CustID;
                        er["PROJID"] = ProjID;
                        er["DCN"] = Dcn;

                        dnaQuery.RptID = rptid1.Trim();
                        dnaQuery.QueryXML = InputReqXML;
                        Results res = docWebService.DocDNAExtract(userInfo, dnaQuery);
                        string statusxml = res.StatusXML;

                        xd.Load(new StringReader(statusxml));
                        if (xd.InnerText.ToUpper() == "SUCCESS")
                        {                            
                            foreach (Document Docs in res.Items)
                            {
                                index = Docs.IndexDetails;
                                foreach (FileAttachment att in Docs.FileAttachmentList)
                                {
                                    Byte[] Bimg = null;
                                    Bimg = att.Attachment;
                                    Filename = att.FileName;
                                    Fs = new FileStream(OutputUrl + "\\" + Dcn + ".tiff", FileMode.Create);
                                    Fs.Write(Bimg, 0, Bimg.Length);
                                    Fs.Close();
                                    Fs.Dispose();
                                }
                            }
                            DcnTable.Rows.Add(dr);
                            //val = true;
                        }
                        else 
                        {
                            dnaQuery.RptID = rptid2.Trim();
                            Results res1 = docWebService.DocDNAExtract(userInfo, dnaQuery);
                            string statusxml1 = res1.StatusXML;

                            xd.Load(new StringReader(statusxml1));
                            if (xd.InnerText.ToUpper() == "SUCCESS")
                            {
                                foreach (Document Docs in res1.Items)
                                {
                                    index = Docs.IndexDetails;
                                    foreach (FileAttachment att in Docs.FileAttachmentList)
                                    {
                                        Byte[] Bimg = null;
                                        Bimg = att.Attachment;
                                        Filename = att.FileName;
                                        Fs = new FileStream(OutputUrl + "\\" + Dcn + ".tiff", FileMode.Create);
                                        Fs.Write(Bimg, 0, Bimg.Length);
                                        Fs.Close();
                                        Fs.Dispose();
                                    }
                                }
                                DcnTable.Rows.Add(dr);
                                //val = true;
                            }
                            ErrorTable.Rows.Add(er);
                           // val = false;
                        }
                    }

                    if (isZip && DcnTable.Rows.Count >0)
                    {

                        if (MakeZip(WorkPath, OutputFileName))
                        {
                            string outputfilename = OutputFileName + Filext;
                            if (MoveFiles(WorkPath, DonePath, outputfilename))
                            {
                                if (Directory.Exists(Path.Combine(WorkPath, OutputFileName)))
                                {
                                    Directory.Delete(Path.Combine(WorkPath, OutputFileName), true);
                                }
                                if (child["OutputFileNameSP"].ToString() == OutputFileNameSP)
                                    child.SetField("OutputFileNameSP", OutputFileName);                                
                            }
                            else { Status = false; }
                        }
                        else { Status = false; }
                    }
                    else { Status = false; }
                    if (DcnTable.Rows.Count > 0)
                    {
                       this.objdb.Updatestatus(DcnTable, "READY", "COMPLETED");                       
                    }
                    if(ErrorTable.Rows.Count > 0)
                    {                       
                        this.objdb.Updatestatus(ErrorTable, "READY", "ERROR");                       
                    }
                    if(Status)
                        this.ShowGrid(parent, child, "COMPLETED", "");
                    else
                        this.ShowGrid(parent, child, "ERROR", "");
                }

                #endregion
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
            string str = Convert.ToString(drBatchRow["OutputFileNameSP"]);
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
                LogFile.WriteErrorLog("DOCDNAExtractImageWS", "clsDOCDNAExtractImageWS.ShowGrid", ex.Message + ":" + str, "APPLICATION", int32_1, int32_2);
            }
        }






    }
}
