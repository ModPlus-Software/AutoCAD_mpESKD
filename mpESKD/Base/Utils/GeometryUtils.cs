﻿namespace mpESKD.Base.Utils;

using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Утилиты работы с геометрией
/// </summary>
public static class GeometryUtils
{
    /// <summary>
    /// Возвращает среднюю точку между двумя указанными 3d точками
    /// </summary>
    /// <param name="firstPoint">Первая 3d точка</param>
    /// <param name="secondPoint">Вторая 3d точка</param>
    public static Point3d GetMiddlePoint3d(Point3d firstPoint, Point3d secondPoint)
    {
        return new Point3d(
            (firstPoint.X + secondPoint.X) / 2,
            (firstPoint.Y + secondPoint.Y) / 2,
            (firstPoint.Z + secondPoint.Z) / 2);
    }

    /// <summary>
    /// Возвращает среднюю точку между двумя указанными 2d точками
    /// </summary>
    /// <param name="firstPoint">Первая 2d точка</param>
    /// <param name="secondPoint">Вторая 2d точка</param>
    public static Point2d GetMiddlePoint2d(Point2d firstPoint, Point2d secondPoint)
    {
        return new Point2d(
            (firstPoint.X + secondPoint.X) / 2,
            (firstPoint.Y + secondPoint.Y) / 2);
    }

    /// <summary>
    /// Представляет точку в виде строки
    /// </summary>
    /// <param name="point3d">Точка</param>
    public static string AsString(this Point3d point3d)
    {
        return $"{point3d.X}${point3d.Y}${point3d.Z}";
    }

    /// <summary>
    /// Представляет вектор в виде строки
    /// </summary>
    /// <param name="vector3d">Вектор</param>
    public static string AsString(this Vector3d vector3d)
    {
        return $"{vector3d.X}${vector3d.Y}${vector3d.Z}";
    }

    /// <summary>
    /// Преобразует строку в 3d точку
    /// </summary>
    /// <param name="str">Строка</param>
    public static Point3d ParseToPoint3d(this string str)
    {
        if (!string.IsNullOrEmpty(str))
        {
            var splitted = str.Split('$');
            if (splitted.Length == 3)
            {
                try
                {
                    return new Point3d(
                        double.Parse(splitted[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture),
                        double.Parse(splitted[1].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture),
                        double.Parse(splitted[2].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture));
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed parse form \"{str}\": {exception.Message}");
                }
            }
        }

        return Point3d.Origin;
    }

    /// <summary>
    /// Конвертация Point3d в Point2d путем отбрасывания Z
    /// </summary>
    /// <param name="point3d">3d точка</param>
    public static Point2d ToPoint2d(this Point3d point3d)
    {
        return new Point2d(point3d.X, point3d.Y);
    }

    /// <summary>
    /// Конвертация Point2d в Point3d с заданием Z = 0.0
    /// </summary>
    /// <param name="point2d">2d точка</param>
    public static Point3d ToPoint3d(this Point2d point2d)
    {
        return new Point3d(point2d.X, point2d.Y, 0.0);
    }

    /// <summary>
    /// Возвращает пару наиболее удаленных друг от друга точек
    /// </summary>
    /// <param name="points">Коллекция точек</param>
    /// <returns></returns>
    public static Tuple<Point3d, Point3d> GetFurthestPoints(this IList<Point3d> points)
    {
        Tuple<Point3d, Point3d> result = default;
        var dist = double.NaN;
        for (int i = 0; i < points.Count; i++)
        {
            var pt1 = points[i];
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j)
                    continue;
                var pt2 = points[j];
                var d = pt1.DistanceTo(pt2);
                if (double.IsNaN(dist) || d > dist)
                {
                    result = new Tuple<Point3d, Point3d>(pt1, pt2);
                    dist = d;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 3Д точка по направлению. Направление берется как единичный вектор из точки pt2 к точке pt1, перемножается на указанную
    /// длину и откладывается от точки pt1. Если между точками нулевое расстояние, будет использован вектор <see cref="Vector3d.XAxis"/>
    /// </summary>
    /// <param name="pt1">Первая точка для получения единичного вектора</param>
    /// <param name="pt2">Вторая точка для получения единичного вектора</param>
    /// <param name="length">Расстояние на котором нужно получить точку</param>
    /// <returns>3Д точка</returns>
    public static Point3d Point3dAtDirection(Point3d pt1, Point3d pt2, double length) => 
        Point3dAtDirection(pt1, pt2, length, Vector3d.XAxis);

    /// <summary>
    /// 3Д точка по направлению. Направление берется как единичный вектор из точки pt2 к точке pt1, перемножается на указанную
    /// длину и откладывается от точки pt1
    /// </summary>
    /// <param name="pt1">Первая точка для получения единичного вектора</param>
    /// <param name="pt2">Вторая точка для получения единичного вектора</param>
    /// <param name="length">Расстояние на котором нужно получить точку</param>
    /// <param name="zeroCaseVector">Единичный вектор, используемый если между точками нулевое расстояние</param>
    /// <returns>3Д точка</returns>
    public static Point3d Point3dAtDirection(Point3d pt1, Point3d pt2, double length, Vector3d zeroCaseVector)
    {
        var vector3d = (pt2 - pt1).GetNormal();
        if (vector3d.IsZeroLength())
            vector3d = zeroCaseVector;
        return pt1 + (vector3d * length);
    }
}