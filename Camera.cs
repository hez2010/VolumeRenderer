namespace VolumeRenderer;
using SharpDX;

class Camera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public float Speed { get; set; } = 0.1f;
    public float Sensitivity { get; set; } = 0.01f;

    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;
    private float _fov;
    private float _aspectRatio;
    private readonly float _nearPlane;
    private readonly float _farPlane;

    public Camera(Vector3 position, float yaw, float pitch, float roll, float fov, float aspectRatio, float nearPlane, float farPlane)
    {
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
        Roll = roll;
        _fov = fov;
        _aspectRatio = aspectRatio;
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        UpdateProjectionMatrix();
        UpdateViewMatrix();
    }

    public void UpdateViewMatrix()
    {
        Quaternion rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, Roll + MathUtil.Pi);
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        _viewMatrix = Matrix4x4.LookAtLH(Position, Position + forward, up);
    }

    public void UpdateAspectRatio(float aspectRatio)
    {
        _aspectRatio = aspectRatio;
    }

    public void UpdateProjectionMatrix()
    {
        _projectionMatrix = Matrix4x4.PerspectiveFovLH(_fov, _aspectRatio, _nearPlane, _farPlane);
    }

    public void Zoom(float delta)
    {
        _fov -= delta * Sensitivity;
        _fov = MathUtil.Clamp(_fov, 0.1f, MathUtil.PiOverTwo);
        UpdateProjectionMatrix();
    }

    public Matrix4x4 GetMVPMatrix(Matrix4x4 model)
    {
        var mvp = model * _viewMatrix * _projectionMatrix;
        return mvp;
    }

    public void Move(CameraMovement direction, float deltaTime)
    {
        float velocity = Speed * deltaTime;
        Quaternion rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, Roll + MathUtil.Pi);
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 right = Vector3.Transform(Vector3.UnitX, rotation);

        if (direction == CameraMovement.Forward) Position += forward * velocity;
        if (direction == CameraMovement.Backward) Position -= forward * velocity;
        if (direction == CameraMovement.Left) Position -= right * velocity;
        if (direction == CameraMovement.Right) Position += right * velocity;

        UpdateViewMatrix();
    }
}
