using System.Numerics;

namespace Editor.Viewport
{
    internal static class ViewportPicking
    {
        public static bool Box(Vector3 origin, Vector3 direction, Vector3 minimum, Vector3 maximum, out float near, out float far)
        {
            near = 0; far = float.MaxValue;
            for (int axis = 0; axis < 3; axis++)
            {
                float p = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
                float d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
                float a = axis == 0 ? minimum.X : axis == 1 ? minimum.Y : minimum.Z;
                float b = axis == 0 ? maximum.X : axis == 1 ? maximum.Y : maximum.Z;
                if (MathF.Abs(d) < 1e-9f) { if (p < a || p > b) return false; continue; }
                float first = (a - p) / d, last = (b - p) / d;
                near = Math.Max(near, Math.Min(first, last)); far = Math.Min(far, Math.Max(first, last));
                if (far < near) return false;
            }
            return true;
        }

        public static bool Triangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float distance)
        {
            distance = 0;
            Vector3 edge1 = b - a, edge2 = c - a, p = Vector3.Cross(direction, edge2);
            float determinant = Vector3.Dot(edge1, p);
            if (MathF.Abs(determinant) < 1e-8f) return false;
            float inverse = 1 / determinant;
            var t = origin - a;
            float u = Vector3.Dot(t, p) * inverse;
            if (u < 0 || u > 1) return false;
            var q = Vector3.Cross(t, edge1);
            float v = Vector3.Dot(direction, q) * inverse;
            if (v < 0 || u + v > 1) return false;
            distance = Vector3.Dot(edge2, q) * inverse;
            return distance >= 0;
        }
    }
}
