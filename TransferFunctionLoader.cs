using SharpDX.Mathematics.Interop;
using System.IO;

namespace VolumeRenderer;

public sealed class TransferFunctionLoader : IDisposable
{
    private bool _disposed;
    public ShaderResourceView FunctionTextureView { get; }
    public SamplerState SamplerState { get; }
    public string FileName { get; }

    public TransferFunctionLoader(D3DDevice device, string path)
    {
        using var dataStream = new DataStream(256 * 4, true, true);
        using var file = File.OpenRead(path);
        file.CopyTo(dataStream);

        var description = new Texture1DDescription
        {
            Width = 256,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
        };

        using var texture = new Texture1D(device, description, dataStream);
        FunctionTextureView = new ShaderResourceView(device, texture);

        SamplerState = new SamplerState(device, new SamplerStateDescription
        {
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            Filter = Filter.MinMagMipPoint,
            BorderColor = new RawColor4(0, 0, 0, 0)
        });

        FileName = path;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
            FunctionTextureView?.Dispose();
            SamplerState?.Dispose();
        }
    }

    ~TransferFunctionLoader()
    {
        Dispose();
    }
}
