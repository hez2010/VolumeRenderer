using Avalonia.Controls;
using Avalonia.Input;

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
}
