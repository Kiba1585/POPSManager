private async void Process_Click(object sender, RoutedEventArgs e)
{
    string folder = VcdPath.Text;

    if (!Directory.Exists(folder))
    {
        Services.Notifications.Error("Debes seleccionar una carpeta válida.");
        return;
    }

    if (GamesList.Items.Count == 0)
    {
        Services.Notifications.Warning("No hay archivos VCD o ISO para procesar.");
        return;
    }

    bool useAdvancedWindow = GamesList.Items.Count >= 2;

    ProgressWindow? win = null;

    if (useAdvancedWindow)
    {
        win = new ProgressWindow
        {
            Owner = Window.GetWindow(this)
        };
        win.Show();
    }
    else
    {
        Services.Progress.Reset();
        Services.Progress.Start("Procesando juego…");
    }

    try
    {
        await Task.Run(async () =>
        {
            await Services.GameProcessor.ProcessFolderAsync(
                folder,
                win?.ViewModel
            );
        });

        Services.Notifications.Success("Procesamiento completado.");
    }
    catch (Exception ex)
    {
        Services.Notifications.Error($"Error durante el procesamiento: {ex.Message}");
    }
    finally
    {
        Services.Progress.Stop();
        win?.Close();
    }
}
