using System.ComponentModel;
using System.Windows.Forms;

namespace QModReloadedGUI
{
    partial class FrmMain
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.TxtGameLocation = new System.Windows.Forms.TextBox();
            this.TxtModFolderLocation = new System.Windows.Forms.TextBox();
            this.BtnBrowse = new System.Windows.Forms.Button();
            this.LblGameLocation = new System.Windows.Forms.Label();
            this.LblModFolderLocation = new System.Windows.Forms.Label();
            this.LblInstalledMods = new System.Windows.Forms.Label();
            this.BtnPatch = new System.Windows.Forms.Button();
            this.LblModInfo = new System.Windows.Forms.Label();
            this.TxtModInfo = new System.Windows.Forms.TextBox();
            this.DlgBrowse = new System.Windows.Forms.FolderBrowserDialog();
            this.BtnAddMod = new System.Windows.Forms.Button();
            this.BtnRemove = new System.Windows.Forms.Button();
            this.BtnRunGame = new System.Windows.Forms.PictureBox();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.BtnRemovePatch = new System.Windows.Forms.Button();
            this.BtnRefresh = new System.Windows.Forms.Button();
            this.BtnRemoveIntros = new System.Windows.Forms.Button();
            this.BtnOpenGameDir = new System.Windows.Forms.Button();
            this.BtnOpenModDir = new System.Windows.Forms.Button();
            this.BtnOpenLog = new System.Windows.Forms.Button();
            this.DgvMods = new System.Windows.Forms.DataGridView();
            this.ChOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChMod = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checklistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LblLog = new System.Windows.Forms.Label();
            this.TxtLog = new System.Windows.Forms.TextBox();
            this.ToolStrip = new System.Windows.Forms.ToolStrip();
            this.LblPatched = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.LblIntroPatched = new System.Windows.Forms.ToolStripLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.checklistToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.modifyResolutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.DlgFile = new System.Windows.Forms.OpenFileDialog();
            this.LblConfig = new System.Windows.Forms.Label();
            this.TxtConfig = new System.Windows.Forms.TextBox();
            this.LblSaved = new System.Windows.Forms.Label();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayIconCtxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.restoreWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.launchGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openmModDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openGameDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.BtnRunGame)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DgvMods)).BeginInit();
            this.ToolStrip.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.trayIconCtxMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // TxtGameLocation
            // 
            this.TxtGameLocation.Location = new System.Drawing.Point(12, 29);
            this.TxtGameLocation.Name = "TxtGameLocation";
            this.TxtGameLocation.Size = new System.Drawing.Size(469, 20);
            this.TxtGameLocation.TabIndex = 0;
            this.ToolTip.SetToolTip(this.TxtGameLocation, "Directory that contains Graveyard Keeper.exe");
            // 
            // TxtModFolderLocation
            // 
            this.TxtModFolderLocation.Location = new System.Drawing.Point(12, 84);
            this.TxtModFolderLocation.Name = "TxtModFolderLocation";
            this.TxtModFolderLocation.ReadOnly = true;
            this.TxtModFolderLocation.Size = new System.Drawing.Size(469, 20);
            this.TxtModFolderLocation.TabIndex = 1;
            this.ToolTip.SetToolTip(this.TxtModFolderLocation, "This cannot be changed due to the nature of QMods.");
            // 
            // BtnBrowse
            // 
            this.BtnBrowse.Location = new System.Drawing.Point(406, 53);
            this.BtnBrowse.Margin = new System.Windows.Forms.Padding(1);
            this.BtnBrowse.Name = "BtnBrowse";
            this.BtnBrowse.Size = new System.Drawing.Size(75, 23);
            this.BtnBrowse.TabIndex = 2;
            this.BtnBrowse.Text = "&Browse";
            this.ToolTip.SetToolTip(this.BtnBrowse, "Browse for directory manually if it fails to auto-detect.");
            this.BtnBrowse.UseCompatibleTextRendering = true;
            this.BtnBrowse.UseVisualStyleBackColor = true;
            this.BtnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // LblGameLocation
            // 
            this.LblGameLocation.AutoSize = true;
            this.LblGameLocation.Location = new System.Drawing.Point(12, 13);
            this.LblGameLocation.Name = "LblGameLocation";
            this.LblGameLocation.Size = new System.Drawing.Size(81, 17);
            this.LblGameLocation.TabIndex = 3;
            this.LblGameLocation.Text = "Game Location";
            this.LblGameLocation.UseCompatibleTextRendering = true;
            // 
            // LblModFolderLocation
            // 
            this.LblModFolderLocation.AutoSize = true;
            this.LblModFolderLocation.Location = new System.Drawing.Point(12, 64);
            this.LblModFolderLocation.Name = "LblModFolderLocation";
            this.LblModFolderLocation.Size = new System.Drawing.Size(107, 17);
            this.LblModFolderLocation.TabIndex = 4;
            this.LblModFolderLocation.Text = "Mod Folder Location";
            this.LblModFolderLocation.UseCompatibleTextRendering = true;
            // 
            // LblInstalledMods
            // 
            this.LblInstalledMods.AutoSize = true;
            this.LblInstalledMods.Location = new System.Drawing.Point(12, 120);
            this.LblInstalledMods.Name = "LblInstalledMods";
            this.LblInstalledMods.Size = new System.Drawing.Size(77, 17);
            this.LblInstalledMods.TabIndex = 5;
            this.LblInstalledMods.Text = "Installed Mods";
            this.LblInstalledMods.UseCompatibleTextRendering = true;
            // 
            // BtnPatch
            // 
            this.BtnPatch.Location = new System.Drawing.Point(218, 53);
            this.BtnPatch.Margin = new System.Windows.Forms.Padding(1);
            this.BtnPatch.Name = "BtnPatch";
            this.BtnPatch.Size = new System.Drawing.Size(92, 23);
            this.BtnPatch.TabIndex = 7;
            this.BtnPatch.Text = "&Apply Patch";
            this.ToolTip.SetToolTip(this.BtnPatch, "Applies the mod patch.");
            this.BtnPatch.UseCompatibleTextRendering = true;
            this.BtnPatch.UseVisualStyleBackColor = true;
            this.BtnPatch.Click += new System.EventHandler(this.BtnPatch_Click);
            // 
            // LblModInfo
            // 
            this.LblModInfo.AutoSize = true;
            this.LblModInfo.Location = new System.Drawing.Point(312, 120);
            this.LblModInfo.Name = "LblModInfo";
            this.LblModInfo.Size = new System.Drawing.Size(48, 17);
            this.LblModInfo.TabIndex = 9;
            this.LblModInfo.Text = "Mod Info";
            this.LblModInfo.UseCompatibleTextRendering = true;
            // 
            // TxtModInfo
            // 
            this.TxtModInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtModInfo.Location = new System.Drawing.Point(312, 140);
            this.TxtModInfo.Multiline = true;
            this.TxtModInfo.Name = "TxtModInfo";
            this.TxtModInfo.ReadOnly = true;
            this.TxtModInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TxtModInfo.Size = new System.Drawing.Size(250, 132);
            this.TxtModInfo.TabIndex = 11;
            // 
            // BtnAddMod
            // 
            this.BtnAddMod.Location = new System.Drawing.Point(12, 436);
            this.BtnAddMod.Name = "BtnAddMod";
            this.BtnAddMod.Size = new System.Drawing.Size(75, 23);
            this.BtnAddMod.TabIndex = 13;
            this.BtnAddMod.Text = "A&dd Mod";
            this.ToolTip.SetToolTip(this.BtnAddMod, "Adds a new mod.");
            this.BtnAddMod.UseCompatibleTextRendering = true;
            this.BtnAddMod.UseVisualStyleBackColor = true;
            this.BtnAddMod.Click += new System.EventHandler(this.BtnAddMod_Click);
            // 
            // BtnRemove
            // 
            this.BtnRemove.Location = new System.Drawing.Point(93, 436);
            this.BtnRemove.Name = "BtnRemove";
            this.BtnRemove.Size = new System.Drawing.Size(84, 23);
            this.BtnRemove.TabIndex = 14;
            this.BtnRemove.Text = "&Remove Mod";
            this.ToolTip.SetToolTip(this.BtnRemove, "Removes the selected mod(s).");
            this.BtnRemove.UseCompatibleTextRendering = true;
            this.BtnRemove.UseVisualStyleBackColor = true;
            this.BtnRemove.Click += new System.EventHandler(this.BtnRemove_Click);
            // 
            // BtnRunGame
            // 
            this.BtnRunGame.Image = ((System.Drawing.Image)(resources.GetObject("BtnRunGame.Image")));
            this.BtnRunGame.Location = new System.Drawing.Point(487, 29);
            this.BtnRunGame.Name = "BtnRunGame";
            this.BtnRunGame.Size = new System.Drawing.Size(75, 75);
            this.BtnRunGame.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.BtnRunGame.TabIndex = 16;
            this.BtnRunGame.TabStop = false;
            this.ToolTip.SetToolTip(this.BtnRunGame, "Click to launch Graveyard Keeper. Launches via Steam first, then by the EXE direc" +
        "tly if Steam fails for whatever reason.");
            this.BtnRunGame.Click += new System.EventHandler(this.BtnRunGame_Click);
            // 
            // BtnRemovePatch
            // 
            this.BtnRemovePatch.Location = new System.Drawing.Point(312, 53);
            this.BtnRemovePatch.Margin = new System.Windows.Forms.Padding(1);
            this.BtnRemovePatch.Name = "BtnRemovePatch";
            this.BtnRemovePatch.Size = new System.Drawing.Size(92, 23);
            this.BtnRemovePatch.TabIndex = 17;
            this.BtnRemovePatch.Text = "Remove Pa&tch";
            this.ToolTip.SetToolTip(this.BtnRemovePatch, "Removes the mod patch only.");
            this.BtnRemovePatch.UseCompatibleTextRendering = true;
            this.BtnRemovePatch.UseVisualStyleBackColor = true;
            this.BtnRemovePatch.Click += new System.EventHandler(this.BtnRemovePatch_Click);
            // 
            // BtnRefresh
            // 
            this.BtnRefresh.Location = new System.Drawing.Point(183, 436);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(53, 23);
            this.BtnRefresh.TabIndex = 18;
            this.BtnRefresh.Text = "Re&fresh";
            this.ToolTip.SetToolTip(this.BtnRefresh, "Click if you\'ve installed mods externally.");
            this.BtnRefresh.UseCompatibleTextRendering = true;
            this.BtnRefresh.UseVisualStyleBackColor = true;
            this.BtnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // BtnRemoveIntros
            // 
            this.BtnRemoveIntros.Location = new System.Drawing.Point(467, 435);
            this.BtnRemoveIntros.Name = "BtnRemoveIntros";
            this.BtnRemoveIntros.Size = new System.Drawing.Size(95, 23);
            this.BtnRemoveIntros.TabIndex = 20;
            this.BtnRemoveIntros.Text = "Remove &Intros";
            this.ToolTip.SetToolTip(this.BtnRemoveIntros, "Removes intros (permanently).");
            this.BtnRemoveIntros.UseCompatibleTextRendering = true;
            this.BtnRemoveIntros.UseVisualStyleBackColor = true;
            this.BtnRemoveIntros.Click += new System.EventHandler(this.BtnRemoveIntros_Click);
            // 
            // BtnOpenGameDir
            // 
            this.BtnOpenGameDir.Location = new System.Drawing.Point(467, 110);
            this.BtnOpenGameDir.Name = "BtnOpenGameDir";
            this.BtnOpenGameDir.Size = new System.Drawing.Size(99, 23);
            this.BtnOpenGameDir.TabIndex = 27;
            this.BtnOpenGameDir.Text = "Ope&n Game Dir";
            this.ToolTip.SetToolTip(this.BtnOpenGameDir, "Open the game directory in Explorer");
            this.BtnOpenGameDir.UseVisualStyleBackColor = true;
            this.BtnOpenGameDir.Click += new System.EventHandler(this.BtnOpenGameDir_Click);
            // 
            // BtnOpenModDir
            // 
            this.BtnOpenModDir.Location = new System.Drawing.Point(371, 110);
            this.BtnOpenModDir.Name = "BtnOpenModDir";
            this.BtnOpenModDir.Size = new System.Drawing.Size(90, 23);
            this.BtnOpenModDir.TabIndex = 28;
            this.BtnOpenModDir.Text = "Open M&od Dir";
            this.ToolTip.SetToolTip(this.BtnOpenModDir, "Open the mod directory in Explorer");
            this.BtnOpenModDir.UseCompatibleTextRendering = true;
            this.BtnOpenModDir.UseVisualStyleBackColor = true;
            this.BtnOpenModDir.Click += new System.EventHandler(this.BtnOpenModDir_Click);
            // 
            // BtnOpenLog
            // 
            this.BtnOpenLog.Location = new System.Drawing.Point(242, 436);
            this.BtnOpenLog.Name = "BtnOpenLog";
            this.BtnOpenLog.Size = new System.Drawing.Size(64, 23);
            this.BtnOpenLog.TabIndex = 29;
            this.BtnOpenLog.Text = "Open &Log";
            this.ToolTip.SetToolTip(this.BtnOpenLog, "Open the log file in your default editor.");
            this.BtnOpenLog.UseVisualStyleBackColor = true;
            this.BtnOpenLog.Click += new System.EventHandler(this.BtnOpenLog_Click);
            // 
            // DgvMods
            // 
            this.DgvMods.AllowDrop = true;
            this.DgvMods.AllowUserToAddRows = false;
            this.DgvMods.AllowUserToDeleteRows = false;
            this.DgvMods.BackgroundColor = System.Drawing.SystemColors.Control;
            this.DgvMods.CausesValidation = false;
            this.DgvMods.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DgvMods.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ChOrder,
            this.ChMod,
            this.ChEnabled});
            this.DgvMods.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.DgvMods.GridColor = System.Drawing.SystemColors.Control;
            this.DgvMods.Location = new System.Drawing.Point(13, 140);
            this.DgvMods.Name = "DgvMods";
            this.DgvMods.ReadOnly = true;
            this.DgvMods.RowHeadersVisible = false;
            this.DgvMods.ShowEditingIcon = false;
            this.DgvMods.Size = new System.Drawing.Size(293, 289);
            this.DgvMods.TabIndex = 35;
            this.ToolTip.SetToolTip(this.DgvMods, "Drag n Drop to re-order mods. Mods will load in the order they appear in this lis" +
        "t.");
            this.DgvMods.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvMods_CellClick);
            this.DgvMods.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvMods_CellContentClick);
            this.DgvMods.DragDrop += new System.Windows.Forms.DragEventHandler(this.DgvMods_DragDrop);
            this.DgvMods.DragOver += new System.Windows.Forms.DragEventHandler(this.DgvMods_DragOver);
            this.DgvMods.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DgvMods_MouseDown);
            this.DgvMods.MouseMove += new System.Windows.Forms.MouseEventHandler(this.DgvMods_MouseMove);
            // 
            // ChOrder
            // 
            this.ChOrder.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.ChOrder.HeaderText = "Order";
            this.ChOrder.Name = "ChOrder";
            this.ChOrder.ReadOnly = true;
            this.ChOrder.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.ChOrder.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.ChOrder.Width = 58;
            // 
            // ChMod
            // 
            this.ChMod.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ChMod.HeaderText = "Mod";
            this.ChMod.Name = "ChMod";
            this.ChMod.ReadOnly = true;
            this.ChMod.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ChEnabled
            // 
            this.ChEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.ChEnabled.HeaderText = "Enabled";
            this.ChEnabled.Name = "ChEnabled";
            this.ChEnabled.ReadOnly = true;
            this.ChEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.ChEnabled.Width = 52;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(67, 22);
            // 
            // checklistToolStripMenuItem
            // 
            this.checklistToolStripMenuItem.Name = "checklistToolStripMenuItem";
            this.checklistToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "&About";
            // 
            // LblLog
            // 
            this.LblLog.AutoSize = true;
            this.LblLog.Location = new System.Drawing.Point(13, 466);
            this.LblLog.Name = "LblLog";
            this.LblLog.Size = new System.Drawing.Size(23, 17);
            this.LblLog.TabIndex = 23;
            this.LblLog.Text = "Log";
            this.LblLog.UseCompatibleTextRendering = true;
            // 
            // TxtLog
            // 
            this.TxtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtLog.Location = new System.Drawing.Point(12, 486);
            this.TxtLog.Multiline = true;
            this.TxtLog.Name = "TxtLog";
            this.TxtLog.ReadOnly = true;
            this.TxtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TxtLog.Size = new System.Drawing.Size(550, 108);
            this.TxtLog.TabIndex = 24;
            // 
            // ToolStrip
            // 
            this.ToolStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LblPatched,
            this.toolStripSeparator2,
            this.LblIntroPatched});
            this.ToolStrip.Location = new System.Drawing.Point(0, 606);
            this.ToolStrip.Name = "ToolStrip";
            this.ToolStrip.Size = new System.Drawing.Size(578, 25);
            this.ToolStrip.TabIndex = 25;
            this.ToolStrip.Text = "toolStrip1";
            // 
            // LblPatched
            // 
            this.LblPatched.Name = "LblPatched";
            this.LblPatched.Size = new System.Drawing.Size(86, 22);
            this.LblPatched.Text = "toolStripLabel1";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // LblIntroPatched
            // 
            this.LblIntroPatched.Name = "LblIntroPatched";
            this.LblIntroPatched.Size = new System.Drawing.Size(86, 22);
            this.LblIntroPatched.Text = "toolStripLabel2";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem1,
            this.checklistToolStripMenuItem1,
            this.modifyResolutionToolStripMenuItem,
            this.aboutToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(578, 24);
            this.menuStrip1.TabIndex = 26;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem1
            // 
            this.fileToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem1});
            this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
            this.fileToolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem1.Text = "F&ile";
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(93, 22);
            this.exitToolStripMenuItem1.Text = "E&xit";
            this.exitToolStripMenuItem1.Click += new System.EventHandler(this.ExitToolStripMenuItem1_Click);
            // 
            // checklistToolStripMenuItem1
            // 
            this.checklistToolStripMenuItem1.Name = "checklistToolStripMenuItem1";
            this.checklistToolStripMenuItem1.Size = new System.Drawing.Size(67, 20);
            this.checklistToolStripMenuItem1.Text = "C&hecklist";
            this.checklistToolStripMenuItem1.ToolTipText = "Click to see if your installation is valid for mods to function.";
            this.checklistToolStripMenuItem1.Click += new System.EventHandler(this.ChecklistToolStripMenuItem1_Click);
            // 
            // modifyResolutionToolStripMenuItem
            // 
            this.modifyResolutionToolStripMenuItem.Name = "modifyResolutionToolStripMenuItem";
            this.modifyResolutionToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
            this.modifyResolutionToolStripMenuItem.Text = "&Modify Resolution";
            this.modifyResolutionToolStripMenuItem.Click += new System.EventHandler(this.ModifyResolutionToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem1
            // 
            this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
            this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem1.Text = "A&bout";
            this.aboutToolStripMenuItem1.Click += new System.EventHandler(this.AboutToolStripMenuItem1_Click);
            // 
            // DlgFile
            // 
            this.DlgFile.FileName = "openFileDialog1";
            this.DlgFile.Multiselect = true;
            this.DlgFile.Title = "Select ZIP file( s)";
            // 
            // LblConfig
            // 
            this.LblConfig.AutoSize = true;
            this.LblConfig.Location = new System.Drawing.Point(312, 275);
            this.LblConfig.Name = "LblConfig";
            this.LblConfig.Size = new System.Drawing.Size(37, 17);
            this.LblConfig.TabIndex = 30;
            this.LblConfig.Text = "Config";
            this.LblConfig.UseCompatibleTextRendering = true;
            // 
            // TxtConfig
            // 
            this.TxtConfig.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtConfig.Location = new System.Drawing.Point(312, 295);
            this.TxtConfig.Multiline = true;
            this.TxtConfig.Name = "TxtConfig";
            this.TxtConfig.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TxtConfig.Size = new System.Drawing.Size(250, 134);
            this.TxtConfig.TabIndex = 31;
            this.TxtConfig.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtConfig_KeyUp);
            this.TxtConfig.Leave += new System.EventHandler(this.TxtConfig_Leave);
            // 
            // LblSaved
            // 
            this.LblSaved.AutoSize = true;
            this.LblSaved.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblSaved.ForeColor = System.Drawing.Color.Green;
            this.LblSaved.Location = new System.Drawing.Point(355, 275);
            this.LblSaved.Name = "LblSaved";
            this.LblSaved.Size = new System.Drawing.Size(46, 17);
            this.LblSaved.TabIndex = 32;
            this.LblSaved.Text = "Saved...";
            this.LblSaved.UseCompatibleTextRendering = true;
            this.LblSaved.Visible = false;
            // 
            // trayIcon
            // 
            this.trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.trayIcon.BalloonTipTitle = "QMod Manager Reloaded";
            this.trayIcon.ContextMenuStrip = this.trayIconCtxMenu;
            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));
            this.trayIcon.Text = "QMod Manager Reloaded";
            this.trayIcon.Visible = true;
            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TrayIcon_MouseDoubleClick);
            // 
            // trayIconCtxMenu
            // 
            this.trayIconCtxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreWindowToolStripMenuItem,
            this.toolStripMenuItem2,
            this.launchGameToolStripMenuItem,
            this.openmModDirectoryToolStripMenuItem,
            this.openGameDirectoryToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem2});
            this.trayIconCtxMenu.Name = "trayIconCtxMenu";
            this.trayIconCtxMenu.Size = new System.Drawing.Size(189, 126);
            // 
            // restoreWindowToolStripMenuItem
            // 
            this.restoreWindowToolStripMenuItem.Name = "restoreWindowToolStripMenuItem";
            this.restoreWindowToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.restoreWindowToolStripMenuItem.Text = "&Restore Window";
            this.restoreWindowToolStripMenuItem.Click += new System.EventHandler(this.RestoreWindowToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(185, 6);
            // 
            // launchGameToolStripMenuItem
            // 
            this.launchGameToolStripMenuItem.Name = "launchGameToolStripMenuItem";
            this.launchGameToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.launchGameToolStripMenuItem.Text = "&Launch Game";
            this.launchGameToolStripMenuItem.Click += new System.EventHandler(this.LaunchGameToolStripMenuItem_Click);
            // 
            // openmModDirectoryToolStripMenuItem
            // 
            this.openmModDirectoryToolStripMenuItem.Name = "openmModDirectoryToolStripMenuItem";
            this.openmModDirectoryToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.openmModDirectoryToolStripMenuItem.Text = "Open &Mod Directory";
            this.openmModDirectoryToolStripMenuItem.Click += new System.EventHandler(this.OpenmModDirectoryToolStripMenuItem_Click);
            // 
            // openGameDirectoryToolStripMenuItem
            // 
            this.openGameDirectoryToolStripMenuItem.Name = "openGameDirectoryToolStripMenuItem";
            this.openGameDirectoryToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.openGameDirectoryToolStripMenuItem.Text = "Open &Game Directory";
            this.openGameDirectoryToolStripMenuItem.Click += new System.EventHandler(this.OpenGameDirectoryToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(185, 6);
            // 
            // exitToolStripMenuItem2
            // 
            this.exitToolStripMenuItem2.Name = "exitToolStripMenuItem2";
            this.exitToolStripMenuItem2.Size = new System.Drawing.Size(188, 22);
            this.exitToolStripMenuItem2.Text = "E&xit";
            this.exitToolStripMenuItem2.Click += new System.EventHandler(this.ExitToolStripMenuItem2_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 631);
            this.Controls.Add(this.DgvMods);
            this.Controls.Add(this.LblSaved);
            this.Controls.Add(this.TxtConfig);
            this.Controls.Add(this.LblConfig);
            this.Controls.Add(this.BtnOpenLog);
            this.Controls.Add(this.BtnOpenModDir);
            this.Controls.Add(this.BtnOpenGameDir);
            this.Controls.Add(this.ToolStrip);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.TxtLog);
            this.Controls.Add(this.LblLog);
            this.Controls.Add(this.BtnRemoveIntros);
            this.Controls.Add(this.BtnRefresh);
            this.Controls.Add(this.BtnRemovePatch);
            this.Controls.Add(this.BtnRunGame);
            this.Controls.Add(this.BtnRemove);
            this.Controls.Add(this.BtnAddMod);
            this.Controls.Add(this.TxtModInfo);
            this.Controls.Add(this.LblModInfo);
            this.Controls.Add(this.BtnPatch);
            this.Controls.Add(this.LblInstalledMods);
            this.Controls.Add(this.LblModFolderLocation);
            this.Controls.Add(this.LblGameLocation);
            this.Controls.Add(this.BtnBrowse);
            this.Controls.Add(this.TxtModFolderLocation);
            this.Controls.Add(this.TxtGameLocation);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FrmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QMod Manager Reloaded";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.Resize += new System.EventHandler(this.FrmMain_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.BtnRunGame)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DgvMods)).EndInit();
            this.ToolStrip.ResumeLayout(false);
            this.ToolStrip.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.trayIconCtxMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextBox TxtGameLocation;
        private TextBox TxtModFolderLocation;
        private Button BtnBrowse;
        private Label LblGameLocation;
        private Label LblModFolderLocation;
        private Label LblInstalledMods;
        private Button BtnPatch;
        private Label LblModInfo;
        private TextBox TxtModInfo;
        private FolderBrowserDialog DlgBrowse;
        private Button BtnAddMod;
        private Button BtnRemove;
        private PictureBox BtnRunGame;
        private ToolTip ToolTip;
        private Button BtnRemovePatch;
        private Button BtnRefresh;
        private Button BtnRemoveIntros;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem checklistToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private Label LblLog;
        private TextBox TxtLog;
        private ToolStrip ToolStrip;
        private ToolStripLabel LblPatched;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripLabel LblIntroPatched;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem1;
        private ToolStripMenuItem checklistToolStripMenuItem1;
        private ToolStripMenuItem aboutToolStripMenuItem1;
        private OpenFileDialog DlgFile;
        private Button BtnOpenGameDir;
        private Button BtnOpenModDir;
        private Button BtnOpenLog;
        private ToolStripMenuItem modifyResolutionToolStripMenuItem;
        private Label LblConfig;
        private TextBox TxtConfig;
        private Label LblSaved;
        private DataGridView DgvMods;
        private DataGridViewTextBoxColumn ChOrder;
        private DataGridViewTextBoxColumn ChMod;
        private DataGridViewCheckBoxColumn ChEnabled;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayIconCtxMenu;
        private ToolStripMenuItem restoreWindowToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem launchGameToolStripMenuItem;
        private ToolStripMenuItem openmModDirectoryToolStripMenuItem;
        private ToolStripMenuItem openGameDirectoryToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem2;
    }
}

