using System.Runtime.InteropServices;

namespace VolumeRenderer;

[StructLayout(LayoutKind.Sequential)]
struct MvpConstantBuffer
{
    public Matrix4x4 ModelViewProjection { get; set; }

    public MvpConstantBuffer(Matrix4x4 mvp)
    {
        ModelViewProjection = mvp;
    }
}
