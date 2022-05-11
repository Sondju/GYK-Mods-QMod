using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QModReloadedInstaller;

namespace QModReloadedGUI
{
    public partial class FrmChecklist : Form
    {
        private readonly Injector _injector;
        private readonly string _gameLocation;
        private readonly string _modLocation;
        public FrmChecklist(Injector injector, string gameLocation, string modLocation)
        {
            _injector = injector;
            _gameLocation = gameLocation;
            _modLocation = modLocation;
            InitializeComponent();
        }

        private static bool CheckFileExists(string sFile)
        {
            var file = new FileInfo(sFile);
            return file.Exists;
        }

        private void FrmChecklist_Load(object sender, EventArgs e)
        {
            ChkModPatched.Checked = _injector.IsInjected();
            ChkNoIntroPatched.Checked = _injector.IsNoIntroInjected();
            ChkGameLocation.Checked = _gameLocation != string.Empty;
            ChkModDirectoryExists.Checked = _modLocation != string.Empty;

            Chk0HarmonyExists.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "0Harmony.dll"));
            ChkNewtonExists.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "Newtonsoft.Json.dll"));
            ChkMonoCecilExists.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "Mono.Cecil.dll"));
            ChkGameLoopVDF.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "Gameloop.Vdf.dll"));
            ChkGameLoopVDFJson.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "Gameloop.Vdf.JsonConverter.dll"));
            ChkInjector.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "QModReloadedInstaller.dll"));
            ChkConfig.Checked =
                CheckFileExists(Path.Combine(Application.StartupPath, "QModReloadedGUI.exe.config"));

            if (Chk0HarmonyExists.Checked && ChkNewtonExists.Checked && ChkMonoCecilExists.Checked &&
                ChkGameLoopVDF.Checked && ChkGameLoopVDFJson.Checked && ChkInjector.Checked && ChkConfig.Checked)
            {
                if (Application.ExecutablePath.Contains("Graveyard Keeper_Data\\Managed"))
                {
                    ChkPatcherLocation.Checked = true;
                }
            }

            foreach (Control control in Controls)
            {
                if (control.GetType() != typeof(CheckBox)) continue;
                var c = (CheckBox)control;
                c.ForeColor = c.Checked ? Color.Green : Color.Red;
            }
        }
    }
}
