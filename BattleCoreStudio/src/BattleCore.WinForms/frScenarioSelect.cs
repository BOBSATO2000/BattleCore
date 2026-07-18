namespace BattleCoreStudio
{
    /// <summary>
    /// 起動時にscenariosフォルダのJSONを一覧表示してシナリオを選択するダイアログ。
    /// </summary>
    internal class frScenarioSelect : Form
    {
        private readonly ListBox  lstScenarios = new();
        private readonly Button   btnOk        = new();
        private readonly Button   btnCancel    = new();

        /// <summary>選択されたシナリオファイルのフルパス。キャンセル時はnull。</summary>
        public string? SelectedPath { get; private set; }

        public frScenarioSelect(string scenariosFolder)
        {
            Text            = "シナリオ選択";
            ClientSize      = new Size(360, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;

            lstScenarios.Location  = new Point(12, 12);
            lstScenarios.Size      = new Size(336, 180);
            lstScenarios.Font      = new Font("MS Gothic", 10f);
            lstScenarios.DoubleClick += (s, e) => Confirm();

            btnOk.Text     = "開始";
            btnOk.Size     = new Size(80, 30);
            btnOk.Location = new Point(176, 204);
            btnOk.Click   += (s, e) => Confirm();

            btnCancel.Text         = "キャンセル";
            btnCancel.Size         = new Size(80, 30);
            btnCancel.Location     = new Point(268, 204);
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lstScenarios, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // scenariosフォルダのJSONを列挙
            if (Directory.Exists(scenariosFolder))
            {
                foreach (var file in Directory.GetFiles(scenariosFolder, "*.json"))
                    lstScenarios.Items.Add(new ScenarioItem(file));
            }

            if (lstScenarios.Items.Count > 0)
                lstScenarios.SelectedIndex = 0;
        }

        private void Confirm()
        {
            if (lstScenarios.SelectedItem is ScenarioItem item)
            {
                SelectedPath = item.Path;
                DialogResult = DialogResult.OK;
            }
        }

        /// <summary>リストボックスに表示するアイテム。ファイル名を表示しフルパスを保持。</summary>
        private sealed class ScenarioItem
        {
            public string Path { get; }
            public ScenarioItem(string path) => Path = path;
            public override string ToString() => System.IO.Path.GetFileNameWithoutExtension(Path);
        }
    }
}
