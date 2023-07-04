using Avalonia.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VolumeRenderer;

public class TfEditorViewModel
{
    public ObservableCollection<TfData> Data { get; } = new();
}

public class TfData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private byte _r, _g, _b, _a;

    public byte Intensity { get; }

    public TfData(byte intensity)
    {
        Intensity = intensity;
    }

    public byte R
    {
        get => _r;
        set
        {
            if (value != _r)
            {
                _r = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    public byte G
    {
        get => _g;
        set
        {
            if (value != _g)
            {
                _g = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    public byte B
    {
        get => _b;
        set
        {
            if (value != _b)
            {
                _b = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    public byte A
    {
        get => _a;
        set
        {
            if (value != _a)
            {
                _a = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    public Brush Color => new SolidColorBrush(new Avalonia.Media.Color(A, R, G, B));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}