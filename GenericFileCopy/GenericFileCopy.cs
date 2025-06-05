// Decompiled with JetBrains decompiler
// Type: PSUpload.ProductionsiteUploadFrm
// Assembly: PSUpload, Version=2.0.6828.36913, Culture=neutral, PublicKeyToken=null
// MVID: 5FA42BE7-0A14-4E52-BFFB-5F5B65841F7D
// Assembly location: C:\Users\maniamar\Downloads\PSUpload\PSUpload.exe

using Ethos.Core;
using Generic.Util;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;


namespace PSUpload
{
    public class ProductionsiteUploadFrm : EthosProcessFormBase
    {
        private FileCopyRename _TooCodeRouter;
        private IContainer components;

        public ProductionsiteUploadFrm() => this.InitializeComponent();

        private void ProductionsiteUploadFrm_Load(object sender, EventArgs e)
        {
            try
            {
                this._TooCodeRouter = new FileCopyRename();
                this.EthosProcess = (IEthosProcess)this._TooCodeRouter;
                ((EthosProcessBase<DataRow, object, object>)this._TooCodeRouter).StatusUpdate += new EventHandler<ProcessEventArgs<DataRow, object, object>>(this.BatchStatusUpdate);
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                ((Form)this).Close();
            }
        }

        private void BatchStatusUpdate(object sender, ProcessEventArgs<DataRow, object, object> e)
        {
            try
            {
                if (this.lvwList.InvokeRequired)
                {
                    // ISSUE: explicit non-virtual call
                    //__nonvirtual (((Control) this).
                    BeginInvoke((Delegate)new EventHandler<ProcessEventArgs<DataRow, object, object>>(this.BatchStatusUpdate), sender, (object)e);
                    Application.DoEvents();
                }
                else
                {
                    DataRow level1Data = e.Level1Data;
                    //DataRow level2Data = e.Level2Data;
                    //DataRow level2Data = e.Level2Data;
                    //DataRow level3Data = e.Level3Data;
                    string str1 = Convert.ToString(level1Data["CustName"]);
                    string str2 = Convert.ToString(level1Data["ProjName"]);
                    //string text = Convert.ToString(level2Data["outputfilename"]);
                    string text = e.AddlInfo1;
                    ListViewItem listViewItem = (ListViewItem)null;
                    if (this.lvwList.Items.Count == 100)
                        this.lvwList.Items[0].Remove();
                    if (this.lvwList.Items.Count > 0)
                       // listViewItem = this.lvwList.FindItemWithText("", true, 0);
                     listViewItem = this.lvwList.FindItemWithText(text, true, 0);
                    if (listViewItem != null)
                    {
                        listViewItem.SubItems[2].Text = str2;
                        listViewItem.SubItems[3].Text = DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
                        listViewItem.SubItems[4].Text = e.AddlInfo1;
                        listViewItem.SubItems[5].Text = e.Status;
                        listViewItem.SubItems[6].Text = e.ErrorDescription;
                    }
                    else
                    {
                        string[] items = new string[7];
                        items[0] = Convert.ToString(++this.ListIndex);
                        items[1] = str1;
                        items[2] = str2;
                        items[3] = DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
                        items[4] = text;
                        //items[4] = "";
                        items[5] = e.Status;
                        items[6] = e.ErrorDescription;
                        listViewItem = new ListViewItem(items)
                        {
                            UseItemStyleForSubItems = false
                        };
                        this.lvwList.Items.Add(listViewItem);
                        listViewItem.EnsureVisible();
                    }
                    if (e.Status.ToUpper() == "COMPLETED")
                        listViewItem.SubItems[5].ForeColor = Color.DarkGreen;
                    else if (e.Status.ToUpper() == "WIP")
                        listViewItem.SubItems[5].ForeColor = Color.Blue;
                    else
                        listViewItem.SubItems[5].ForeColor = Color.DarkRed;
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("frmTiffCreation.cs", nameof(BatchStatusUpdate), ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl1.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvwList
            // 
            this.lvwList.Location = new System.Drawing.Point(4, 5);
            this.lvwList.Size = new System.Drawing.Size(1062, 492);
            // 
            // tabControl1
            // 
            this.tabControl1.Location = new System.Drawing.Point(0, 74);
            this.tabControl1.Size = new System.Drawing.Size(1078, 536);
            // 
            // tabMain
            // 
            this.tabMain.Location = new System.Drawing.Point(4, 30);
            this.tabMain.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabMain.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabMain.Size = new System.Drawing.Size(1070, 502);
            // 
            // ProductionsiteUploadFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1078, 646);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "ProductionsiteUploadFrm";
            this.Text = "GenericFileCopy";
            this.Load += new System.EventHandler(this.ProductionsiteUploadFrm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
