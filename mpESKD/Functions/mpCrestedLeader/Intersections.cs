using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpCrestedLeader;

using System;

internal class Intersections
{
    /// <summary>
    /// Возвращает точку пересечения 2х 2D векторов
    /// </summary>
    internal static Point2d? GetIntersectionBetweenVectors(Point2d point1, Vector2d vector1, Point2d point2, Vector2d vector2)
    {
        if (point1.Equals(point2))
            return null;

        var v1 = point1 + vector1;
        var v2 = point2 + vector2;

        // далее по уравнению прямой по двум точкам

        var x11 = point1.X;
        var y11 = point1.Y;
        var x21 = v1.X;
        var y21 = v1.Y;

        var x12 = point2.X;
        var y12 = point2.Y;
        var x22 = v2.X;
        var y22 = v2.Y;

        var a1 = (y21 - y11) / (x21 - x11);
        var a2 = (y22 - y12) / (x22 - x12);

        var b1 = ((y11 * (x21 - x11)) + (x11 * (y11 - y21))) / (x21 - x11);
        var b2 = ((y12 * (x22 - x12)) + (x12 * (y12 - y22))) / (x22 - x12);

        var x = (b1 - b2) / (a2 - a1);
        var y = (a2 * x) + b2;

        return !double.IsNaN(x) || !double.IsNaN(y) ? new Point2d(x, y) : default;
    }

    internal static Point3d? GetIntersectionBetweenVectors(Point3d point1, Vector2d vector1, Point3d point2,
        Vector2d vector2)
    {

        var point2d = GetIntersectionBetweenVectors(point1.ToPoint2d(), vector1, point2.ToPoint2d(), vector2);

        if (point2d == null) return null;

        return new Point3d(point2d.Value.X, point2d.Value.Y, point1.Z);
    }

    internal static Point3d? GetIntersectionBetweenCircleLine(Line line, Circle circle)
    {
        /*
        double x1 = 1, y1 = 1; // Координаты первого конца отрезка (внутри окружности)
        double x2 = 4, y2 = 4; // Координаты второго конца отрезка (вне окружности)
        double xc = 3, yc = 3; // Координаты центра окружности
        double R = 2;          // Радиус окружности

        var intersection = FindIntersection(x1, y1, x2, y2, xc, yc, R);
        if (intersection != null)
        {
            Console.WriteLine($"Точка пересечения: ({intersection.Item1}, {intersection.Item2})");
        }
        else
        {
            Console.WriteLine("Пересечения нет.");
        }*/

        var x1 = line.EndPoint.X;
        var y1 = line.EndPoint.Y;
        var x2 = line.StartPoint.X;
        var y2 = line.StartPoint.Y;
        var xc = circle.Center.X;
        var yc = circle.Center.Y;
        var r = circle.Radius;

        var intersection = FindIntersection(x1, y1, x2, y2, xc, yc, r);
        if (intersection != null)
        {
            return new Point3d(intersection.Value.Item1, intersection.Value.Item2, line.StartPoint.Z);
        }

        return null;
        
    }

    private static (double, double)? FindIntersection(double x1, double y1, double x2, double y2, double xc, double yc, double r)
    {
        // Коэффициенты квадратного уравнения
        double dx = x2 - x1;
        double dy = y2 - y1;

        double A = dx * dx + dy * dy;
        double B = 2 * (dx * (x1 - xc) + dy * (y1 - yc));
        double C = (x1 - xc) * (x1 - xc) + (y1 - yc) * (y1 - yc) - r * r;

        // Дискриминант
        double D = B * B - 4 * A * C;

        if (D < 0)
        {
            return null;  // Пересечений нет
        }
        else
        {
            double sqrtD = Math.Sqrt(D);
            double t1 = (-B + sqrtD) / (2 * A);
            double t2 = (-B - sqrtD) / (2 * A);

            double t = (t1 >= 0 && t1 <= 1) ? t1 : t2;

            if (t >= 0 && t <= 1)
            {
                // Находим точку пересечения
                double ix = x1 + t * dx;
                double iy = y1 + t * dy;
                return (ix, iy);
            }

            return null;  // Пересечения нет
        }
    }
}
