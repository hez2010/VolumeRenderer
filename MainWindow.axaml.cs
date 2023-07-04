using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace VolumeRenderer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        volrdn.PressKey(e);
    }

    public void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        volrdn.ChangePointerWheel((float)e.Delta.Y);
    }

    public async void TfChange_OnClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;
        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Choose transfer function"
        });

        if (result is [IStorageFile file] && file.TryGetLocalPath() is string path)
        {
            volrdn.ChangeTransferFunction(path);
        }
    }

    public void TfEdit_OnClicked(object? sender, RoutedEventArgs e)
    {
        new TfEditorWindow().ShowDialog(this);
    }
}
