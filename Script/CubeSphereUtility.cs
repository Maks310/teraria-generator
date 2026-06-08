using UnityEngine;

namespace TerariaGenerator.Planets
{
    public static class CubeSphereUtility
    {
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };

        public static Vector3 FaceNormal(int faceIndex) => FaceNormals[Mathf.Clamp(faceIndex, 0, FaceNormals.Length - 1)];

        public static Vector3 CubeToSphere(Vector3 cubePoint)
        {
            float x2 = cubePoint.x * cubePoint.x;
            float y2 = cubePoint.y * cubePoint.y;
            float z2 = cubePoint.z * cubePoint.z;
            return new Vector3(
                cubePoint.x * Mathf.Sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f),
                cubePoint.y * Mathf.Sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f),
                cubePoint.z * Mathf.Sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f)).normalized;
        }

        public static Vector3 PointOnFace(int faceIndex, float u, float v)
        {
            Vector3 normal = FaceNormal(faceIndex);
            Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
            Vector3 axisB = Vector3.Cross(normal, axisA);
            Vector3 cubePoint = normal + (u * 2f - 1f) * axisA + (v * 2f - 1f) * axisB;
            return CubeToSphere(cubePoint);
        }
    }
}
