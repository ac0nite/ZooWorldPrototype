using System;
using UnityEngine;

namespace ZooWorld.Systems
{
    public sealed class ArenaBounds
    {
        private const float OutsideEpsilon = 0.00001f;
        private readonly Camera _camera;
        private readonly Plane _ground;
        private int _cachedFrame = -1;
        private float _cachedOrthoSize;
        private float _cachedAspect;
        private bool _cachedOrthographic;
        private bool _cornersValid;
        private Vector3 _bottomLeft;
        private Vector3 _bottomRight;
        private Vector3 _topRight;
        private Vector3 _topLeft;
        private Vector3 _center;

        public ArenaBounds(Camera camera, float groundHeight = 0)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            if (float.IsNaN(groundHeight) || float.IsInfinity(groundHeight))
            {
                throw new ArgumentOutOfRangeException(nameof(groundHeight));
            }

            _ground = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
        }

        public Vector3 GetPoint(Vector2 normalizedPosition)
        {
            if (float.IsNaN(normalizedPosition.x) || float.IsNaN(normalizedPosition.y) ||
                normalizedPosition.x < 0 || normalizedPosition.x > 1 ||
                normalizedPosition.y < 0 || normalizedPosition.y > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedPosition));
            }

            if (!TryGetPoint(normalizedPosition, out Vector3 point))
            {
                throw new InvalidOperationException("Arena camera must face the ground plane.");
            }

            return point;
        }

        public bool TryGetInwardDirection(Vector3 position, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (!TryGetCorners())
            {
                return false;
            }

            bool outside = IsOutside(position, _bottomLeft, _bottomRight) ||
                IsOutside(position, _bottomRight, _topRight) ||
                IsOutside(position, _topRight, _topLeft) ||
                IsOutside(position, _topLeft, _bottomLeft);
            direction = outside ? new Vector3(_center.x - position.x, 0, _center.z - position.z).normalized : Vector3.zero;
            return outside;
        }

        private bool TryGetCorners()
        {
            if (_cachedFrame != Time.frameCount || _camera.transform.hasChanged ||
                _cachedOrthoSize != _camera.orthographicSize ||
                _cachedAspect != _camera.aspect ||
                _cachedOrthographic != _camera.orthographic)
            {
                _cornersValid = TryGetPoint(Vector2.zero, out _bottomLeft) &&
                    TryGetPoint(Vector2.right, out _bottomRight) &&
                    TryGetPoint(Vector2.one, out _topRight) &&
                    TryGetPoint(Vector2.up, out _topLeft);
                _center = (_bottomLeft + _bottomRight + _topRight + _topLeft) * 0.25f;
                _camera.transform.hasChanged = false;
                _cachedFrame = Time.frameCount;
                _cachedOrthoSize = _camera.orthographicSize;
                _cachedAspect = _camera.aspect;
                _cachedOrthographic = _camera.orthographic;
            }

            return _cornersValid;
        }

        private bool TryGetPoint(Vector2 normalizedPosition, out Vector3 point)
        {
            point = Vector3.zero;
            Ray ray = _camera.ViewportPointToRay(new Vector3(normalizedPosition.x, normalizedPosition.y, 0));
            if (!_ground.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        private bool IsOutside(Vector3 position, Vector3 start, Vector3 end)
        {
            Vector3 normal = Vector3.Cross(end - start, Vector3.up);
            return Vector3.Dot(position - start, normal) * Vector3.Dot(_center - start, normal) < -OutsideEpsilon;
        }
    }
}
