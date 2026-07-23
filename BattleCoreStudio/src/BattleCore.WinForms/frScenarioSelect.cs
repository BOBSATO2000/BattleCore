using BattleCore.Scenario;
using BattleCore.World;

namespace BattleCoreStudio
{
    /// <summary>
    /// 起動時にscenariosフォルダのJSONを一覧表示してシナリオとプレイヤー勢力を選択するダイアログ。
    /// </summary>
    internal class frScenarioSelect : Form
    {
        private readonly ListBox  lstScenarios  = new();
        private readonly ComboBox cmbPlayerClan = new();
        private readonly Label    lblClan       = new();
        private readonly Button   btnOk         = new();
        private readonly Button   btnCancel      = new();

        /// <summary>選択されたシナリオファイルのフルパス。キャンセル時はnull。</summary>
        public string? SelectedPath { get; private set; }

        /// <summary>プレイヤーが操作する勢力ID。0=観戦モード（全AI）。</summary>
        public int PlayerClanId { get; private set; } = 0;

        public frScenarioSelect(string scenariosFolder)
        {
            Text            = "シナリオ選択";
            ClientSize      = new Size(360, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(20, 20, 40);
            ForeColor       = Color.White;

            lstScenarios.Location    = new Point(12, 12);
            lstScenarios.Size        = new Size(336, 180);
            lstScenarios.Font        = new Font("MS Gothic", 10f);
            lstScenarios.BackColor   = Color.FromArgb(30, 30, 50);
            lstScenarios.ForeColor   = Color.White;
            lstScenarios.DoubleClick += (s, e) => Confirm();
            lstScenarios.SelectedIndexChanged += LstScenarios_SelectedIndexChanged;

            lblClan.Text     = "プレイヤー勢力:";
            lblClan.Location = new Point(12, 204);
            lblClan.Size     = new Size(110, 20);
            lblClan.Font     = new Font("MS Gothic", 9f);

            cmbPlayerClan.Location      = new Point(126, 200);
            cmbPlayerClan.Size          = new Size(222, 24);
            cmbPlayerClan.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPlayerClan.Font          = new Font("MS Gothic", 9f);
            cmbPlayerClan.BackColor     = Color.FromArgb(30, 30, 50);
            cmbPlayerClan.ForeColor     = Color.White;

            btnOk.Text      = "開始";
            btnOk.Size      = new Size(80, 30);
            btnOk.Location  = new Point(176, 272);
            btnOk.BackColor = Color.FromArgb(40, 80, 40);
            btnOk.ForeColor = Color.White;
            btnOk.Click    += (s, e) => Confirm();

            btnCancel.Text         = "キャンセル";
            btnCancel.Size         = new Size(80, 30);
            btnCancel.Location     = new Point(268, 272);
            btnCancel.BackColor    = Color.FromArgb(60, 30, 30);
            btnCancel.ForeColor    = Color.White;
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lstScenarios, lblClan, cmbPlayerClan, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (Directory.Exists(scenariosFolder))
            {
                foreach (var file in Directory.GetFiles(scenariosFolder, "*.json"))
                    lstScenarios.Items.Add(new ScenarioItem(file));
            }

            if (lstScenarios.Items.Count > 0)
                lstScenarios.SelectedIndex = 0;
        }

        private void LstScenarios_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstScenarios.SelectedItem is not ScenarioItem item) return;
            cmbPlayerClan.Items.Clear();
            cmbPlayerClan.Items.Add(new ClanItem(0, "【観戦モード（全AI）】"));
            try
            {
                var (world, _, _) = ScenarioLoader.Load(item.Path);
                foreach (var clan in world.Clans)
                    cmbPlayerClan.Items.Add(new ClanItem(clan.Id, clan.Name));
            }
            catch { /* 読み込み失敗時は観戦モードのみ */ }
            cmbPlayerClan.SelectedIndex = 0;
        }

        private void Confirm()
        {
            if (lstScenarios.SelectedItem is ScenarioItem item)
            {
                SelectedPath = item.Path;
                PlayerClanId = cmbPlayerClan.SelectedItem is ClanItem ci ? ci.Id : 0;
                DialogResult = DialogResult.OK;
            }
        }

        private sealed class ScenarioItem
        {
            public string Path { get; }
            public ScenarioItem(string path) => Path = path;
            public override string ToString() => System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        private sealed class ClanItem
        {
            public int    Id   { get; }
            public string Name { get; }
            public ClanItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}
