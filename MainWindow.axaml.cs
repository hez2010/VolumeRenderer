using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using System.Collections.Generic;

namespace VolumeRenderer;

public partial class MainWindow : Window
{
    private bool _pointerInHistogram;
    private double[] _histogramX = [];
    private double[] _histogramY = [];

    public MainWindow()
    {
        InitializeComponent();
        volrdn.RawLoaded += (_, rawLoader) =>
        {
            _histogramX = rawLoader.HistogramX.ToArray();
            _histogramY = rawLoader.HistogramY.ToArray();
            UpdateHistogram();
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_pointerInHistogram)
            volrdn.PressKey(e);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!_pointerInHistogram)
            volrdn.ChangePointerWheel((float)e.Delta.Y);
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _pointerInHistogram = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _pointerInHistogram = false;
    }

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateHistogram();
    }

    private void UpdateHistogram()
    {
        if (_histogramX is []) return;

        var xs = new List<double>();
        var ys = new List<double>();
        var exclusions = new HashSet<double>();

        if (Filter is TextBox { Text: var text })
        {
            foreach (var i in text?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            {
                if (double.TryParse(i, out var d))
                {
                    exclusions.Add(d);
                }
            }
        }

        for (var i = 0; i < _histogramX.Length; i++)
        {
            if (exclusions.Contains(_histogramX[i])) continue;
            xs.Add(_histogramX[i]);
            ys.Add(_histogramY[i]);
        }

        Histogram.Plot.Clear();
        Histogram.Plot.AddScatter(xs.ToArray(), ys.ToArray());
        Histogram.Refresh();
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
            volrdn.UpdateTransferFunction(path);
        }
    }

    public async void TfEdit_OnClicked(object? sender, RoutedEventArgs e)
    {
        if (volrdn.GetTransferFunction() is not string tfFile)
        {
            await new ContentDialog
            {
                Title = "Error",
                Content = "No transfer function has been set!",
                PrimaryButtonText = "OK"
            }.ShowAsync();
            return;
        }
        else
        {
            var tfEditor = new TfEditorWindow();
            await tfEditor.SetTransferFunction(tfFile);
            tfEditor.DataApplied += (_, _) => volrdn.UpdateTransferFunction();
            await tfEditor.ShowDialog(this);
        }
    }
}
