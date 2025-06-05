using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Ethos.Core;
using Generic.Util;
using System.Configuration;

namespace IDRSTiffZipCreationConversion
{
    public partial class IDRSTiffZipCreationConvForm : EthosProcessFormBase
    {
        IDRSTiffZipCreation _spdf = null;
        public IDRSTiffZipCreationConvForm()
        {
            InitializeComponent();
        }

        private void FrmCignaFileConvLoad(object sender, EventArgs e)
        {
            try
            {
                _spdf = new IDRSTiffZipCreation();
                EthosProcess = _spdf;
                string IDRSTiffZipCreationConv = ConfigurationManager.AppSettings["Tool_ExeName"].ToString();
                this.Text = IDRSTiffZipCreationConv;
                _spdf.StatusUpdate += OnStatusUpdate;
                _spdf.ProcessBegin += IDRSTiffZipCreationBegin;
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("IDRSTiffZipCreationConv", "IDRSTiffZipCreationConvLoad", ex.Message);
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        void IDRSTiffZipCreationBegin(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new EventHandler<EventArgs>(IDRSTiffZipCreationBegin), sender,
                    e);
                Application.DoEvents();
                return;
            }
            lvwList.Items.Clear();
        }

        private void OnStatusUpdate(object sender, ProcessEventArgs<DataRow, DataRow, object> e)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(
                        new EventHandler<ProcessEventArgs<DataRow, DataRow, object>>(OnStatusUpdate), sender,
                        e);
                    Application.DoEvents();
                    return;
                }

                DataRow processRow = e.Level1Data;
                DataRow childrow = e.Level2Data;
                string FileName = Convert.ToString(childrow["OutputFileName"]) + "";
                //string CustomerDCN = childrow["DCN"] + "";
                //string CustomerDCN = "";
                string custName = Convert.ToString(processRow["CustName"]);
                string projName = Convert.ToString(processRow["ProjName"]);

                ListViewItem item = null;
                if (lvwList.Items.Count == 100)
                    lvwList.Items[0].Remove();
                if (lvwList.Items.Count > 0)
                    item = lvwList.FindItemWithText(FileName, true, 0);

                if (item != null)
                {
                    item.SubItems[4].Text = FileName;
                    item.SubItems[3].Text = DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
                    item.SubItems[5].Text = e.Status;
                    item.SubItems[6].Text = e.ErrorDescription;
                    item.SubItems[5].ForeColor = e.Status.ToUpper() == "COMPLETED" ? Color.Green : Color.Red;
                }
                else
                {
                    item = new ListViewItem(new[]
                        {
                            Convert.ToString(++ListIndex),
                            custName,
                            projName,
                            DateTime.Now.ToString("MM/dd/yy HH:mm:ss"),
                            FileName,
                            e.Status,
                            e.ErrorDescription
                        });

                    lvwList.Items.Add(item);
                    item.EnsureVisible();
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteErrorLog("IDRSTiffZipCreationConvForm", "BatchStatusUpdate", ex.Message);
            }
        }
    }
}
