using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VolumeRenderer;

public partial class TfEditorWindow : Window
{
    private readonly TfEditorViewModel _viewModel = new();
    private string? _fileName;

    public TfEditorWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public async Task SetTransferFunction(string fileName)
    {
        await using var fileStream = File.Open(fileName, FileMode.Open);
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
        _fileName = fileName;
    }

    public async void Apply_OnClicked(object? sender, RoutedEventArgs e)
    {
        if (_fileName is null) return;

        await using (var fileStream = File.OpenWrite(_fileName))
        {
            for (var i = 0; i < 256; i++)
            {
                fileStream.WriteByte(_viewModel.Data[i].R);
                fileStream.WriteByte(_viewModel.Data[i].G);
                fileStream.WriteByte(_viewModel.Data[i].B);
                fileStream.WriteByte(_viewModel.Data[i].A);
            }
        }

        DataApplied?.Invoke(this, new());
    }

    public async void Csv_OnClicked(object? sender, RoutedEventArgs e)
    {
        if (_fileName is null) return;
        var sb = new StringBuilder();
        sb.AppendLine("Intensity, R, G, B, A");
        for (var i = 0; i < 256; i++)
        {
            sb.AppendLine($"{i}, {_viewModel.Data[i].R}, {_viewModel.Data[i].G}, {_viewModel.Data[i].B}, {_viewModel.Data[i].A}");
        }
        var file = Path.ChangeExtension(Path.GetTempFileName(), ".csv");
        await File.WriteAllTextAsync(file, sb.ToString());
        if (Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true }) is Process process)
        {
            await process.WaitForExitAsync();
            process.Dispose();
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Length is 0) return;
            var title = lines[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.Replace("\"", "")).ToArray();
            if (title.Length != 5) return;
            foreach (var line in lines.Skip(1))
            {
                var index = 0;
                byte intensity = 0, r = 0, g = 0, b = 0, a = 0;
                foreach (var i in line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.Replace("\"", "")))
                {
                    if (index >= 5) return;

                    if (byte.TryParse(i, out var data))
                    {
                        switch (title[index].ToLowerInvariant())
                        {
                            case "intensity":
                                intensity = data; break;
                            case "r":
                                r = data; break;
                            case "g":
                                g = data; break;
                            case "b":
                                b = data; break;
                            case "a":
                                a = data; break;
                            default:
                                return;
                        }
                    }

                    index++;
                }
                _viewModel.Data[intensity].R = r;
                _viewModel.Data[intensity].G = g;
                _viewModel.Data[intensity].B = b;
                _viewModel.Data[intensity].A = a;
            }
        }
    }



    public event EventHandler<EventArgs>? DataApplied;
}
