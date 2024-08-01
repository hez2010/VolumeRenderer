using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VolumeRenderer;

sealed class RawLoader<T> : IRawLoader where T : unmanaged
{
    private bool _disposed;
    private readonly Dictionary<T, int> _histogram = new();

    public ShaderResourceView RawTextureView { get; }
    public SamplerState SamplerState { get; }
    public ICollection<double> HistogramX { get; } = new List<double>();
    public ICollection<double> HistogramY { get; } = new List<double>();
    public string Name { get; }
    public TransferFunctionLoader TransferFunction { get; set; }

    public RawLoader(D3DDevice device, TransferFunctionLoader tfLoader, string path, int x, int y, int z, int seq)
    {
        if (seq < 0 || seq >= 3) throw new ArgumentOutOfRangeException(nameof(seq));

        var numberOfData = x * y * z;
        var dataSize = Unsafe.SizeOf<T>();
        using var dataStream = new DataStream(numberOfData * dataSize, true, true);
        using var file = File.OpenRead(path);
        file.CopyTo(dataStream);

        var description = new Texture3DDescription
        {
            Width = x,
            Height = y,
            Depth = z,
            MipLevels = 1,
            Format = Format.R8_UNorm,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        using var texture = new Texture3D(device, description, [new DataBox(dataStream.DataPointer, x * dataSize, x * y * dataSize)]);
        RawTextureView = new ShaderResourceView(device, texture);

        SamplerState = new SamplerState(device, new SamplerStateDescription
        {
            AddressU = TextureAddressMode.Border,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            Filter = Filter.MinMagMipLinear,
            BorderColor = new RawColor4(0, 0, 0, 0)
        });

        unsafe
        {
            var baseAddr = (T*)dataStream.DataPointer.ToPointer();
            int index = 0;
            for (var i = 0; i < x; i++)
            {
                for (var j = 0; j < y; j++)
                {
                    for (var k = 0; k < z; k++)
                    {
                        var intensity = Unsafe.Read<T>(baseAddr + index);
                        CollectionsMarshal.GetValueRefOrAddDefault(_histogram, intensity, out _)++;
                        index++;
                    }
                }
            }
        }

        foreach (var (k, v) in _histogram.OrderBy(h => h.Key))
        {
            HistogramX.Add(Convert.ToDouble(k));
            HistogramY.Add(v);
        }

        Name = $"{seq switch { 0 => "B", 1 => "G", 2 => "R", _ => throw new UnreachableException() }} - {Path.GetFileName(path)}";
        TransferFunction = tfLoader;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
            RawTextureView?.Dispose();
            SamplerState?.Dispose();
            TransferFunction?.Dispose();
        }
    }

    ~RawLoader()
    {
        Dispose();
    }
}
