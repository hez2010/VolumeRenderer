using System.Runtime.InteropServices;

namespace VolumeRenderer;

[StructLayout(LayoutKind.Sequential)]
struct RayCastingConstantBuffer
{
    public float StepSize { get; set; }
    public float Padding { get; }
    public Vector2 ScreenSize { get; set; }

    public RayCastingConstantBuffer(float stepSize, Vector2 screenSize)
    {
        StepSize = stepSize;
        ScreenSize = screenSize;
    }
}
