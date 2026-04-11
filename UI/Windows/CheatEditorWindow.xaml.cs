using POPSManager.Logic.Cheats;
using POPSManager.Settings;
using POPSManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace POPSManager.UI.Windows
{
    public partial class CheatEditorWindow : Window
    {
        private readonly string _gameFolder;
        private readonly string _cd1Folder;
        private readonly string _cheatPath;

        private readonly CheatManagerService _manager;
        private readonly CheatSettingsService _settings;
        private readonly PathsService _paths;

        private List<CheatDefinition> _officialCheats = new();
        private List<CheatDefinition> _userCheats = new();
        private List<string> _existingCheats = new();

        public CheatEditorWindow(
            string gameFolder,
            CheatManagerService manager,
            CheatSettingsService settings,
            PathsService paths)
        {
            InitializeComponent();

            _gameFolder = gameFolder;
            _manager = manager;
            _settings = settings;
            _paths = paths;

            _cd1Folder = Path.Combine(gameFolder, "CD1");
            _cheatPath = Path.Combine(_cd1Folder, "CHEAT.TXT");

            TitleText.Text = $"Editar Cheats — {Path.GetFileName(gameFolder)}";

            LoadCheats();
        }

        // ============================================================
        //  CARGAR CHEATS
        // ============================================================
        private void LoadCheats()
        {
            _officialCheats = CheatLibrary.GetOfficialCheats().ToList();

            // FIX: RootFolder viene de PathsService, NO de CheatSettings
            _userCheats = _manager.LoadUserCheats(_paths.RootFolder).ToList();

            _existingCheats = _manager.LoadCheatFile(_cheatPath);

            CheatsList.Items.Clear();

            foreach (var cheat in _officialCheats)
                CheatsList.Items.Add(cheat);

            foreach (var cheat in _userCheats)
                CheatsList.Items.Add(cheat);
        }

        // ============================================================
        //  SELECCIÓN DE CHEAT
        // ============================================================
        private void CheatsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CheatsList.SelectedItem is not CheatDefinition cheat)
                return;

            CheatName.Text = cheat.Name;
            CheatCategory.Text = $"Categoría: {cheat.Category}";
            CheatDescription.Text = cheat.Description;

            CheatEnabledCheck.IsChecked = _existingCheats.Contains(cheat.Code);
        }

        // ============================================================
        //  GUARDAR
        // ============================================================
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var selected = new List<string>();

            foreach (var item in CheatsList.Items)
            {
                if (item is CheatDefinition cheat)
                {
                    if (_existingCheats.Contains(cheat.Code) ||
                        (CheatsList.SelectedItem == cheat && CheatEnabledCheck.IsChecked == true))
                    {
                        selected.Add(cheat.Code);
                    }
                }
            }

            _manager.SaveCheatFile(_cheatPath, selected);

            MessageBox.Show("CHEAT.TXT actualizado correctamente.",
                            "POPSManager",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            Close();
        }

        // ============================================================
        //  CANCELAR
        // ============================================================
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ============================================================
        //  AÑADIR CHEAT PERSONALIZADO
        // ============================================================
        private void AddCustomCheat_Click(object sender, RoutedEventArgs e)
        {
            string code = Microsoft.VisualBasic.Interaction.InputBox(
                "Introduce el código del cheat (ej: VMODE=NTSC):",
                "Nuevo cheat personalizado");

            if (string.IsNullOrWhiteSpace(code))
                return;

            var cheat = new CheatDefinition
            {
                Code = code.Trim(),
                Name = code.Trim(),
                Description = "Cheat personalizado",
                Category = "Personalizado",
                IsUserDefined = true
            };

            _userCheats.Add(cheat);

            // FIX: Guardar cheats personalizados en RootFolder correcto
            _manager.SaveUserCheats(_paths.RootFolder, _userCheats);

            LoadCheats();
        }

        // ============================================================
        //  ELIMINAR CHEAT PERSONALIZADO
        // ============================================================
        private void DeleteCustomCheat_Click(object sender, RoutedEventArgs e)
        {
            if (CheatsList.SelectedItem is not CheatDefinition cheat)
                return;

            if (!cheat.IsUserDefined)
            {
                MessageBox.Show("Solo puedes eliminar cheats personalizados.",
                                "POPSManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            _userCheats.Remove(cheat);

            // FIX: Guardar cambios en la ruta correcta
            _manager.SaveUserCheats(_paths.RootFolder, _userCheats);

            LoadCheats();
        }
    }
}
