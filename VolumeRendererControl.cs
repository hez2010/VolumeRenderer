using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using SharpDX.Direct3D;
using Avalonia.VisualTree;
using Buffer = SharpDX.Direct3D11.Buffer;
using Avalonia.LogicalTree;

namespace VolumeRenderer;

public sealed class VolumeRendererControl : Control
{
    private CompositionSurfaceVisual? _visual;
    private Compositor? _compositor;
    private CompositionDrawingSurface? _surface;

    private D3DDevice? _device;
    private DeviceContext? _context;
    private D3D11Swapchain? _swapchain;

    private bool _initialized, _updateQueued;

    private Camera? _camera;
    private Shader<MvpConstantBuffer, RayCastingConstantBuffer>? _rayCastingShader;
    private Shader<MvpConstantBuffer>? _cubeShader;
    private TransferFunctionLoader? _transferFunctionLoader;
    private RawLoader? _rawLoader;
    private RayGenerator? _rayGenerator;
    private Buffer? _vertexBuffer, _indexBuffer;

    private long _lastTime;
    private float _deltaTime;
    private int _framesPerSecond;

    public static readonly DirectProperty<VolumeRendererControl, string?> InfoProperty =
        AvaloniaProperty.RegisterDirect<VolumeRendererControl, string?>(nameof(Info), o => o.Info, (o, v) => o.Info = v);

    private string? _info;
    public string? Info
    {
        get => _info;
        set => SetAndRaise(InfoProperty, ref _info, value);
    }

    public static readonly DirectProperty<VolumeRendererControl, float> YawProperty =
        AvaloniaProperty.RegisterDirect<VolumeRendererControl, float>(nameof(Yaw), o => o.Yaw, (o, v) => o.Yaw = v);

    private float _yaw;
    public float Yaw
    {
        get => _yaw;
        set
        {
            SetAndRaise(YawProperty, ref _yaw, value);
            if (_camera is not null)
            {
                _camera.Yaw = MathUtil.DegreesToRadians(value);
                _camera.UpdateViewMatrix();
            }
        }
    }

    public static readonly DirectProperty<VolumeRendererControl, float> PitchProperty =
        AvaloniaProperty.RegisterDirect<VolumeRendererControl, float>(nameof(Pitch), o => o.Pitch, (o, v) => o.Pitch = v);

    private float _pitch;
    public float Pitch
    {
        get => _pitch;
        set
        {
            SetAndRaise(PitchProperty, ref _pitch, value);
            if (_camera is not null)
            {
                _camera.Pitch = MathUtil.DegreesToRadians(value);
                _camera.UpdateViewMatrix();
            }
        }
    }

    public static readonly DirectProperty<VolumeRendererControl, float> RollProperty =
        AvaloniaProperty.RegisterDirect<VolumeRendererControl, float>(nameof(Roll), o => o.Roll, (o, v) => o.Roll = v);

    private float _roll;

    public float Roll
    {
        get => _roll;
        set
        {
            SetAndRaise(RollProperty, ref _roll, value);
            if (_camera is not null)
            {
                _camera.Roll = MathUtil.DegreesToRadians(value);
                _camera.UpdateViewMatrix();
            }
        }
    }


    private int _fps;

    public static readonly DirectProperty<VolumeRendererControl, int> FpsProperty =
        AvaloniaProperty.RegisterDirect<VolumeRendererControl, int>(nameof(Fps), o => o.Fps);

    public int Fps
    {
        get => _fps;
        private set => SetAndRaise(FpsProperty, ref _fps, value);
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        await InitializeAsync();
    }

    protected override async void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_initialized)
        {
            _initialized = false;

            if (_swapchain is not null)
            {
                await _swapchain.DisposeAsync();
                _swapchain = null;
            }

            _surface?.Dispose();
            _surface = null;

            _rayCastingShader?.Dispose();
            _rayCastingShader = null;
            _cubeShader?.Dispose();
            _cubeShader = null;
            _transferFunctionLoader?.Dispose();
            _transferFunctionLoader = null;
            _rawLoader?.Dispose();
            _rawLoader = null;
            _rayGenerator?.Dispose();
            _rayGenerator = null;
            _vertexBuffer?.Dispose();
            _vertexBuffer = null;
            _indexBuffer?.Dispose();
            _indexBuffer = null;

            _camera = null;

            _context?.Dispose();
            _context = null;
            _device?.Dispose();
            _device = null;
        }

        base.OnDetachedFromLogicalTree(e);
    }

    public async Task InitializeAsync()
    {
        try
        {
            var selfVisual = ElementComposition.GetElementVisual(this)!;
            _compositor = selfVisual.Compositor;

            _surface = _compositor.CreateDrawingSurface();
            _visual = _compositor.CreateSurfaceVisual();
            _visual.Size = new(Bounds.Width, Bounds.Height);
            _visual.Surface = _surface;
            ElementComposition.SetElementChildVisual(this, _visual);
            var (result, info) = await InitializeCoreAsync(_compositor, _surface);
            Info = info;
            _initialized = result;
            QueueNextFrame();
        }
        catch (Exception e)
        {
            Info = e.ToString();
        }
    }

    private async Task<(bool success, string info)> InitializeCoreAsync(Compositor compositor,
        CompositionDrawingSurface surface)
    {
        var root = this.GetVisualRoot();
        if (root == null)
            return (false, "Not visual root is present.");

        var interop = await compositor.TryGetCompositionGpuInterop();
        if (interop == null)
            return (false, "Compositor doesn't support interop for the current backend.");

        if (interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                .D3D11TextureGlobalSharedHandle) != true)
            return (false, "DXGI shared handle import is not supported by the current graphics backend.");

        using var factory = new DxgiFactory();
        using var adapter = factory.GetAdapter1(0);

        try
        {
            _device = new D3DDevice(adapter, DeviceCreationFlags.Debug, new[]
            {
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1,
            });

            _context = _device.ImmediateContext;
            _swapchain = new D3D11Swapchain(_device, interop, surface);

            _vertexBuffer = Buffer.Create(_device, BindFlags.VertexBuffer, new[] {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 1.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 1.0f)
            });
            _indexBuffer = Buffer.Create(_device, BindFlags.IndexBuffer, new[] {
                1, 5, 7, 7, 3, 1,
                0, 2, 6, 6, 4, 0,
                0, 1, 3, 3, 2, 0,
                7, 5, 4, 4, 6, 7,
                2, 3, 7, 7, 6, 2,
                1, 0, 4, 4, 5, 1
            });

            var pixelSize = PixelSize.FromSize(Bounds.Size, root.RenderScaling);
            _camera = new Camera(new(0, 0, -2), 0, 0, 0, MathUtil.PiOverFour, (float)pixelSize.Width / pixelSize.Height, 0.1f, 800.0f);
            _cubeShader = new Shader<MvpConstantBuffer>(_device, "shaders/cube.v.hlsl", "shaders/cube.p.hlsl");
            _rayCastingShader = new Shader<MvpConstantBuffer, RayCastingConstantBuffer>(_device, "shaders/ray_casting.v.hlsl", "shaders/ray_casting.p.hlsl");
            _transferFunctionLoader = new TransferFunctionLoader(_device, "data/transferfunction/transfer_function.dat");
            _rawLoader = new RawLoader(_device, "data/raw/bonsai_256x256x256_uint8.raw", 256, 256, 256, RawDataType.U8);
            _rayGenerator = new RayGenerator(_device, pixelSize.Width, pixelSize.Height);
        }
        catch (Exception ex)
        {
            return (false, ex.ToString());
        }
        return (true, $"{_device.FeatureLevel} {adapter.Description1.Description}");
    }

    private void UpdateFrame()
    {
        _updateQueued = false;
        var root = this.GetVisualRoot();
        if (root == null)
            return;

        _visual!.Size = new(Bounds.Width, Bounds.Height);
        RenderFrame(PixelSize.FromSize(Bounds.Size, root.RenderScaling));
        QueueNextFrame();
    }

    private void QueueNextFrame()
    {
        if (_initialized && !_updateQueued && _compositor != null)
        {
            _updateQueued = true;
            _compositor?.RequestCompositionUpdate(UpdateFrame);
        }
    }

    private void RenderFrame(PixelSize pixelSize)
    {
        if (_rayCastingShader is null ||
            _cubeShader is null ||
            _transferFunctionLoader is null ||
            _rawLoader is null ||
            _rayGenerator is null ||
            _camera is null ||
            _context is null ||
            _swapchain is null)
        {
            throw new InvalidOperationException("Volume renderer is not initialized.");
        }

        using var draw = _swapchain.BeginDraw(pixelSize, out var renderTargetView);

        var mvp = _camera.GetMVPMatrix(Matrix4x4.Identity);
        var viewport = new ViewportF(0, 0, pixelSize.Width, pixelSize.Height);
        _context.Rasterizer.SetViewport(viewport);

        _context.OutputMerger.DepthStencilState = _rayGenerator.FrontFaceDepthStencilState;

        _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector3>(), 0));
        _context.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
        _cubeShader.Use(_context);
        _cubeShader.SetVertexConstantBuffer(_context, new(mvp));

        _context.ClearRenderTargetView(_rayGenerator.FrontFaceRenderTargetView, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
        _context.ClearDepthStencilView(_rayGenerator.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        _context.OutputMerger.SetTargets(_rayGenerator.DepthStencilView, _rayGenerator.FrontFaceRenderTargetView);
        _context.Rasterizer.State = new RasterizerState(_device, new RasterizerStateDescription
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid
        });

        _context.DrawIndexed(36, 0, 0);

        _context.OutputMerger.DepthStencilState = _rayGenerator.BackFaceDepthStencilState;

        _context.ClearRenderTargetView(_rayGenerator.BackFaceRenderTargetView, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
        _context.ClearDepthStencilView(_rayGenerator.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        _context.OutputMerger.SetTargets(_rayGenerator.DepthStencilView, _rayGenerator.BackFaceRenderTargetView);
        _context.Rasterizer.State = new RasterizerState(_device, new RasterizerStateDescription
        {
            CullMode = CullMode.Front,
            FillMode = FillMode.Solid
        });

        _context.DrawIndexed(36, 0, 0);

        _context.Rasterizer.State = new RasterizerState(_device, new RasterizerStateDescription
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid
        });

        _context.ClearRenderTargetView(renderTargetView, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
        _context.ClearDepthStencilView(_rayGenerator.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        _context.OutputMerger.SetRenderTargets(_rayGenerator.DepthStencilView, renderTargetView);
        _rayCastingShader.Use(_context);

        _rayCastingShader.SetVertexConstantBuffer(_context, new(mvp));
        _rayCastingShader.SetPixelConstantBuffer(_context, new(0.01f, new(pixelSize.Width, pixelSize.Height)));

        _context.PixelShader.SetSampler(0, _rayGenerator.SamplerState);
        _context.PixelShader.SetSampler(1, _rawLoader.SamplerState);
        _context.PixelShader.SetSampler(2, _transferFunctionLoader.SamplerState);

        _context.PixelShader.SetShaderResource(0, _rayGenerator.FrontFaceTextureView);
        _context.PixelShader.SetShaderResource(1, _rayGenerator.BackFaceTextureView);
        _context.PixelShader.SetShaderResource(2, _rawLoader.RawTextureView);
        _context.PixelShader.SetShaderResource(3, _transferFunctionLoader.FunctionTextureView);

        _context.DrawIndexed(36, 0, 0);

        CountFps();
    }

    private void CountFps()
    {
        var currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        ++_framesPerSecond;
        _deltaTime = (currentTime - _lastTime) / 1000.0f;
        if (currentTime - _lastTime > 500)
        {
            _lastTime = currentTime;
            Fps = (int)(_framesPerSecond / _deltaTime);
            _framesPerSecond = 0;
        }
    }

    public void PressKey(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
                _camera?.Move(CameraMovement.Forward, _deltaTime);
                break;
            case Key.S:
                _camera?.Move(CameraMovement.Backward, _deltaTime);
                break;
            case Key.A:
                _camera?.Move(CameraMovement.Left, _deltaTime);
                break;
            case Key.D:
                _camera?.Move(CameraMovement.Right, _deltaTime);
                break;

        }
    }

    public void ChangePointerWheel(float yOffset)
    {
        _camera?.Zoom(yOffset);
    }
}
