using POPSManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace POPSManager.UI.Windows
{
    public partial class GameSelectorWindow : Window
    {
        private readonly PathsService _paths;

        public string? SelectedGameFolder { get; private set; }

        public GameSelectorWindow(PathsService paths)
        {
            InitializeComponent();
            _paths = paths;

            LoadGames();
        }

        // ============================================================
        //  CARGAR JUEGOS PROCESADOS
        // ============================================================
        private void LoadGames()
        {
            try
            {
                if (!Directory.Exists(_paths.PopsFolder))
                    return;

                var folders = Directory.GetDirectories(_paths.PopsFolder)
                                       .Where(f => Directory.GetDirectories(f)
                                                            .Any(d => Path.GetFileName(d).StartsWith("CD1",
                                                                StringComparison.OrdinalIgnoreCase)))
                                       .OrderBy(f => f)
                                       .ToList();

                foreach (var folder in folders)
                {
                    GamesList.Items.Add(Path.GetFileName(folder));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando juegos: {ex.Message}",
                                "POPSManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ============================================================
        //  BOTÓN CANCELAR
        // ============================================================
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedGameFolder = null;
            Close();
        }

        // ============================================================
        //  BOTÓN EDITAR
        // ============================================================
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (GamesList.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un juego primero.",
                                "POPSManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            string folderName = GamesList.SelectedItem.ToString()!;
            SelectedGameFolder = Path.Combine(_paths.PopsFolder, folderName);

            DialogResult = true;
            Close();
        }
    }
}
