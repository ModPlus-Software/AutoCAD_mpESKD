using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader;

using System;

public class CircleLineIntersection
{
    public static Point3d? GetIntersection(Line line, Circle circle)
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
