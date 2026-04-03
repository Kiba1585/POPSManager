using Microsoft.Win32;
using POPSManager.Controls;
using POPSManager.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace POPSManager
{
    public partial class MainWindow : Window
    {
        private readonly Converter converter;
        private readonly GameProcessor processor;

        private readonly List<string> pendingFiles = new();

        public MainWindow()
        {
            InitializeComponent();

            converter = new Converter(
                updateProgress: UpdateProgress,
                updateSpinner: UpdateSpinner,
                log: AddLog,
                notify: ShowNotification
            );

            processor = new GameProcessor(
                updateProgress: UpdateProgress,
                updateSpinner: UpdateSpinner,
                log: AddLog,
                notify: ShowNotification
            );
        }

        // ============================================================
        //  NOTIFICACIONES VISUALES (TOASTS)
        // ============================================================

        private void ShowNotification(UiNotification notification)
        {
            Dispatcher.Invoke(() =>
            {
                string styleKey = notification.Type switch
                {
                    NotificationType.Success => "ToastSuccessStyle",
                    NotificationType.Info => "ToastInfoStyle",
                    NotificationType.Warning => "ToastWarningStyle",
                    NotificationType.Error => "ToastErrorStyle",
                    _ => "ToastInfoStyle"
                };

                Border toast = new Border
                {
                    Style = (Style)FindResource(styleKey),
                    RenderTransform = new TranslateTransform()
                };

                toast.Child = new TextBlock
                {
                    Text = notification.Message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                };

                NotificationArea.Children.Insert(0, toast);

                Storyboard show = (Storyboard)FindResource("ToastShowAnimation");
                show.Begin(toast);

                Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Storyboard hide = (Storyboard)FindResource("ToastHideAnimation");
                        hide.Completed += (s, e) => NotificationArea.Children.Remove(toast);
                        hide.Begin(toast);
                    });
                });
            });
        }

        // ============================================================
        //  LOGS
        // ============================================================

        private void AddLog(string message)
        {
            Console.WriteLine(message);
        }

        // ============================================================
        //  PROGRESO
        // ============================================================

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Progreso: {value}%";
            });
        }

        private void UpdateSpinner(string text)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = text;
            });
        }

        // ============================================================
        //  DRAG & DROP
        // ============================================================

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (IsValidFile(file))
                {
                    pendingFiles.Add(file);
                    UpdatePreview(file);
                }
            }

            ShowNotification(new UiNotification(NotificationType.Info,
                $"{pendingFiles.Count} archivo(s) listos para procesar."));
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private bool IsValidFile(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return ext is ".bin" or ".cue" or ".iso" or ".vcd";
        }

        // ============================================================
        //  SELECCIÓN DE ARCHIVOS
        // ============================================================

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Archivos PS1/PS2|*.bin;*.cue;*.iso;*.vcd",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    pendingFiles.Add(file);
                    UpdatePreview(file);
                }

                ShowNotification(new UiNotification(NotificationType.Info,
                    $"{pendingFiles.Count} archivo(s) listos para procesar."));
            }
        }

        // ============================================================
        //  VISTA PREVIA
        // ============================================================

        private void UpdatePreview(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            PreviewName.Text = name;

            string? gameId = GameIdDetector.DetectGameId(file);
            PreviewGameId.Text = gameId ?? "Desconocido";

            PreviewRegion.Text = gameId != null && gameId.StartsWith("SCES") ? "PAL" : "NTSC";

            PreviewType.Text = file.EndsWith(".iso", StringComparison.OrdinalIgnoreCase)
                ? "ISO"
                : file.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase)
                    ? "VCD"
                    : "BIN/CUE";
        }

        // ============================================================
        //  PROCESAR ARCHIVOS
        // ============================================================

        private async void ProcessFiles_Click(object sender, RoutedEventArgs e)
        {
            if (pendingFiles.Count == 0)
            {
                ShowNotification(new UiNotification(NotificationType.Warning,
                    "No hay archivos para procesar."));
                return;
            }

            ShowNotification(new UiNotification(NotificationType.Info,
                "Procesando archivos..."));

            foreach (var file in pendingFiles)
            {
                if (file.EndsWith(".vcd", StringComparison.OrdinalIgnoreCase))
                {
                    await processor.ProcessFolder(
                        Path.GetDirectoryName(file)!,
                        Path.Combine(AppContext.BaseDirectory, "APPS")
                    );
                }
                else
                {
                    await converter.ConvertFolder(
                        Path.GetDirectoryName(file)!,
                        Path.Combine(AppContext.BaseDirectory, "POPS")
                    );
                }
            }

            ShowNotification(new UiNotification(NotificationType.Success,
                "Proceso completado."));

            pendingFiles.Clear();
        }
    }
}
