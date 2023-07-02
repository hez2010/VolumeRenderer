namespace VolumeRenderer;

sealed class D3D11Swapchain : SwapchainBase<D3D11SwapchainImage>
{
    private readonly D3DDevice _device;

    public D3D11Swapchain(D3DDevice device, ICompositionGpuInterop interop, CompositionDrawingSurface target)
        : base(interop, target)
    {
        _device = device;
    }

    protected override D3D11SwapchainImage CreateImage(PixelSize size) => new(_device, size, Interop, Target);

    public IDisposable BeginDraw(PixelSize size, out RenderTargetView view)
    {
        var rv = BeginDrawCore(size, out var image);
        view = image.RenderTargetView;
        return rv;
    }
}
