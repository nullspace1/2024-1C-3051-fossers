using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WarSteel.Entities;
using WarSteel.Scenes;

namespace WarSteel.Common;

public class Camera
{
    public Matrix Projection { get; private set; }
    public Matrix View { get; private set; }
    public Transform Transform;
    private MouseState _previousMouseState;

    private GameObject _followed;

    private Vector3 _offset;

    private float _smoothSpeed = 0.8f;

    private float _rotationSpeed = 3f;

    private float _verticalOffset = 400f;

    private float _currentPitch = 0;

    private float _currentYaw = 0;

    private const float defaultNearPlaneDistance = 0.1f;
    private const float defaultFarPlaneDistance = 1000f;
    private const float defaultFOV = MathHelper.PiOver2;

    public Camera(Vector3 offset, float aspectRatio, float fov = defaultFOV, float nearPlaneDistance = defaultNearPlaneDistance, float farPlaneDistance = defaultFarPlaneDistance)
    {
        _offset = offset;
        Transform = new Transform
        {
            Position = _offset
        };
        Projection = Matrix.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlaneDistance, farPlaneDistance);
    }


    public void Follow(GameObject obj)
    {
        Transform.Position = obj.Transform.AbsolutePosition + _offset;
        Transform.Orientation = Quaternion.CreateFromYawPitchRoll(_currentPitch, _currentYaw, 0);
        _followed = obj;
        _previousMouseState = Mouse.GetState();
    }

    public void StopFollowing()
    {
        _followed = null;
    }

    public void Update(Scene scene, GameTime gameTime)
    {
        if (_followed != null)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            MouseState state = Mouse.GetState();

            float deltaX = state.X - _previousMouseState.X;
            float deltaY = state.Y - _previousMouseState.Y;

            _currentYaw -= deltaX * _rotationSpeed * dt;
            _currentPitch -= deltaY * _rotationSpeed * dt;

            _currentPitch = Math.Clamp(_currentPitch, -40, 80);

            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(_currentYaw, _currentPitch, 0);
            Vector3 desiredPosition = _followed.Transform.AbsolutePosition + Vector3.Transform(_offset, Matrix.CreateFromQuaternion(rotation));
            desiredPosition.Y = MathHelper.Max(desiredPosition.Y, _followed.Transform.AbsolutePosition.Y + 200);

            Vector3 smoothedPosition = Vector3.Lerp(Transform.AbsolutePosition, desiredPosition, _smoothSpeed);
            Transform.Position = smoothedPosition;
            Transform.LookAt(_followed.Transform.AbsolutePosition + Vector3.Up * _verticalOffset);

            _previousMouseState = state;

        }

        View = Transform.View;
    }
}
