using SharpDX.Mathematics.Interop;
using System.IO;

namespace VolumeRenderer;

sealed class RawLoader : IDisposable
{
    private bool _disposed;
    public ShaderResourceView RawTextureView { get; }
    public SamplerState SamplerState { get; }

    public RawLoader(D3DDevice device, string path, int x, int y, int z, RawDataType dataType)
    {
        var numberOfData = x * y * z;
        var dataSize = dataType is RawDataType.U8 ? 1 : 2;
        using var dataStream = new DataStream(numberOfData * dataSize, true, true);
        using var file = File.OpenRead(path);
        file.CopyTo(dataStream);

        var description = new Texture3DDescription
        {
            Width = x,
            Height = y,
            Depth = z,
            MipLevels = 1,
            Format = dataType == RawDataType.U8 ? Format.R8_UNorm : Format.R16_UNorm,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        using var texture = new Texture3D(device, description, new DataBox[] { new DataBox(dataStream.DataPointer, x * dataSize, x * y * dataSize) } );
        RawTextureView = new ShaderResourceView(device, texture);

        SamplerState = new SamplerState(device, new SamplerStateDescription
        {
            AddressU = TextureAddressMode.Border,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            Filter = Filter.MinMagMipLinear,
            BorderColor = new RawColor4(0, 0, 0, 0)
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
            RawTextureView?.Dispose();
            SamplerState?.Dispose();
        }
    }

    ~RawLoader()
    {
        Dispose();
    }
}
