using SharpDX.Mathematics.Interop;
using System.IO;

namespace VolumeRenderer;

sealed class TransferFunctionLoader : IDisposable
{
    private bool _disposed;
    public ShaderResourceView FunctionTextureView { get; }
    public SamplerState SamplerState { get; }

    public TransferFunctionLoader(D3DDevice device, string path)
    {
        Span<byte> transferFunctionData = new byte[256 * 4];
        using var file = File.OpenRead(path);
        file.ReadExactly(transferFunctionData);

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

        DataStream data;
        unsafe
        {
            fixed (byte* ptr = transferFunctionData)
            {
                data = new DataStream((nint)ptr, 256 * 4, true, false);
            }
        }

        using var texture = new Texture1D(device, description, data);
        FunctionTextureView = new ShaderResourceView(device, texture);

        SamplerState = new SamplerState(device, new SamplerStateDescription
        {
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            Filter = Filter.MinMagMipPoint,
            BorderColor = new RawColor4(0, 0, 0, 0)
        });
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
