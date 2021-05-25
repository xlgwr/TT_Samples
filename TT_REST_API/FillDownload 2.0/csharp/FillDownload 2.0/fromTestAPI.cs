using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FillDownload
{
    public partial class fromTestAPI : Form
    {
        public FrmFillDownload frmFillDownload;
        public fromTestAPI()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (!initapi())
                {
                    return;
                }

                button1.Enabled = false;
                string targetstr = txtURLBase.Text;
                string urlparams = txtURLparams.Text;
                var result = RestManager.GetRequest(targetstr, urlparams);
                rtxmsg.Clear();
                rtxmsg.AppendText(result.Content);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                button1.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }
        bool initapi()
        {
            string app_key = frmFillDownload.txtSecret.Text.Split(':')[0];
            string app_secret = frmFillDownload.txtSecret.Text;
            RestManager.Init(app_key, app_secret, frmFillDownload.txtEnvironment.Text, frmFillDownload.txtURL.Text);
            if (!RestManager.IsAuthorized())
            {
                MessageBox.Show("Rest API was not able to log in with provided App Key and Secret");
                return false;
            }
            else
            {
                rtxmsg.AppendText("Successfully logged in with app key and secret"); 
            }
            return true;
        }
    }
}
