using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Mono.Cecil;
using Newtonsoft.Json;
using QModReloaded;

namespace QModReloadedGUI;

public partial class FrmMain : Form
{
    private (string location, bool found) _gameLocation;
    private string _modLocation = string.Empty;
    private readonly List<QMod> _modList = new();
    private Injector _injector;
    private FrmChecklist _frmChecklist;
    private FrmAbout _frmAbout;
    private FrmResModifier _frmResModifier;
    private string _currentlySelectedModConfigLocation;
    private Rectangle _dragBoxFromMouseDown;
    private int _rowIndexFromMouseDown;
    private int _rowIndexOfItemUnderMouseToDrop;

    public FrmMain()
    {
        InitializeComponent();
    }

    private static bool IsGameRunning()
    {
        var processes = Process.GetProcessesByName("Graveyard Keeper");
        if (processes.Length <= 0) return false;
        MessageBox.Show(@"Please close the game before running any patches.", @"Close game.",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);
        return true;
    }

    public void WriteLog(string message)
    {
        Utilities.WriteLog(message, _gameLocation.location);

        if (TxtLog.Text.Length > 0)
            TxtLog.AppendText(Environment.NewLine + message);
        else

            TxtLog.AppendText(message);
    }

    private void SetLocations(string directory)
    {
        if (directory == string.Empty)
        {
            _gameLocation = Utilities.GetGameDirectory();
            if (_gameLocation.found)
            {
                TxtGameLocation.Text = _gameLocation.location;
                _modLocation = $@"{_gameLocation.location}\QMods";
                TxtModFolderLocation.Text = _modLocation;
            }
            else
            {
                TxtGameLocation.Text = @"Cannot locate automatically.Please browse for game location.";
            }
        }
        else
        {
            TxtGameLocation.Text = directory;
            _modLocation = $@"{directory}\QMods";
            TxtModFolderLocation.Text = _modLocation;
            _gameLocation.found = true;
        }

        if (!_gameLocation.found) return;
        if (new DirectoryInfo(_modLocation).Exists) return;
        Directory.CreateDirectory(_modLocation);
        WriteLog("INFO: QMods directory created.");
    }

    private static bool CreateJson(string file)
    {
        var sFile = new FileInfo(file);
        var path = new FileInfo(file).DirectoryName;
        var fileNameWithoutExt = sFile.Name.Substring(0, sFile.Name.Length - 4);
        var fileNameWithExt = sFile.Name;
        var (namesp, type, method, found) = GetModEntryPoint(file);
        var newMod = new QMod
        {
            DisplayName = fileNameWithoutExt,
            Enable = true,
            ModAssemblyPath = path,
            AssemblyName = fileNameWithExt,
            Author = "?",
            Id = fileNameWithoutExt,
            EntryMethod = found ? $"{namesp}.{type}.{method}" : $"{fileNameWithoutExt}.MainPatcher.Patch",
            Config = new Dictionary<string, object>(),
            Priority = "Last or First",
            Requires = Array.Empty<string>(),
            Version = "?",
        };
        var newJson = JsonConvert.SerializeObject(newMod, Formatting.Indented);
        if (path == null) return false;
        File.WriteAllText(Path.Combine(path, "mod.json"), newJson);
        var files = new FileInfo(Path.Combine(path, "mod.json"));
        return files.Exists;
    }

    private void UpdateLoadOrders()
    {
        foreach (DataGridViewRow row in DgvMods.Rows)
        {
            foreach (var mod in _modList.Where(mod => mod.DisplayName == DgvMods.Rows[row.Index].Cells[1].Value.ToString()))
            {
                DgvMods.Rows[row.Index].Cells[0].Value = row.Index + 1;
                mod.LoadOrder = row.Index + 1;
                var json = JsonConvert.SerializeObject(mod, Formatting.Indented);
                File.WriteAllText(Path.Combine(mod.ModAssemblyPath,"mod.json"),json);
            }
        }
    }


    private void LoadMods()
    {
        try
        {
            _modList.Clear();
            DgvMods.Rows.Clear();
            if (!_gameLocation.found) return;
            var dllFiles = Directory.GetFiles(_modLocation, "*.dll", SearchOption.AllDirectories);
            foreach (var dllFile in dllFiles)
            {
               // GetModEntryPoint(dllFile);
                var path = new FileInfo(dllFile).DirectoryName;
                if (path == null) continue;
                var dllFileName = new FileInfo(dllFile).Name;
                var modJsonFile = Directory.GetFiles(path, "mod.json", SearchOption.TopDirectoryOnly);
                var infoJsonFile = Directory.GetFiles(path, "info.json", SearchOption.TopDirectoryOnly);
                string jsonFile;
                if (modJsonFile.Length == 1 && infoJsonFile.Length == 1)
                {
                    WriteLog(
                        $"Multiple JSON detected for {dllFileName}. Please remove one. Either mod.json or info.json, not both.");
                    continue;
                }

                if (modJsonFile.Length == 1)
                {
                    jsonFile = modJsonFile[0];
                }
                else if (infoJsonFile.Length == 1)
                {
                    File.Copy(infoJsonFile[0], Path.Combine(new FileInfo(infoJsonFile[0]).DirectoryName!, "mod.json"),
                        true);
                    File.Delete(infoJsonFile[0]);
                    jsonFile = "mod.json";
                }
                else
                {
                    var result = MessageBox.Show($@"No JSON found for {dllFileName}. Would you like to create one?",
                        @"Create JSON", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        var createResult = CreateJson(dllFile);
                        if (createResult == false)
                        {
                            WriteLog("Error creating JSON file.");
                            continue;
                        }

                        TxtLog.Text = "";
                        LoadMods();
                        return;
                    }

                    continue;
                }

                var mod = QMod.FromJsonFile(Path.Combine(path, jsonFile));
                if (mod == null)
                {
                    WriteLog($"{dllFileName} didn't have a valid json.");
                }
                else
                {
                    mod.ModAssemblyPath = path;
                    if (!string.IsNullOrEmpty(mod.EntryMethod))
                    {
                        _modList.Add(mod);
                        DgvMods.Rows.Add(mod.LoadOrder, mod.DisplayName, mod.Enable);
                       
                        WriteLog(mod.DisplayName + " added.");
                    }
                    else
                    {
                        WriteLog(mod.DisplayName + " had issues and wasn't loaded.");
                    }
                }
            }

            WriteLog(
                "All mods with an entry point added. This doesn't mean they'll load correctly or function if they do load.");
        }
        catch (Exception ex)
        {
            WriteLog($"LoadMods() ERROR: {ex.Message}");
        }
    }

    private void CheckPatched()
    {
        if (!_gameLocation.found) return;

        _injector = new Injector(_gameLocation.location);
        if (_injector.IsInjected())
        {
            LblPatched.Text = @"Game Mod Patched";
            LblPatched.ForeColor = Color.Green;
            BtnPatch.Enabled = false;
            BtnRemovePatch.Enabled = true;
        }
        else
        {
            LblPatched.Text = @"Game Not Mod Patched";
            LblPatched.ForeColor = Color.Red;
            BtnPatch.Enabled = true;
            BtnRemovePatch.Enabled = false;
        }

        if (_injector.IsNoIntroInjected())
        {
            LblIntroPatched.Text = @"Intros Removed";
            LblIntroPatched.ForeColor = Color.Green;
        }
        else
        {
            LblIntroPatched.Text = @"Intros Not Removed";
            LblIntroPatched.ForeColor = Color.Red;
        }
    }

    private void FrmMain_Load(object sender, EventArgs e)
    {
        SetLocations(string.Empty);
        LoadMods();
        CheckPatched();
        DgvMods.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DgvMods.Sort(DgvMods.Columns[0], ListSortDirection.Ascending);
    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
        var result = DlgBrowse.ShowDialog();
        if (result != DialogResult.OK) return;
        var di = new DirectoryInfo(DlgBrowse.SelectedPath);
        var fi = new FileInfo(di.FullName + "\\Graveyard Keeper.exe");
        if (fi.Exists)
            SetLocations(di.ToString());
        else
            MessageBox.Show(@"Please select the directory containing Graveyard Keeper.exe", @"Wrong directory.",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }

    private void BtnRunGame_Click(object sender, EventArgs e)
    {
        try
        {
            using var steam = new Process();
            steam.StartInfo.FileName = "steam://rungameid/599140";
            steam.Start();
        }
        catch (Exception)
        {
            try
            {
                using var gyk = new Process();
                gyk.StartInfo.FileName = Path.Combine(_gameLocation.location, "Graveyard Keeper.exe");
                gyk.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Error launching game: " + ex.Message, @"Error", MessageBoxButtons.OK);
            }
        }
    }

    private void BtnPatch_Click(object sender, EventArgs e)
    {
        if (IsGameRunning()) return;
        if (_injector.IsInjected())
        {
            MessageBox.Show(@"All patching already done!", @"Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var (_, message) = _injector.Inject();
        WriteLog(message);
        CheckPatched();
    }

    private void BtnRemovePatch_Click(object sender, EventArgs e)
    {
        if (IsGameRunning()) return;
        var (_, message) = _injector.Remove();
        WriteLog(message);
        CheckPatched();
    }

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        LoadMods();
    }


    private static (string namesp, string type, string method, bool found) GetModEntryPoint(string mod)
    {
        try
        {
            var modAssembly = AssemblyDefinition.ReadAssembly(mod);

            var toInspect = modAssembly.MainModule
                .GetTypes()
                .SelectMany(t => t.Methods
                    .Where(m => m.HasBody)
                    .Select(m => new { t, m }));

            toInspect = toInspect.Where(x => x.m.Name == "Patch");

            foreach (var method in toInspect)
                if (method.m.Body.Instructions.Where(instruction => instruction.Operand != null).Any(instruction => instruction.Operand.ToString().Contains("PatchAll")))
                {
                    return (method.t.Namespace, method.t.Name, method.m.Name, true);
                }
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"GetModEntryPoint(): Error, {ex.Message}");
        }
        return (null, null, null, false);
    }

    private void BtnRemoveIntros_Click(object sender, EventArgs e)
    {
        if (IsGameRunning()) return;
        if (_injector.IsNoIntroInjected() && !_injector.IsInjected())
        {
            var alreadyResult = MessageBox.Show(@"Intro patch already done! Apply mod patch now?", @"Done!",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (alreadyResult == DialogResult.Yes) BtnPatch_Click(sender, e);
            return;
        }

        if (_injector.IsNoIntroInjected())
        {
            MessageBox.Show(@"All patching already done!", @"Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var patchResult =
            MessageBox.Show(
                @"Note! This is permanent and will require a Steam validate to restore the intros. Continue?",
                @"Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (patchResult != DialogResult.Yes) return;
        var (injected, message) = _injector.InjectNoIntros();
        if (injected)
        {
            WriteLog(message);
            var dlgResult = MessageBox.Show(
                @"Intros have been disabled, would you like to apply the patch now?",
                @"Done!", MessageBoxButtons.YesNo);
            if (dlgResult == DialogResult.Yes) BtnPatch_Click(sender, e);
        }
        else
        {
            WriteLog(message);
            MessageBox.Show(@"There was an issue patching out intros. Validate Steam files and try again.", @"Hmmm",
                MessageBoxButtons.OK);
        }
    }

    private void ToggleModEnabled(bool enabled, int row)
    {
        try
        {

            QMod modFound = null;
            foreach (var mod in _modList.Where(mod => mod.DisplayName == DgvMods.Rows[row].Cells[1].Value.ToString()))
                modFound = mod;

            if (modFound == null) return;
            if (enabled)
            {
                modFound.Enable = true;
                WriteLog("Enabled " + modFound.DisplayName);
            }
            else
            {
                modFound.Enable = false;
                WriteLog("Disabled " + modFound.DisplayName);
            }

            var newJson = JsonConvert.SerializeObject(modFound, Formatting.Indented);
            File.WriteAllText(Path.Combine(modFound.ModAssemblyPath, "mod.json"), newJson);
        }
        catch (Exception)
        {
            WriteLog("Issues toggling mod functionality.");
        }
    }

    private void ExitToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void ChecklistToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        _frmChecklist ??= new FrmChecklist(_injector, _gameLocation.location, _modLocation);
        _frmChecklist.ShowDialog();
        _frmChecklist = null;
    }

    private void AboutToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        _frmAbout ??= new FrmAbout();
        _frmAbout.ShowDialog();
        _frmAbout = null;
    }

    private bool AddMod(string file)
    {
        try
        {
            var modZip = new FileInfo(file);
            if (!modZip.Exists) return false;
            var modArchive = ZipFile.OpenRead(modZip.FullName);
            foreach (var entry in modArchive.Entries)
            {
                if (entry.FullName.EndsWith("dll", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(file,
                        _modLocation + "\\" + entry.FullName.Substring(0, entry.FullName.Length - 4));
                    break;
                }

                ZipFile.ExtractToDirectory(file, _modLocation);
                break;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void BtnAddMod_Click(object sender, EventArgs e)
    {
        var dlgResult = DlgFile.ShowDialog(this);
        if (dlgResult == DialogResult.OK)
            foreach (var zip in DlgFile.FileNames)
            {
                var result = AddMod(zip);
                if (result) WriteLog($"Extracted {zip}.");
            }

        LoadMods();
    }

    private void BtnOpenModDir_Click(object sender, EventArgs e)
    {
        if (_gameLocation.found)
            Process.Start(_modLocation);
        else
            MessageBox.Show(@"Set game location first.", @"Game", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private void BtnOpenGameDir_Click(object sender, EventArgs e)
    {
        if (_gameLocation.found)
            Process.Start(_gameLocation.location);
        else
            MessageBox.Show(@"Set game location first.", @"Game", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private void BtnOpenLog_Click(object sender, EventArgs e)
    {
        var file = Path.Combine(_gameLocation.location, "qmod_reloaded_log.txt");
        if (File.Exists(file))
            Process.Start(file);
        else
            MessageBox.Show(@"No log available yet.", @"Log", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private void BtnRemove_Click(object sender, EventArgs e)
    {
        var result = MessageBox.Show(@"This will remove the selected mod(s). Continue?", @"Remove mods",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        foreach (DataGridViewRow rows in DgvMods.SelectedRows)
        {
            var modName = rows.Cells[1].Value.ToString();
            var mod = _modList.FirstOrDefault(mod => mod.DisplayName == modName);
            if (mod == null) return;
            Directory.Delete(mod.ModAssemblyPath, true);
        }
        LoadMods();
    }

    private void ModifyResolutionToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _frmResModifier ??= new FrmResModifier(_gameLocation.location);
        _frmResModifier.ShowDialog();
        _frmResModifier = null;
    }

    private void TxtConfig_KeyUp(object sender, KeyEventArgs e)
    {
        if (TxtConfig.Text.Length == 0)
        {
            MessageBox.Show(@"The config file is now blank. This mod may or may not function correctly, if at all.",
                @"Blank config.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        try
        {
            File.WriteAllText(_currentlySelectedModConfigLocation, TxtConfig.Text);
            LblSaved.Visible = true;
        }
        catch (Exception)
        {
            WriteLog($"Issue saving config: {_currentlySelectedModConfigLocation}");
            throw;
        }
    }

    private void TxtConfig_Leave(object sender, EventArgs e)
    {
        LblSaved.Visible = false;
    }

    private void DgvMods_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != 2) return;
        if (DgvMods.CurrentRow == null) return;
        if (e.RowIndex != DgvMods.CurrentRow.Index) return;
        if (DgvMods.CurrentRow.Cells[2].Value.Equals(0))
        {
            ToggleModEnabled(true, DgvMods.CurrentRow.Index);
            DgvMods.CurrentRow.Cells[2].Value = 1;
        }
        else
        {
            ToggleModEnabled(false, DgvMods.CurrentRow.Index);
            DgvMods.CurrentRow.Cells[2].Value = 0;
        }
        DgvMods_CellClick(sender, e);
    }

    private void DgvMods_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        
        if (DgvMods.SelectedRows.Count > 1)
        {
            TxtModInfo.Clear();
            TxtConfig.Clear();
            return;
        }
        LblSaved.Visible = false;
        try
        {
            QMod modFound = null;
            foreach (var mod in _modList.Where(mod => mod.DisplayName == DgvMods.CurrentRow.Cells[1].Value.ToString()))
                modFound = mod;

            if (modFound == null) return;
            TxtModInfo.Clear();
            TxtModInfo.Text += @"ID: " + modFound.Id + Environment.NewLine;
            TxtModInfo.Text += @"Name: " + modFound.DisplayName + Environment.NewLine;
            TxtModInfo.Text += @"Author: " + modFound.Author + Environment.NewLine;
            TxtModInfo.Text += @"Version: " + modFound.Version + Environment.NewLine;
            TxtModInfo.Text += @"Enabled: " + modFound.Enable + Environment.NewLine;
            TxtModInfo.Text += @"DLL Name: " + modFound.AssemblyName + Environment.NewLine;
            TxtModInfo.Text += @"Entry Method: " + modFound.EntryMethod + Environment.NewLine;
            TxtModInfo.Text += @"Mod Path: " + modFound.ModAssemblyPath + Environment.NewLine;
            string path = null;
            var files = Directory.GetFiles(modFound.ModAssemblyPath, "*", SearchOption.AllDirectories);
            string[] configs = { ".ini", ".json", ".txt" };
            foreach (var file in files)
            {
                if (file.EndsWith("mod.json")) continue;
                if (!file.Contains("config")) continue;
                if (configs.Contains(new FileInfo(file).Extension))
                {
                    path = file;
                }
            }

            if (path != null)
            {
                if (!File.Exists(path)) return;
                var config = File.ReadAllText(path);
                _currentlySelectedModConfigLocation = path;
                TxtConfig.Text = config;
            }
            else
            {
                WriteLog($"No config file found for {modFound.DisplayName} that is JSON/TXT/INI.");
            }
        }
        catch (NullReferenceException ex)
        {
            WriteLog($"List Mods ERROR: {ex.Message}");
        }
        catch (Exception ex)
        {
            WriteLog($"List Mods ERROR: {ex.Message}");
        }
    }

    private void DgvMods_MouseMove(object sender, MouseEventArgs e)
    {
        if ((e.Button & MouseButtons.Left) != MouseButtons.Left) return;
        if (_dragBoxFromMouseDown != Rectangle.Empty &&
            !_dragBoxFromMouseDown.Contains(e.X, e.Y))
        {
            DgvMods.DoDragDrop(
                DgvMods.Rows[_rowIndexFromMouseDown],
                DragDropEffects.Move);
        }
    }

    private void DgvMods_MouseDown(object sender, MouseEventArgs e)
    {
        _rowIndexFromMouseDown = DgvMods.HitTest(e.X, e.Y).RowIndex;
        if (_rowIndexFromMouseDown != -1)
        {
            var dragSize = SystemInformation.DragSize;
            _dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                    e.Y - (dragSize.Height / 2)),
                dragSize);
        }
        else
            _dragBoxFromMouseDown = Rectangle.Empty;
    }

    private void DgvMods_DragOver(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;
    }

    private void DgvMods_DragDrop(object sender, DragEventArgs e)
    {
        var clientPoint = DgvMods.PointToClient(new Point(e.X, e.Y));
        _rowIndexOfItemUnderMouseToDrop =
            DgvMods.HitTest(clientPoint.X, clientPoint.Y).RowIndex;
        if (e.Effect != DragDropEffects.Move) return;
        if (_rowIndexOfItemUnderMouseToDrop < 0)
        {
            return;
        }
        DgvMods.Rows.RemoveAt(_rowIndexFromMouseDown);
        if (e.Data.GetData(
            typeof(DataGridViewRow)) is DataGridViewRow rowToMove) DgvMods.Rows.Insert(_rowIndexOfItemUnderMouseToDrop, rowToMove);
        UpdateLoadOrders();
    }

    private void ExitToolStripMenuItem2_Click(object sender, EventArgs e)
    {
        ExitToolStripMenuItem1_Click(sender,e);
    }

    private void LaunchGameToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnRunGame_Click(sender,e);
    }

    private void OpenmModDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnOpenModDir_Click(sender,e);
    }

    private void OpenGameDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnOpenGameDir_Click( sender,e);
    }

    private void RestoreWindowToolStripMenuItem_Click(object sender, EventArgs e)
    {
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
    }

    private void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
    }

    private void FrmMain_Resize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            ShowInTaskbar = false;
        }
    }
}