using System.ComponentModel;
using System.Windows.Forms;

namespace QModReloadedGUI
{
    partial class FrmChecklist
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmChecklist));
            this.ChkModPatched = new System.Windows.Forms.CheckBox();
            this.ChkNoIntroPatched = new System.Windows.Forms.CheckBox();
            this.ChkModDirectoryExists = new System.Windows.Forms.CheckBox();
            this.ChkGameLocation = new System.Windows.Forms.CheckBox();
            this.ChkMonoCecilExists = new System.Windows.Forms.CheckBox();
            this.Chk0HarmonyExists = new System.Windows.Forms.CheckBox();
            this.ChkPatcherLocation = new System.Windows.Forms.CheckBox();
            this.ChkNewtonExists = new System.Windows.Forms.CheckBox();
            this.ChkGameLoopVDFJson = new System.Windows.Forms.CheckBox();
            this.ChkGameLoopVDF = new System.Windows.Forms.CheckBox();
            this.ChkInjector = new System.Windows.Forms.CheckBox();
            this.ChkConfig = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // ChkModPatched
            // 
            this.ChkModPatched.AutoCheck = false;
            this.ChkModPatched.AutoSize = true;
            this.ChkModPatched.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkModPatched.Location = new System.Drawing.Point(11, 11);
            this.ChkModPatched.Name = "ChkModPatched";
            this.ChkModPatched.Size = new System.Drawing.Size(277, 18);
            this.ChkModPatched.TabIndex = 0;
            this.ChkModPatched.Text = "Mod Patch Applied (Required for mods to function)";
            this.ChkModPatched.UseCompatibleTextRendering = true;
            this.ChkModPatched.UseVisualStyleBackColor = true;
            // 
            // ChkNoIntroPatched
            // 
            this.ChkNoIntroPatched.AutoCheck = false;
            this.ChkNoIntroPatched.AutoSize = true;
            this.ChkNoIntroPatched.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkNoIntroPatched.Location = new System.Drawing.Point(11, 35);
            this.ChkNoIntroPatched.Name = "ChkNoIntroPatched";
            this.ChkNoIntroPatched.Size = new System.Drawing.Size(189, 18);
            this.ChkNoIntroPatched.TabIndex = 1;
            this.ChkNoIntroPatched.Text = "No Intro Patch Applied (Optional)";
            this.ChkNoIntroPatched.UseCompatibleTextRendering = true;
            this.ChkNoIntroPatched.UseVisualStyleBackColor = true;
            // 
            // ChkModDirectoryExists
            // 
            this.ChkModDirectoryExists.AutoCheck = false;
            this.ChkModDirectoryExists.AutoSize = true;
            this.ChkModDirectoryExists.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkModDirectoryExists.Location = new System.Drawing.Point(11, 83);
            this.ChkModDirectoryExists.Name = "ChkModDirectoryExists";
            this.ChkModDirectoryExists.Size = new System.Drawing.Size(127, 18);
            this.ChkModDirectoryExists.TabIndex = 2;
            this.ChkModDirectoryExists.Text = "Mod Directory Exists";
            this.ChkModDirectoryExists.UseCompatibleTextRendering = true;
            this.ChkModDirectoryExists.UseVisualStyleBackColor = true;
            // 
            // ChkGameLocation
            // 
            this.ChkGameLocation.AutoCheck = false;
            this.ChkGameLocation.AutoSize = true;
            this.ChkGameLocation.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkGameLocation.Location = new System.Drawing.Point(11, 59);
            this.ChkGameLocation.Name = "ChkGameLocation";
            this.ChkGameLocation.Size = new System.Drawing.Size(158, 18);
            this.ChkGameLocation.TabIndex = 3;
            this.ChkGameLocation.Text = "Game Location Configured";
            this.ChkGameLocation.UseCompatibleTextRendering = true;
            this.ChkGameLocation.UseVisualStyleBackColor = true;
            // 
            // ChkMonoCecilExists
            // 
            this.ChkMonoCecilExists.AutoCheck = false;
            this.ChkMonoCecilExists.AutoSize = true;
            this.ChkMonoCecilExists.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkMonoCecilExists.Location = new System.Drawing.Point(11, 131);
            this.ChkMonoCecilExists.Name = "ChkMonoCecilExists";
            this.ChkMonoCecilExists.Size = new System.Drawing.Size(341, 18);
            this.ChkMonoCecilExists.TabIndex = 4;
            this.ChkMonoCecilExists.Text = "Mono.Cecil.dll Exists (Required for main application to function)";
            this.ChkMonoCecilExists.UseCompatibleTextRendering = true;
            this.ChkMonoCecilExists.UseVisualStyleBackColor = true;
            // 
            // Chk0HarmonyExists
            // 
            this.Chk0HarmonyExists.AutoCheck = false;
            this.Chk0HarmonyExists.AutoSize = true;
            this.Chk0HarmonyExists.ForeColor = System.Drawing.SystemColors.Desktop;
            this.Chk0HarmonyExists.Location = new System.Drawing.Point(11, 107);
            this.Chk0HarmonyExists.Name = "Chk0HarmonyExists";
            this.Chk0HarmonyExists.Size = new System.Drawing.Size(282, 18);
            this.Chk0HarmonyExists.TabIndex = 5;
            this.Chk0HarmonyExists.Text = "0Harmony.dll Exists (Required for mods to function)";
            this.Chk0HarmonyExists.UseCompatibleTextRendering = true;
            this.Chk0HarmonyExists.UseVisualStyleBackColor = true;
            // 
            // ChkPatcherLocation
            // 
            this.ChkPatcherLocation.AutoCheck = false;
            this.ChkPatcherLocation.AutoSize = true;
            this.ChkPatcherLocation.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkPatcherLocation.Location = new System.Drawing.Point(12, 275);
            this.ChkPatcherLocation.Name = "ChkPatcherLocation";
            this.ChkPatcherLocation.Size = new System.Drawing.Size(630, 18);
            this.ChkPatcherLocation.TabIndex = 6;
            this.ChkPatcherLocation.Text = "Patcher and associated files located in \"Graveyard Keeper_Data\\Managed\" directory" +
    " (Required for everything to function)";
            this.ChkPatcherLocation.UseCompatibleTextRendering = true;
            this.ChkPatcherLocation.UseVisualStyleBackColor = true;
            // 
            // ChkNewtonExists
            // 
            this.ChkNewtonExists.AutoCheck = false;
            this.ChkNewtonExists.AutoSize = true;
            this.ChkNewtonExists.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkNewtonExists.Location = new System.Drawing.Point(11, 155);
            this.ChkNewtonExists.Name = "ChkNewtonExists";
            this.ChkNewtonExists.Size = new System.Drawing.Size(368, 18);
            this.ChkNewtonExists.TabIndex = 7;
            this.ChkNewtonExists.Text = "Newtonsoft.Json.dll Exists (Required for main application to function)";
            this.ChkNewtonExists.UseCompatibleTextRendering = true;
            this.ChkNewtonExists.UseVisualStyleBackColor = true;
            // 
            // ChkGameLoopVDFJson
            // 
            this.ChkGameLoopVDFJson.AutoCheck = false;
            this.ChkGameLoopVDFJson.AutoSize = true;
            this.ChkGameLoopVDFJson.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkGameLoopVDFJson.Location = new System.Drawing.Point(11, 203);
            this.ChkGameLoopVDFJson.Name = "ChkGameLoopVDFJson";
            this.ChkGameLoopVDFJson.Size = new System.Drawing.Size(433, 18);
            this.ChkGameLoopVDFJson.TabIndex = 8;
            this.ChkGameLoopVDFJson.Text = "Gameloop.Vdf.JsonConverter.dll Exists (Required for main application to function)" +
    "";
            this.ChkGameLoopVDFJson.UseCompatibleTextRendering = true;
            this.ChkGameLoopVDFJson.UseVisualStyleBackColor = true;
            // 
            // ChkGameLoopVDF
            // 
            this.ChkGameLoopVDF.AutoCheck = false;
            this.ChkGameLoopVDF.AutoSize = true;
            this.ChkGameLoopVDF.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkGameLoopVDF.Location = new System.Drawing.Point(11, 179);
            this.ChkGameLoopVDF.Name = "ChkGameLoopVDF";
            this.ChkGameLoopVDF.Size = new System.Drawing.Size(357, 18);
            this.ChkGameLoopVDF.TabIndex = 9;
            this.ChkGameLoopVDF.Text = "Gameloop.Vdf.dll Exists (Required for main application to function)";
            this.ChkGameLoopVDF.UseCompatibleTextRendering = true;
            this.ChkGameLoopVDF.UseVisualStyleBackColor = true;
            // 
            // ChkInjector
            // 
            this.ChkInjector.AutoCheck = false;
            this.ChkInjector.AutoSize = true;
            this.ChkInjector.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkInjector.Location = new System.Drawing.Point(11, 227);
            this.ChkInjector.Name = "ChkInjector";
            this.ChkInjector.Size = new System.Drawing.Size(416, 18);
            this.ChkInjector.TabIndex = 10;
            this.ChkInjector.Text = "QModReloaded.dll Exists (Required for main application and mods to function)";
            this.ChkInjector.UseCompatibleTextRendering = true;
            this.ChkInjector.UseVisualStyleBackColor = true;
            // 
            // ChkConfig
            // 
            this.ChkConfig.AutoCheck = false;
            this.ChkConfig.AutoSize = true;
            this.ChkConfig.ForeColor = System.Drawing.SystemColors.Desktop;
            this.ChkConfig.Location = new System.Drawing.Point(11, 251);
            this.ChkConfig.Name = "ChkConfig";
            this.ChkConfig.Size = new System.Drawing.Size(424, 18);
            this.ChkConfig.TabIndex = 11;
            this.ChkConfig.Text = "QModReloadedGUI.exe.config Exists (Required for main application to function)";
            this.ChkConfig.UseCompatibleTextRendering = true;
            this.ChkConfig.UseVisualStyleBackColor = true;
            // 
            // FrmChecklist
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(648, 304);
            this.Controls.Add(this.ChkConfig);
            this.Controls.Add(this.ChkInjector);
            this.Controls.Add(this.ChkGameLoopVDF);
            this.Controls.Add(this.ChkGameLoopVDFJson);
            this.Controls.Add(this.ChkNewtonExists);
            this.Controls.Add(this.ChkPatcherLocation);
            this.Controls.Add(this.Chk0HarmonyExists);
            this.Controls.Add(this.ChkMonoCecilExists);
            this.Controls.Add(this.ChkGameLocation);
            this.Controls.Add(this.ChkModDirectoryExists);
            this.Controls.Add(this.ChkNoIntroPatched);
            this.Controls.Add(this.ChkModPatched);
            this.ForeColor = System.Drawing.Color.Red;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmChecklist";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Checklist";
            this.Load += new System.EventHandler(this.FrmChecklist_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckBox ChkModPatched;
        private CheckBox ChkNoIntroPatched;
        private CheckBox ChkModDirectoryExists;
        private CheckBox ChkGameLocation;
        private CheckBox ChkMonoCecilExists;
        private CheckBox Chk0HarmonyExists;
        private CheckBox ChkPatcherLocation;
        private CheckBox ChkNewtonExists;
        private CheckBox ChkGameLoopVDFJson;
        private CheckBox ChkGameLoopVDF;
        private CheckBox ChkInjector;
        private CheckBox ChkConfig;
    }
}