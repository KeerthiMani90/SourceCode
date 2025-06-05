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
    private BaUpload _baUpload;
    private IContainer components;

    public ProductionsiteUploadFrm() => this.InitializeComponent();

    private void ProductionsiteUploadFrm_Load(object sender, EventArgs e)
    {
      try
      {
        this._baUpload = new BaUpload();
        this.EthosProcess = (IEthosProcess) this._baUpload;
        ((EthosProcessBase<DataRow, DataRow, DataRow>) this._baUpload).StatusUpdate += new EventHandler<ProcessEventArgs<DataRow, DataRow, DataRow>>(this.BatchStatusUpdate);
      }
      catch (Exception ex)
      {
        int num = (int) MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        ((Form) this).Close();
      }
    }

    private void BatchStatusUpdate(object sender, ProcessEventArgs<DataRow, DataRow, DataRow> e)
    {
      try
      {
        if (this.lvwList.InvokeRequired)
        {
          // ISSUE: explicit non-virtual call
          __nonvirtual (((Control) this).BeginInvoke((Delegate) new EventHandler<ProcessEventArgs<DataRow, DataRow, DataRow>>(this.BatchStatusUpdate), sender, (object) e));
          Application.DoEvents();
        }
        else
        {
          DataRow level1Data = e.Level1Data;
          DataRow level2Data = e.Level2Data;
          DataRow level3Data = e.Level3Data;
          string str1 = Convert.ToString(level1Data["CustName"]);
          string str2 = Convert.ToString(level2Data["ProjName"]);
          string text = Convert.ToString(level3Data["BatchId"]);
          ListViewItem listViewItem = (ListViewItem) null;
          if (this.lvwList.Items.Count == 100)
            this.lvwList.Items[0].Remove();
          if (this.lvwList.Items.Count > 0)
            listViewItem = this.lvwList.FindItemWithText(text, true, 0);
          if (listViewItem != null)
          {
            listViewItem.SubItems[2].Text = str2;
            listViewItem.SubItems[3].Text = DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
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
        LogFile.WriteErrorLog("frmTiffCreation.cs", nameof (BatchStatusUpdate), ex.Message);
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof (ProductionsiteUploadFrm));
      ((Control) this).SuspendLayout();
      ((ContainerControl) this).AutoScaleDimensions = new SizeF(6f, 13f);
      ((ContainerControl) this).AutoScaleMode = AutoScaleMode.Font;
      ((Form) this).ClientSize = new Size(719, 400);
      ((Form) this).Icon = (Icon) componentResourceManager.GetObject("$this.Icon");
      ((Control) this).Name = nameof (ProductionsiteUploadFrm);
      ((Control) this).Text = "ProductionsiteUpload";
      ((Form) this).Load += new EventHandler(this.ProductionsiteUploadFrm_Load);
      ((Control) this).ResumeLayout(false);
      ((Control) this).PerformLayout();
    }
  }
}
