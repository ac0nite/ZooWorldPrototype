using System;
using UnityEngine;

namespace ZooWorld.Systems
{
    public sealed class ArenaBounds
    {
        private const float OutsideEpsilon = 0.00001f;
        private readonly Camera _camera;
        private readonly Plane _ground;

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
            if (!TryGetPoint(Vector2.zero, out Vector3 bottomLeft) ||
                !TryGetPoint(Vector2.right, out Vector3 bottomRight) ||
                !TryGetPoint(Vector2.one, out Vector3 topRight) ||
                !TryGetPoint(Vector2.up, out Vector3 topLeft))
            {
                return false;
            }

            Vector3 center = (bottomLeft + bottomRight + topRight + topLeft) * 0.25f;
            bool outside = IsOutside(position, bottomLeft, bottomRight, center) ||
                IsOutside(position, bottomRight, topRight, center) ||
                IsOutside(position, topRight, topLeft, center) ||
                IsOutside(position, topLeft, bottomLeft, center);
            direction = outside ? new Vector3(center.x - position.x, 0, center.z - position.z).normalized : Vector3.zero;
            return outside;
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

        private static bool IsOutside(Vector3 position, Vector3 start, Vector3 end, Vector3 center)
        {
            Vector3 normal = Vector3.Cross(end - start, Vector3.up);
            return Vector3.Dot(position - start, normal) * Vector3.Dot(center - start, normal) < -OutsideEpsilon;
        }
    }
}
