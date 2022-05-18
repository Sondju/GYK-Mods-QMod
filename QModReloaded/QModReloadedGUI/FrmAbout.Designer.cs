﻿using System.ComponentModel;
using System.Windows.Forms;

namespace QModReloadedGUI
{
    partial class FrmAbout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAbout));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.LblTitle = new System.Windows.Forms.Label();
            this.LblCredits = new System.Windows.Forms.Label();
            this.LblCreditsUrl = new System.Windows.Forms.LinkLabel();
            this.LblMyUrl = new System.Windows.Forms.LinkLabel();
            this.BtnOK = new System.Windows.Forms.Button();
            this.TxtVersion = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(-2, -9);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(150, 150);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // LblTitle
            // 
            this.LblTitle.AutoSize = true;
            this.LblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblTitle.Location = new System.Drawing.Point(151, 9);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(353, 40);
            this.LblTitle.TabIndex = 1;
            this.LblTitle.Text = "QMod Manager Reloaded";
            this.LblTitle.UseCompatibleTextRendering = true;
            // 
            // LblCredits
            // 
            this.LblCredits.AutoSize = true;
            this.LblCredits.Location = new System.Drawing.Point(154, 97);
            this.LblCredits.Name = "LblCredits";
            this.LblCredits.Size = new System.Drawing.Size(210, 17);
            this.LblCredits.TabIndex = 2;
            this.LblCredits.Text = "Credits to oldark87 for the QMod system.";
            this.LblCredits.UseCompatibleTextRendering = true;
            // 
            // LblCreditsUrl
            // 
            this.LblCreditsUrl.AutoSize = true;
            this.LblCreditsUrl.Location = new System.Drawing.Point(154, 118);
            this.LblCreditsUrl.Name = "LblCreditsUrl";
            this.LblCreditsUrl.Size = new System.Drawing.Size(233, 17);
            this.LblCreditsUrl.TabIndex = 3;
            this.LblCreditsUrl.TabStop = true;
            this.LblCreditsUrl.Text = "https://github.com/oldark87/GraveyardKeeper";
            this.LblCreditsUrl.UseCompatibleTextRendering = true;
            this.LblCreditsUrl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LblCreditsUrl_LinkClicked);
            // 
            // LblMyUrl
            // 
            this.LblMyUrl.AutoSize = true;
            this.LblMyUrl.Location = new System.Drawing.Point(157, 69);
            this.LblMyUrl.Name = "LblMyUrl";
            this.LblMyUrl.Size = new System.Drawing.Size(239, 17);
            this.LblMyUrl.TabIndex = 4;
            this.LblMyUrl.TabStop = true;
            this.LblMyUrl.Text = "https://github.com/p1xel8ted/GraveyardKeeper";
            this.LblMyUrl.UseCompatibleTextRendering = true;
            this.LblMyUrl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LblMyUrl_LinkClicked);
            // 
            // BtnOK
            // 
            this.BtnOK.Location = new System.Drawing.Point(456, 108);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(41, 23);
            this.BtnOK.TabIndex = 6;
            this.BtnOK.Text = "O&K";
            this.BtnOK.UseCompatibleTextRendering = true;
            this.BtnOK.UseVisualStyleBackColor = true;
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // TxtVersion
            // 
            this.TxtVersion.Location = new System.Drawing.Point(157, 44);
            this.TxtVersion.Name = "TxtVersion";
            this.TxtVersion.ReadOnly = true;
            this.TxtVersion.Size = new System.Drawing.Size(239, 20);
            this.TxtVersion.TabIndex = 7;
            // 
            // FrmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(509, 141);
            this.Controls.Add(this.TxtVersion);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.LblMyUrl);
            this.Controls.Add(this.LblCreditsUrl);
            this.Controls.Add(this.LblCredits);
            this.Controls.Add(this.LblTitle);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAbout";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Load += new System.EventHandler(this.FrmAbout_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PictureBox pictureBox1;
        private Label LblTitle;
        private Label LblCredits;
        private LinkLabel LblCreditsUrl;
        private LinkLabel LblMyUrl;
        private Button BtnOK;
        private TextBox TxtVersion;
    }
}