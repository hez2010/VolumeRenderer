using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace VolumeRenderer;

public partial class TfEditorWindow : Window
{
    private readonly TfEditorViewModel _viewModel = new();
    public TfEditorWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }
    public async void Load_OnClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;
        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Choose transfer function"
        });

        if (result is [IStorageFile file])
        {
            _viewModel.Data.Clear();
            await using var fileStream = await file.OpenReadAsync();
            var buffer = new byte[4 * 256];
            await fileStream.ReadExactlyAsync(buffer);
            for (var i = 0; i < 256; i++)
            {
                _viewModel.Data.Add(new TfData((byte)i)
                {
                    R = buffer[i * 4],
                    G = buffer[i * 4 + 1],
                    B = buffer[i * 4 + 2],
                    A = buffer[i * 4 + 3]
                });
            }
        }
    }

    public async void Save_OnClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;
        var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save transfer function"
        });

        if (result is IStorageFile file)
        {
            await using var fileStream = await file.OpenWriteAsync();
            for (var i = 0; i < 256; i++)
            {
                fileStream.WriteByte(_viewModel.Data[i].R);
                fileStream.WriteByte(_viewModel.Data[i].G);
                fileStream.WriteByte(_viewModel.Data[i].B);
                fileStream.WriteByte(_viewModel.Data[i].A);
            }
        }
    }
}
