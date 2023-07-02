using Avalonia.Platform;
using DxgiResource = SharpDX.DXGI.Resource;

namespace VolumeRenderer;

sealed class D3D11SwapchainImage : ISwapchainImage
{
    public PixelSize Size { get; }
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private readonly Texture2D _texture;
    private readonly KeyedMutex _mutex;
    private readonly IntPtr _handle;
    private PlatformGraphicsExternalImageProperties _properties;
    private ICompositionImportedGpuImage? _imported;
    public Task? LastPresent { get; private set; }
    public RenderTargetView RenderTargetView { get; }

    public D3D11SwapchainImage(D3DDevice device, PixelSize size,
        ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        Size = size;
        _interop = interop;
        _target = target;
        _texture = new Texture2D(device,
            new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                CpuAccessFlags = default,
                OptionFlags = ResourceOptionFlags.SharedKeyedmutex,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            });
        _mutex = _texture.QueryInterface<KeyedMutex>();
        using (var res = _texture.QueryInterface<DxgiResource>())
            _handle = res.SharedHandle;
        _properties = new PlatformGraphicsExternalImageProperties
        {
            Width = size.Width,
            Height = size.Height,
            Format = PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
        };

        RenderTargetView = new RenderTargetView(device, _texture);
    }

    public void BeginDraw()
    {
        _mutex.Acquire(0, int.MaxValue);
    }

    public void Present()
    {
        _mutex.Release(1);
        _imported ??= _interop.ImportImage(
            new PlatformHandle(_handle, KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle),
            _properties);
        LastPresent = _target.UpdateWithKeyedMutexAsync(_imported, 1, 0);
    }


    public async ValueTask DisposeAsync()
    {
        if (LastPresent != null)
        {
            try
            {
                await LastPresent;
            }
            catch
            {
                // Ignore
            }
        }

        RenderTargetView.Dispose();
        _mutex.Dispose();
        _texture.Dispose();
    }
}
