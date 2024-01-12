﻿namespace mpESKD.Functions.mpConcreteJoint;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Шов бетонирования
/// </summary>
[SmartEntityDisplayNameKey("h191")]
[SystemStyleDescriptionKey("h196")]
public class ConcreteJoint : SmartLinearEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcreteJoint"/> class.
    /// </summary>
    public ConcreteJoint()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcreteJoint"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public ConcreteJoint(ObjectId objectId)
        : base(objectId)
    {
    }

    private List<Point3d> MiddlePointsOCS
    {
        get
        {
            var points = new List<Point3d>();
            MiddlePoints.ForEach(p => points.Add(p.TransformBy(BlockTransform.Inverse())));
            return points;
        }
    }

    #region Properties

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 5.0;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 4, "p35", "Continuous", descLocalKey: "d35")]
    public override string LineType { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 5, "p6", 1.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    /// Не используется!
    public override string TextStyle { get; set; }

    /// <summary>
    /// Ширина излома
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p116", 6, 1, 20, nameSymbol: "w")]
    [SaveToXData]
    public double BreakWidth { get; set; } = 6;

    /// <summary>
    /// Высота излома
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p116", 4, 1, 20, nameSymbol: "h")]
    [SaveToXData]
    public double BreakHeight { get; set; } = 4;

    /// <summary>
    /// Видимость отрезков по краям сегментов
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p117", false, descLocalKey: "p118")]
    [SaveToXData]
    public bool EdgesVisible { get; set; } = false;

    /// <summary>
    /// Ширина отрезков по краям сегментов
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p117", 2, 2, 30, nameSymbol: "e")]
    [SaveToXData]
    public double EdgesWidth { get; set; } = 2;

    #endregion

    #region Geometry

    /// <summary>
    /// Коллекция линий-изломов
    /// </summary>
    private List<Line> _lines = new();

    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();
            entities.AddRange(_lines);
            foreach (var e in entities)
            {
                SetChangeablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var middlePoint in MiddlePoints)
        {
            yield return middlePoint;
        }
    }

    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS, scale);
            }
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
    {
        _lines.Clear();

        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);

        // точка обрыва излома в конце предыдущего сегмента = точка начала излома текущего сегмента
        (Point2d, double) pointStartBreak = (insertionPoint.ToPoint2d(), 0.0);

        // для каждой пары точек из points
        for (var i = 0; i < points.Count; i++)
        {
            if (i + 1 < points.Count)
                pointStartBreak = CreateBreakBlocksBetween2Points(points[i], points[i + 1], pointStartBreak, scale);
        }

        foreach (var e in _lines)
        {
            SetChangeablePropertiesToNestedEntity(e);
        }
    }

    /// <summary>
    /// Возвращает точку пересечения 2х 2D векторов
    /// </summary>
    private Point2d GetIntersectBetweenVectors(Point2d point1, Vector2d vector1, Point2d point2, Vector2d vector2)
    {
        var v1 = point1 + vector1;
        var v2 = point2 + vector2;

        // далее по уравнению прямой по двум точкам

        var x1_1 = point1.X;
        var y1_1 = point1.Y;
        var x2_1 = v1.X;
        var y2_1 = v1.Y;

        var x1_2 = point2.X;
        var y1_2 = point2.Y;
        var x2_2 = v2.X;
        var y2_2 = v2.Y;

        var a1 = (y2_1 - y1_1) / (x2_1 - x1_1);
        var a2 = (y2_2 - y1_2) / (x2_2 - x1_2);

        var b1 = ((y1_1 * (x2_1 - x1_1)) + (x1_1 * (y1_1 - y2_1))) / (x2_1 - x1_1);
        var b2 = ((y1_2 * (x2_2 - x1_2)) + (x1_2 * (y1_2 - y2_2))) / (x2_2 - x1_2);

        var x = (b1 - b2) / (a2 - a1);
        var y = (a2 * x) + b2;

        return !double.IsNaN(x) || !double.IsNaN(y) ? new Point2d(x, y) : default;
    }

    /// <summary>
    /// Отрисовка изломов между крайними точками сегмента
    /// </summary>
    /// <returns>кортеж (точка обрыва излома в конце сегмента , длина неполного излома в конце сегмента в о.е.)</returns>
    private (Point2d, double) CreateBreakBlocksBetween2Points(Point2d point1, Point2d point2, (Point2d, double) pointStartBreak, double scale)
    {
        var result = (point2, 0.0);

        var normalVector = (point2 - point1).GetNormal();
        var perpendicularVector = normalVector.GetPerpendicularVector();

        // вектор длиной, равной ширине излома, в направлении линии шва
        var breakNormalVector = normalVector * BreakWidth * scale;

        // вектор длиной, равной высоте излома, перпендикулярный линии шва
        var breakPerpendicularVector = perpendicularVector * BreakHeight * scale;

        // точка на средней линии, на кот. опускается перпендикуляр от точки pointStartBreak
        Point2d pointStart;

        // Если первая точка на средней линии шва не совпадает с последней точкой излома предыдущего участка
        if (!point1.Equals(pointStartBreak.Item1))
        {
            pointStart = GetIntersectBetweenVectors(pointStartBreak.Item1, perpendicularVector, point1, normalVector);

            // если нет наклона текущего участка по отношению к предыдущему
            if (pointStart == default)
            {
                pointStart = point1;
            }

            // отрисовка излома-префикса и определение точки начала отрисовки основного блока изломов
            pointStart = pointStart + normalVector * DrawPrefixBreak(pointStart, pointStartBreak, scale, normalVector, breakPerpendicularVector);
        }
        else
        {
            if (EdgesVisible)
            {
                pointStart = point1 + (normalVector * EdgesWidth);
                _lines.Add(new Line(point1.ToPoint3d(), pointStart.ToPoint3d()));
            }
            else
            {
                pointStart = point1;
            }
        }

        // длина отрезка между точками
        var length = pointStart.GetDistanceTo(point2);// - (EdgesVisible ? EdgesWidth : 0);

        // количество целых изломов
        int breakBlocksCount = (int)(length / BreakWidth / scale);

        if (breakBlocksCount > 0)
        {
            // массив граничных точек изломов
            Point2d[] limitBlockPoints = new Point2d[breakBlocksCount + 1];
            for (int i = 0; i < limitBlockPoints.Count(); i++)
            {
                limitBlockPoints[i] = i == 0 ? pointStart : pointStart + (normalVector * BreakWidth * i * scale);
            }

            for (int i = 0; i < limitBlockPoints.Count() - 1; i++)
            {
                // точка на 1/4 отрезка излома
                var point12start = limitBlockPoints[i] + (breakNormalVector * 0.25);

                // отложить от нее перпендикуляр на расст h/2
                var point12 = point12start + (breakPerpendicularVector * 0.50);

                // точка на 3/4 отрезка излома
                var point14start = limitBlockPoints[i] + (breakNormalVector * 0.75);

                // отложить от нее перпендикуляр на расст -h/2
                var point14 = point14start + (breakPerpendicularVector.Negate() * 0.50);

                _lines.Add(new Line(limitBlockPoints[i].ToPoint3d(), point12.ToPoint3d()));
                _lines.Add(new Line(point12.ToPoint3d(), point14.ToPoint3d()));
                _lines.Add(new Line(point14.ToPoint3d(), limitBlockPoints[i + 1].ToPoint3d()));
            }

            // длина неполного излома в конце сегмента
            var remnant = length - (breakBlocksCount * BreakWidth * scale);

            if (remnant > 0)
            {
                result = DrawSuffixBreak(limitBlockPoints, remnant, scale, breakNormalVector,
                    breakPerpendicularVector, normalVector, perpendicularVector);
            }
            else if (EdgesVisible)
            {
                _lines.Add(new Line(limitBlockPoints.Last().ToPoint3d(), point2.ToPoint3d()));
            }
        }

        return result;
    }

    /// <summary>
    /// Отрисовка префикса (неполного излома в начале сегмента)
    /// </summary>
    private double DrawPrefixBreak(Point2d pointStart, (Point2d, double) pointStartBreak,
        double scale, Vector2d normalVector, Vector2d breakPerpendicularVector)
    {
        var h = BreakHeight * scale;
        var w = BreakWidth * scale;

        var revativePrevRemant = pointStartBreak.Item2;

        var distanceStartBreakPointToStartPoint = pointStartBreak.Item1.GetDistanceTo(pointStart);

        // точка на середине излома
        Point2d point3;

        // точка излома на 3/4 ширины излома от начала
        Point2d point4Start;

        // точка излома на 3/4 ширины излома от начала, вниз на половину высоты
        Point2d point4;

        Point2d endPoint;

        switch (revativePrevRemant)
        {
            case var x when x < 0.25:

                double distanceToPoint2;
                if (normalVector.GetAngleTo(Vector2d.XAxis).RadianToDegree() <= 90)
                    distanceToPoint2 = (h / 2) - distanceStartBreakPointToStartPoint;
                else
                    distanceToPoint2 = (h / 2) + distanceStartBreakPointToStartPoint;

                var distanceToPoint2Start = w / (2 * h) * distanceToPoint2;

                // точка излома на 1/4 ширины излома от начала
                var point2Start = pointStartBreak.Item1 + (normalVector * distanceToPoint2Start);

                // точка излома на 1/4 ширины излома от начала, вверх на половину высоты
                var point2 = point2Start + (normalVector.GetPerpendicularVector() * distanceToPoint2);

                point3 = pointStart + (normalVector * (distanceToPoint2Start + (0.25 * w)));

                point4Start = pointStart + (normalVector * (distanceToPoint2Start + (0.50 * w)));

                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);

                endPoint = pointStart + (normalVector * (distanceToPoint2Start + (0.75 * w)));

                _lines.Add(new Line(pointStartBreak.Item1.ToPoint3d(), point2.ToPoint3d()));
                _lines.Add(new Line(point2.ToPoint3d(), point3.ToPoint3d()));
                _lines.Add(new Line(point3.ToPoint3d(), point4.ToPoint3d()));
                _lines.Add(new Line(point4.ToPoint3d(), endPoint.ToPoint3d()));

                return endPoint.GetDistanceTo(pointStart);

            case var x when x >= 0.25 && x < 0.50:

                var distanceToPoint3Start = w / (2 * h) * distanceStartBreakPointToStartPoint;

                point3 = pointStart + (normalVector * distanceToPoint3Start);

                point4Start = pointStart + (normalVector * (distanceToPoint3Start + (0.25 * w)));

                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);

                var endPointx = pointStart + (normalVector * (distanceToPoint3Start + (0.50 * w)));

                _lines.Add(new Line(pointStartBreak.Item1.ToPoint3d(), point3.ToPoint3d()));
                _lines.Add(new Line(point3.ToPoint3d(), point4.ToPoint3d()));
                _lines.Add(new Line(point4.ToPoint3d(), endPointx.ToPoint3d()));

                return endPointx.GetDistanceTo(pointStart);

            case var x when x >= 0.50 && x < 0.75:

                double distanceToPoint4;
                if (normalVector.GetAngleTo(Vector2d.XAxis).RadianToDegree() <= 90)
                    distanceToPoint4 = (h / 2) - distanceStartBreakPointToStartPoint;
                else
                    distanceToPoint4 = (h / 2) + distanceStartBreakPointToStartPoint;

                var distanceToPoint4Start = w / (2 * h) * distanceToPoint4;
                point4Start = pointStart + (normalVector * distanceToPoint4Start);
                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);
                endPoint = pointStart + (normalVector * (distanceToPoint4Start + (w * 0.25)));

                _lines.Add(new Line(pointStartBreak.Item1.ToPoint3d(), point4.ToPoint3d()));
                _lines.Add(new Line(point4.ToPoint3d(), endPoint.ToPoint3d()));

                return endPoint.GetDistanceTo(pointStart);

            case var x when x >= 0.75:

                var distanceToEndPoint = w / (2 * h) * distanceStartBreakPointToStartPoint;
                endPoint = pointStart + (normalVector * distanceToEndPoint);

                _lines.Add(new Line(pointStartBreak.Item1.ToPoint3d(), endPoint.ToPoint3d()));

                return endPoint.GetDistanceTo(pointStart);

            default:
                return default;
        }
    }

    /// <summary>
    /// Отрисовка суффикса (неполного излома в конце сегмента)
    /// </summary>
    private (Point2d, double) DrawSuffixBreak(Point2d[] limitBlockPoints, double remnant,
        double scale, Vector2d breakNormalVector, Vector2d breakPerpendicularVector, Vector2d normalVector,
        Vector2d perpendicularVector)
    {

        var h = BreakHeight * scale;
        var w = BreakWidth * scale;

        var revativeRemant = remnant / BreakWidth / scale;

        var remnantPoint1 = limitBlockPoints.Last();

        // точка излома на 1/4 ширины излома от начала
        var remnantPoint2Start = remnantPoint1 + (breakNormalVector * 0.25);

        // точка излома на 1/4 ширины излома от начала, вверх на половину высоты
        var remnantPoint2 = remnantPoint2Start + (breakPerpendicularVector * 0.50);

        // точка на середине излома
        var remnantPoint3 = remnantPoint1 + (breakNormalVector * 0.50);

        // точка излома на 3/4 ширины излома от начала
        var remnantPoint4Start = remnantPoint1 + (breakNormalVector * 0.75);

        // точка излома на 3/4 ширины излома от начала, вниз на половину высоты
        var remnantPoint4 = remnantPoint4Start + (breakPerpendicularVector.Negate() * 0.50);

        var remnantEndPoint = remnantPoint1 + (normalVector * remnant);

        switch (revativeRemant)
        {
            case var x when x < 0.25:
                var point12Remnant = remnantEndPoint + (perpendicularVector * (2 * h * remnant / w));

                if (!EdgesVisible)
                {
                    _lines.Add(new Line(remnantPoint1.ToPoint3d(), point12Remnant.ToPoint3d()));
                    return (point12Remnant, revativeRemant);
                }
                else
                {
                    _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantEndPoint.ToPoint3d()));
                    return (remnantEndPoint, 0);
                }

            case var x when x >= 0.25 && x < 0.50:
                if (!EdgesVisible)
                {
                    _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));
                    var point23Remnant = remnantEndPoint + (perpendicularVector * 2 * h / w * ((w / 2) - remnant));
                    _lines.Add(new Line(remnantPoint2.ToPoint3d(), point23Remnant.ToPoint3d()));
                    return (point23Remnant, revativeRemant);
                }
                else
                {
                    _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantEndPoint.ToPoint3d()));
                    return (remnantEndPoint, 0);
                }

            case var x when x >= 0.50 && x < 0.75:

                _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));
                _lines.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint3.ToPoint3d()));
                if (!EdgesVisible)
                {
                    var point34Remnant = remnantEndPoint + (perpendicularVector.Negate() * (2 * h * (remnant - (w / 2)) / w));
                    _lines.Add(new Line(remnantPoint3.ToPoint3d(), point34Remnant.ToPoint3d()));
                    return (point34Remnant, revativeRemant);
                }
                else
                {
                    _lines.Add(new Line(remnantPoint3.ToPoint3d(), remnantEndPoint.ToPoint3d()));
                    return (remnantEndPoint, 0);
                }

            case var x when x >= 0.75:
                _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));

                if (!EdgesVisible)
                {
                    _lines.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint4.ToPoint3d()));
                    var point45Remnant = remnantEndPoint + (perpendicularVector.Negate() * 2 * h / w * ((w / 2) - (remnant - (w / 2))));
                    _lines.Add(new Line(remnantPoint4.ToPoint3d(), point45Remnant.ToPoint3d()));
                    return (point45Remnant, revativeRemant);
                }
                else
                {
                    _lines.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint3.ToPoint3d()));
                    _lines.Add(new Line(remnantPoint3.ToPoint3d(), remnantEndPoint.ToPoint3d()));
                    return (remnantEndPoint, 0);
                }

            default:
                return (default, default);
        }


    }

    private static Point2dCollection GetPointsForMainPolyline(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var points = new Point2dCollection
        {
            insertionPoint.ToPoint2d()
        };

        middlePoints.ForEach(p => points.Add(p.ToPoint2d()));
        points.Add(endPoint.ToPoint2d());

        return points;
    }

    #endregion
}