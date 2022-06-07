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
using QModReloaded;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private const string CleanMd5 = "b75466bdcc44f5f098d4b22dc047b175"; //hash for Assembly-CSharp.dll 1.405
    private static readonly JsonSerializerOptions JsonOptions = new()
    { WriteIndented = true, IncludeFields = true, UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
    };

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
                Properties.Settings.Default.GamePath = _gameLocation.location;
                Properties.Settings.Default.Save();
            }
            else
            {
                TxtGameLocation.Text = @"Cannot locate automatically.Please browse for game location.";
            }
        }
        else
        {
            _gameLocation.location = directory;
            TxtGameLocation.Text = directory;
            _modLocation = $@"{directory}\QMods";
            TxtModFolderLocation.Text = _modLocation;
            _gameLocation.found = true;
            Properties.Settings.Default.GamePath = directory;
            Properties.Settings.Default.Save();
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
            Version = "?",
        };
        var newJson = JsonSerializer.Serialize(newMod, JsonOptions);
        if (path == null) return false;
        File.WriteAllText(Path.Combine(path, "mod.json"), newJson);
        var files = new FileInfo(Path.Combine(path, "mod.json"));
        return files.Exists;
    }

    private void UpdateLoadOrders()
    {
        foreach (DataGridViewRow row in DgvMods.Rows)
        {
            foreach (var mod in _modList.Where(mod =>
                         mod.DisplayName == DgvMods.Rows[row.Index].Cells[1].Value.ToString()))
            {
                DgvMods.Rows[row.Index].Cells[0].Value = row.Index + 1;
                mod.LoadOrder = row.Index + 1;
                var json = JsonSerializer.Serialize(mod, JsonOptions);
                File.WriteAllText(Path.Combine(mod.ModAssemblyPath, "mod.json"), json);
            }
        }

        CheckQueueEverything();
    }


    private void LoadMods()
    {
        try
        {
            _modList.Clear();
            DgvMods.Rows.Clear();
            if (!_gameLocation.found) return;

            var dllFiles =
                Directory.EnumerateDirectories(_modLocation).SelectMany(
                    directory => Directory.EnumerateFiles(directory, "*.dll"));

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

            DgvMods.Sort(DgvMods.Columns[0], ListSortDirection.Ascending);
            WriteLog(
                "All mods with an entry point added. This doesn't mean they'll load correctly or function if they do load.");
        }
        catch (Exception ex)
        {
            WriteLog($"LoadMods() ERROR: {ex.Message}");
        }

        CheckQueueEverything();
        CheckAllModsActive();
        CheckPatched();
    }

    private void CheckQueueEverything()
    {
        var foundQueueEverything = _modList.Find(x => x.Id == "QueueEverything");
        var foundExhaustless = _modList.Find(x => x.Id == "Exhaust-less");
        var foundFasterCraft = _modList.Find(x => x.Id == "FasterCraft");
        var showOrderMessage = false;
        if (foundQueueEverything != null)
        {
            if (foundExhaustless != null)
            {
                if (foundQueueEverything.LoadOrder < foundExhaustless.LoadOrder)
                {
                    showOrderMessage = true;
                }
            }

            if (foundFasterCraft != null)
            {
                if (foundQueueEverything.LoadOrder < foundFasterCraft.LoadOrder)
                {
                    showOrderMessage = true;
                }
            }
        }

        if (showOrderMessage)
        {
            MessageBox.Show(
                @"It seems you have Queue Everything!* and Exhaust-less/FasterCraft set to an invalid load order. Please ensure that Queue Everything is further down the load order than both of those mods, or it won't detect them.",
                @"Load Order Issue", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    public bool ModInList(string mod)
    {
        return _modList.Any(x => x.DisplayName.ToLower().Contains(mod.ToLower()));
    }

    private void CheckPatched()
    {

        if (!_gameLocation.found) return;

        _injector = new Injector(_gameLocation.location);
        if (_injector.IsInjected())
        {
            LblPatched.Text = @"Mod Injector Installed";
            LblPatched.ForeColor = Color.Green;
            BtnPatch.Enabled = false;
            BtnRemovePatch.Enabled = true;
        }
        else
        {
            LblPatched.Text = @"Mod Injector Not Installed";
            LblPatched.ForeColor = Color.Red;
            BtnPatch.Enabled = true;
            BtnRemovePatch.Enabled = false;
        }

        if (_injector.IsNoIntroInjected())
        {
            if (ModInList("intros"))
            {
                LblIntroPatched.Text = @"Intros Removed (via mod and patch?).";
                LblIntroPatched.ForeColor = Color.DarkOrange;
            }
            else
            {
                LblIntroPatched.Text = @"Intros Removed (via patch).";
                LblIntroPatched.ForeColor = Color.Green;
            }
        }
        else
        {
            if (ModInList("intros"))
            {
                LblIntroPatched.Text = @"Intros Removed (via mod).";
                LblIntroPatched.ForeColor = Color.Green;
            }
            else
            {
                LblIntroPatched.Text = @"Intros Not Removed";
                LblIntroPatched.ForeColor = Color.Red;
            }
        }

        try
        {
            if (!Utilities.CalculateMd5(Path.Combine(_gameLocation.location,
                    "Graveyard Keeper_Data\\Managed\\Assembly-CSharp.dll")).Equals(CleanMd5)) return;

            File.Copy(Path.Combine(_gameLocation.location, "Graveyard Keeper_Data\\Managed\\Assembly-CSharp.dll"),
                Path.Combine(_gameLocation.location, "Graveyard Keeper_Data\\Managed\\dep\\Assembly-CSharp.dll"),
                true);
            WriteLog("Clean Assembly-CSharp.dll detected. Backing up to Graveyard Keeper_Data\\Managed\\dep");
        }
        catch (FileNotFoundException)
        {
            WriteLog("Assembly-CSharp.dll not found. Probably need to check that out.");
        }
        catch (Exception)
        {
            //
        }
    }

    private void FrmMain_Load(object sender, EventArgs e)
    {
        
        SetLocations(Properties.Settings.Default.GamePath);
        LoadMods();
        DgvMods.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        DgvMods.Sort(DgvMods.Columns[0], ListSortDirection.Ascending);
        DgvMods.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        DgvMods.AllowUserToResizeRows = false;

        if (!Properties.Settings.Default.AlertShown)
        {
            MessageBox.Show(
                @"PLEASE READ: I have upgraded an integral DLL (Harmony 1 to Harmony 2) to the latest version available as the current one is quite old and the new one has " +
                @"a greater toolkit - this means mods will need to be updated as well. All my mods have been updated, (its a single line of code) and I have updated other mods I use." +
                @" These updated mods will be available on my GitHub until the original author updates. Please re-verify game files, and re-run the patch process. You will not be shown this again.", @"STOP", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            Properties.Settings.Default.AlertShown = true;
            Properties.Settings.Default.Save();
        }

    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
        var result = DlgBrowse.ShowDialog();
        if (result != DialogResult.OK) return;
        var di = new DirectoryInfo(DlgBrowse.SelectedPath);
        var fi = new FileInfo(di.FullName + "\\Graveyard Keeper.exe");
        if (fi.Exists)
        {
            Console.WriteLine(di.ToString());
            SetLocations(di.ToString());
            LoadMods();
        }
        else
        {
            MessageBox.Show(@"Please select the directory containing Graveyard Keeper.exe", @"Wrong directory.",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    private void RunGame()
    {
        try
        {
            if (!Properties.Settings.Default.LaunchDirectly)
            {
                using var steam = new Process();
                steam.StartInfo.FileName = "steam://rungameid/599140";
                steam.Start();
            }
            else
            {
                using var gyk = new Process();
                gyk.StartInfo.FileName = Path.Combine(_gameLocation.location, "Graveyard Keeper.exe");
                gyk.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(@"Error launching game: " + ex.Message, @"Error", MessageBoxButtons.OK);
            
        }
        finally
        {
            WindowState = FormWindowState.Minimized;
        }
    }

    private void BtnRunGame_Click(object sender, EventArgs e)
    {
        RunGame();
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
                    .Select(m => new {t, m}));

            toInspect = toInspect.Where(x => x.m.Name == "Patch");

            foreach (var method in toInspect)
                if (method.m.Body.Instructions.Where(instruction => instruction.Operand != null)
                    .Any(instruction => instruction.Operand.ToString().Contains("PatchAll")))
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
            modFound.Enable = enabled;

            var newJson = JsonSerializer.Serialize(modFound, JsonOptions);
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
            _modList.Remove(mod);
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

        DgvModsClick();
    }

    private void CheckAllModsActive()
    {
        ChkToggleMods.Checked = true;
        foreach (var unused in _modList.Where(mod => mod.Enable == false))
        {
            ChkToggleMods.Checked = false;
            break;
        }
    }

    private void DgvModsClick()
    {
        if (DgvMods.SelectedRows.Count > 1)
        {
            TxtModInfo.Clear();
            TxtConfig.Clear();
            return;
        }

        CheckAllModsActive();

        LblSaved.Visible = false;
        try
        {
            QMod modFound = null;
            foreach (var mod in _modList.Where(mod => mod.DisplayName == DgvMods.CurrentRow?.Cells[1].Value.ToString()))
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
            string[] configs = { ".ini", ".json", ".txt", ".cfg" };
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
                TxtConfig.Clear();
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

    private void DgvMods_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        DgvModsClick();
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
                typeof(DataGridViewRow)) is DataGridViewRow rowToMove)
            DgvMods.Rows.Insert(_rowIndexOfItemUnderMouseToDrop, rowToMove);
        UpdateLoadOrders();
    }

    private void ExitToolStripMenuItem2_Click(object sender, EventArgs e)
    {
        ExitToolStripMenuItem1_Click(sender, e);
    }

    private void LaunchGameToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnRunGame_Click(sender, e);
    }

    private void OpenmModDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnOpenModDir_Click(sender, e);
    }

    private void OpenGameDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        BtnOpenGameDir_Click(sender, e);
    }

    private void RestoreWindowToolStripMenuItem_Click(object sender, EventArgs e)
    {
        WindowState = FormWindowState.Normal;
        Focus();
        ShowInTaskbar = true;
    }

    private void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        if(WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        else
        {
            TopMost = true;
            Focus();
            BringToFront();
            TopMost = false;
        }
        Focus();
        ShowInTaskbar = true;
    }

    private void FrmMain_Resize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            ShowInTaskbar = false;
        }
    }

    private void BtnRestore_Click(object sender, EventArgs e)
    {
        var result =
            MessageBox.Show(
                @"This will restore any backed up Assembly-CSharp.dll. You will need to re-patch to use mods. Continue?",
                @"Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        try
        {
            File.Copy(Path.Combine(_gameLocation.location, "Graveyard Keeper_Data\\Managed\\dep\\Assembly-CSharp.dll"),
                Path.Combine(_gameLocation.location, "Graveyard Keeper_Data\\Managed\\Assembly-CSharp.dll"), true);
            FrmMain_Load(sender, e);
            WriteLog("Restored Assembly-CSharp.dll from the Graveyard Keeper_Data\\Managed\\dep directory.");
        }
        catch (FileNotFoundException)
        {
            WriteLog("A backed up Assembly-CSharp.dll could not be found.");
        }
        catch (Exception ex)
        {
            WriteLog($"An error occurred: {ex.Message}.");
        }
    }

    private void DgvMods_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (DgvMods.SelectedRows.Count > 1)
        {
            return;
        }

        QMod modFound = null;
        try
        {

            foreach (var mod in _modList.Where(mod => mod.DisplayName == DgvMods.CurrentRow?.Cells[1].Value.ToString()))
                modFound = mod;
            if (!_gameLocation.found) return;
            if (modFound != null)
                Process.Start(modFound.ModAssemblyPath);
        }
        catch (Exception)
        {
            WriteLog($"Issue locating folder for {modFound?.DisplayName}.");
        }
    }
    
    private void ChkToggleMods_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in DgvMods.Rows)
        {
            if (ChkToggleMods.Checked)
            {
                row.Cells[2].Value = 1;
                ToggleModEnabled(true, row.Index);
            }
            else
            {
                row.Cells[2].Value = 0;
                ToggleModEnabled(false, row.Index);
            }
        }
    }

    private void BtnLaunchModless_Click(object sender, EventArgs e)
    {
        if (IsGameRunning()) return;
        foreach (var mod in _modList)
        {
            WriteLog("Disabling mods and launching game.");
            mod.Enable = false;
            var newJson = JsonSerializer.Serialize(mod, JsonOptions);
            File.WriteAllText(Path.Combine(mod.ModAssemblyPath, "mod.json"), newJson);
        }
        RunGame();
    }

    private void DgvMods_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Up or Keys.Down)
        {
            DgvModsClick();
        }
    }

    private void ChkLaunchExeDirectly_CheckStateChanged(object sender, EventArgs e)
    {
        Properties.Settings.Default.LaunchDirectly = ChkLaunchExeDirectly.Checked;
        Properties.Settings.Default.Save();
    }
}