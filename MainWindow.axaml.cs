using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using System.Collections.ObjectModel;

namespace VolumeRenderer;

public partial class MainWindow : Window
{
    private bool _pointerInHistogram;
    private TfEditorWindow? _tfEditor;

    record HistogramData(double[] X, double[] Y);

    public ObservableCollection<IRawLoader> Models { get; } = new();
    private IRawLoader? currentSelection;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        volrdn.RawLoaded += (_, rawLoader) =>
        {
            if (!Models.Contains(rawLoader))
            {
                Models.Add(rawLoader);
            }
            if (currentSelection is null)
            {
                currentSelection = rawLoader;
                comboBox.SelectedIndex = 0;
            }
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
        if (currentSelection?.HistogramX is null or { Count: 0 }) return;

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
        for(var i = 0; i < 1000; i++)
        {
            exclusions.Add(i);
        }

        for (var i = 0; i < currentSelection.HistogramX.Count; i++)
        {
            if (exclusions.Contains(currentSelection.HistogramX.ElementAt(i))) continue;
            xs.Add(currentSelection.HistogramX.ElementAt(i));
            ys.Add(currentSelection.HistogramY.ElementAt(i));
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

        if (currentSelection is not null && result is [IStorageFile file] && file.TryGetLocalPath() is string path)
        {
            volrdn.UpdateTransferFunction(currentSelection, path);
        }
    }

    public async void TfEdit_OnClicked(object? sender, RoutedEventArgs e)
    {
        if (_tfEditor is TfEditorWindow window)
        {
            window.Activate();
            return;
        }

        if (currentSelection is null)
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
            _tfEditor = new TfEditorWindow();
            await _tfEditor.SetTransferFunction(currentSelection.TransferFunction.FileName);
            _tfEditor.DataApplied += (_, _) => volrdn.UpdateTransferFunction(currentSelection);
            _tfEditor.Closing += (_, _) => _tfEditor = null;
            _tfEditor.Show();
        }
    }

    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        currentSelection = e.AddedItems[0] as IRawLoader;
        UpdateHistogram();
    }
}
