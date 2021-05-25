
namespace FillDownload
{
    partial class fromTestAPI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtxmsg = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.txtURLparams = new System.Windows.Forms.TextBox();
            this.txtURLBase = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // rtxmsg
            // 
            this.rtxmsg.Location = new System.Drawing.Point(12, 41);
            this.rtxmsg.Name = "rtxmsg";
            this.rtxmsg.Size = new System.Drawing.Size(776, 397);
            this.rtxmsg.TabIndex = 54;
            this.rtxmsg.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(684, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 53;
            this.button1.Text = "调用";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(195, 19);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(77, 12);
            this.label10.TabIndex = 51;
            this.label10.Text = "方法及参数：";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(26, 19);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 12);
            this.label9.TabIndex = 52;
            this.label9.Text = "接口前缀：";
            // 
            // txtURLparams
            // 
            this.txtURLparams.Location = new System.Drawing.Point(278, 14);
            this.txtURLparams.Name = "txtURLparams";
            this.txtURLparams.Size = new System.Drawing.Size(401, 21);
            this.txtURLparams.TabIndex = 49;
            this.txtURLparams.Text = "/account/{accountId}/limits";
            // 
            // txtURLBase
            // 
            this.txtURLBase.Location = new System.Drawing.Point(97, 14);
            this.txtURLBase.Name = "txtURLBase";
            this.txtURLBase.Size = new System.Drawing.Size(86, 21);
            this.txtURLBase.TabIndex = 50;
            this.txtURLBase.Text = "ttaccount";
            // 
            // fromTestAPI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.rtxmsg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txtURLparams);
            this.Controls.Add(this.txtURLBase);
            this.Name = "fromTestAPI";
            this.Text = "fromTestAPI";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtxmsg;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtURLparams;
        private System.Windows.Forms.TextBox txtURLBase;
    }
}