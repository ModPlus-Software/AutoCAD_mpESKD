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
    /// Коллекция сегментов
    /// </summary>
    private readonly List<ConcreteJointLineSegment> _segments = new ();

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
    /// Ширина излома линии шва
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p116", 6, 1, 20, nameSymbol: "w")]
    [SaveToXData]
    public double BreakWidth { get; set; } = 6;

    /// <summary>
    /// Высота излома линии шва
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p119", 4, 1, 20, nameSymbol: "h")]
    [SaveToXData]
    public double BreakHeight { get; set; } = 4;

    /// <summary>
    /// Видимость линии по краям сегментов
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p118", false, descLocalKey: "p118")]
    [SaveToXData]
    public bool EdgeLineVisible { get; set; } = false;

    /// <summary>
    /// Длина линии на левом краю сегмента шва
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p117", 1, 0.5, 30, nameSymbol: "e")]
    [SaveToXData]
    public double EdgeLineWidth { get; set; } = 1;

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();

            foreach (var segment in _segments)
            {
                entities.AddRange(segment.Lines);
            }

            foreach (var e in entities)
            {
                SetChangeablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var middlePoint in MiddlePoints)
        {
            yield return middlePoint;
        }
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                InsertionPointOCS.X + (15 * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
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
        _segments.Clear();
        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);

        for (var i = 0; i < points.Count; i++)
        {
            if (i < points.Count - 1)
            {
                if (IsLightCreation && i < points.Count - 2)
                {
                    _segments.Add(new ConcreteJointLineSegment(
                        new List<Line> { new (points[i].ToPoint3d(), points[i + 1].ToPoint3d()) },
                        points[i + 1],
                        0));

                    continue;
                }

                if (i == 0)
                {
                    _segments.Add(CreateSegment(points[i], points[i + 1],
                        insertionPoint.ToPoint2d(), 0, scale));
                }
                else
                {
                    _segments.Add(CreateSegment(points[i], points[i + 1],
                        _segments[i - 1].EndPointLineBreak, _segments[i - 1].RemnantAtEnd, scale));
                }
            }
        }

        foreach (var segment in _segments)
        {
            foreach (var line in segment.Lines)
            {
                SetChangeablePropertiesToNestedEntity(line);
            }
        }
    }

    /// <summary>
    /// Отрисовка изломов между крайними точками сегмента
    /// </summary>
    /// <param name="point1">начальная точка сегмента</param>
    /// <param name="point2">конечная точка сегмента</param>
    /// <param name="pointPrevEndBreak">точка обрыва излома в конце предыдущего сегмента</param>
    /// <param name="prevRemnant">длина неполного излома в конце предыдущего сегмента</param>
    /// <returns>Сегмент линии шва <see cref="ConcreteJointLineSegment"/></returns>
    private ConcreteJointLineSegment CreateSegment(Point2d point1, Point2d point2,
        Point2d pointPrevEndBreak, double prevRemnant, double scale)
    {
        ConcreteJointLineSegment result = new (new List<Line>(), Point2d.Origin, 0d);

        var normalVector = (point2 - point1).GetNormal();
        var perpendicularVector = normalVector.GetPerpendicularVector();

        // вектор длиной, равной ширине излома, в направлении линии шва
        var breakNormalVector = normalVector * BreakWidth * scale;

        // вектор длиной, равной высоте излома, перпендикулярный линии шва
        var breakPerpendicularVector = perpendicularVector * BreakHeight * scale;

        // точка на средней линии, на кот. опускается перпендикуляр от точки pointPrevEndBreak
        Point2d pointStart;

        // Если первая точка на оси шва не совпадает с последней точкой излома предыдущего участка
        if (!point1.Equals(pointPrevEndBreak))
        {
            pointStart = GetIntersectBetweenVectors(pointPrevEndBreak, perpendicularVector, point1, normalVector);

            // если нет наклона текущего участка по отношению к предыдущему
            if (pointStart == default)
            {
                pointStart = point1;
            }

            if (!EdgeLineVisible)
            {
                // отрисовка излома-префикса и определение точки начала отрисовки основного блока изломов
                var prefixBreak = GetPrefixBreak(pointStart, pointPrevEndBreak, prevRemnant, scale, normalVector,
                    breakPerpendicularVector);
                var prefixBreakLines = prefixBreak.Item1;
                var prefixBreakLenght = prefixBreak.Item2;

                pointStart += normalVector * prefixBreakLenght;

                result.Lines.AddRange(prefixBreakLines);
            }
            else
            {
                pointStart = point1 + (normalVector * EdgeLineWidth * scale);

                // Рисуется префикс как линия вдоль оси шва
                result.Lines.Add(new Line(point1.ToPoint3d(), pointStart.ToPoint3d()));
            }
        }
        else
        {
            if (!EdgeLineVisible)
            {
                pointStart = point1;
            }
            else
            {
                pointStart = point1 + (normalVector * EdgeLineWidth * scale);
                result.Lines.Add(new Line(point1.ToPoint3d(), pointStart.ToPoint3d()));
            }
        }

        var length = pointStart.GetDistanceTo(point2) - (EdgeLineVisible ? (EdgeLineWidth * scale) : 0);

        // количество целых изломов
        var breakBlocksCount = (int)(length / BreakWidth / scale);

        if (breakBlocksCount > 0)
        {
            // массив граничных точек изломов
            var limitBlockPoints = new Point2d[breakBlocksCount + 1];
            for (int i = 0; i < limitBlockPoints.Count(); i++)
            {
                limitBlockPoints[i] = i == 0 ? pointStart : pointStart + (normalVector * BreakWidth * i * scale);
            }

            for (var i = 0; i < limitBlockPoints.Count() - 1; i++)
            {
                // точка на 1/4 отрезка излома
                var point12Start = limitBlockPoints[i] + (breakNormalVector * 0.25);

                // отложить от нее перпендикуляр на расстоянии h/2
                var point12 = point12Start + (breakPerpendicularVector * 0.50);

                // точка на 3/4 отрезка излома
                var point14Start = limitBlockPoints[i] + (breakNormalVector * 0.75);

                // отложить от нее перпендикуляр на расстоянии -h/2
                var point14 = point14Start + (breakPerpendicularVector.Negate() * 0.50);

                result.Lines.Add(new Line(limitBlockPoints[i].ToPoint3d(), point12.ToPoint3d()));
                result.Lines.Add(new Line(point12.ToPoint3d(), point14.ToPoint3d()));
                result.Lines.Add(new Line(point14.ToPoint3d(), limitBlockPoints[i + 1].ToPoint3d()));
            }

            // длина неполного излома в конце сегмента
            var remnant = length - (breakBlocksCount * BreakWidth * scale);

            if (remnant > 0)
            {
                result.RemnantAtEnd = remnant;

                var suffixBreak = DrawSuffixBreak(limitBlockPoints, remnant, scale, breakNormalVector,
                    breakPerpendicularVector, normalVector, perpendicularVector);

                var suffixBreakLines = suffixBreak.Item1;
                var suffixBreakEndPoint = suffixBreak.Item2;

                result.Lines.AddRange(suffixBreakLines);
                result.EndPointLineBreak = suffixBreakEndPoint;

                if (EdgeLineVisible)
                {
                    result.Lines.Add(new Line(suffixBreakEndPoint.ToPoint3d(), point2.ToPoint3d()));
                }
            }
            else if (EdgeLineVisible)
            {
                result.Lines.Add(new Line(limitBlockPoints.Last().ToPoint3d(), point2.ToPoint3d()));
            }
        }
        else
        {
            if (EdgeLineVisible)
            {
                result.Lines.Add(new Line(pointStart.ToPoint3d(), point2.ToPoint3d()));
            }
        }

        return result;
    }

    /// <summary>
    /// Возвращает префикс (неполный излом в начале сегмента)
    /// </summary>
    /// <returns>Кортеж (список_линий_префикса, длина_префикса)</returns>
    private (List<Line>, double) GetPrefixBreak(Point2d pointStart, Point2d pointStartBreak, double prevRemnant,
        double scale, Vector2d normalVector, Vector2d breakPerpendicularVector)
    {
        var h = BreakHeight * scale;
        var w = BreakWidth * scale;

        var revativePrevRemnant = prevRemnant / BreakWidth / scale;

        var distanceStartBreakPointToStartPoint = pointStartBreak.GetDistanceTo(pointStart);

        // точка на середине излома
        Point2d point3;

        // точка излома на 3/4 ширины излома от начала
        Point2d point4Start;

        // точка излома на 3/4 ширины излома от начала, вниз на половину высоты
        Point2d point4;

        Point2d endPoint;

        switch (revativePrevRemnant)
        {
            case < 0.25:

                double distanceToPoint2;
                if (normalVector.GetAngleTo(Vector2d.XAxis).RadianToDegree() <= 90)
                    distanceToPoint2 = (h / 2) - distanceStartBreakPointToStartPoint;
                else
                    distanceToPoint2 = (h / 2) + distanceStartBreakPointToStartPoint;

                var distanceToPoint2Start = w / (2 * h) * distanceToPoint2;

                // точка излома на 1/4 ширины излома от начала
                var point2Start = pointStartBreak + (normalVector * distanceToPoint2Start);

                // точка излома на 1/4 ширины излома от начала, вверх на половину высоты
                var point2 = point2Start + (normalVector.GetPerpendicularVector() * distanceToPoint2);

                point3 = pointStart + (normalVector * (distanceToPoint2Start + (0.25 * w)));
                point4Start = pointStart + (normalVector * (distanceToPoint2Start + (0.50 * w)));
                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);
                endPoint = pointStart + (normalVector * (distanceToPoint2Start + (0.75 * w)));

                return (new List<Line>
                    {
                        new (pointStartBreak.ToPoint3d(), point2.ToPoint3d()),
                        new (point2.ToPoint3d(), point3.ToPoint3d()),
                        new (point3.ToPoint3d(), point4.ToPoint3d()),
                        new (point4.ToPoint3d(), endPoint.ToPoint3d()),
                    },
                    endPoint.GetDistanceTo(pointStart));

            case >= 0.25 and < 0.50:
                var distanceToPoint3Start = w / (2 * h) * distanceStartBreakPointToStartPoint;

                point3 = pointStart + (normalVector * distanceToPoint3Start);
                point4Start = pointStart + (normalVector * (distanceToPoint3Start + (0.25 * w)));
                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);
                endPoint = pointStart + (normalVector * (distanceToPoint3Start + (0.50 * w)));

                return (new List<Line>
                    {
                        new (pointStartBreak.ToPoint3d(), point3.ToPoint3d()),
                        new (point3.ToPoint3d(), point4.ToPoint3d()),
                        new (point4.ToPoint3d(), endPoint.ToPoint3d()),
                    },
                    endPoint.GetDistanceTo(pointStart));

            case >= 0.50 and < 0.75:
                double distanceToPoint4;
                if (normalVector.GetAngleTo(Vector2d.XAxis).RadianToDegree() <= 90)
                    distanceToPoint4 = (h / 2) - distanceStartBreakPointToStartPoint;
                else
                    distanceToPoint4 = (h / 2) + distanceStartBreakPointToStartPoint;

                var distanceToPoint4Start = w / (2 * h) * distanceToPoint4;
                point4Start = pointStart + (normalVector * distanceToPoint4Start);
                point4 = point4Start + (breakPerpendicularVector.Negate() * 0.50);
                endPoint = pointStart + (normalVector * (distanceToPoint4Start + (w * 0.25)));

                return (new List<Line>
                    {
                        new (pointStartBreak.ToPoint3d(), point4.ToPoint3d()),
                        new (point4.ToPoint3d(), endPoint.ToPoint3d()),
                    },
                    endPoint.GetDistanceTo(pointStart));

            case >= 0.75:
                var distanceToEndPoint = w / (2 * h) * distanceStartBreakPointToStartPoint;
                endPoint = pointStart + (normalVector * distanceToEndPoint);

                return (new List<Line>
                    {
                        new (pointStartBreak.ToPoint3d(), endPoint.ToPoint3d()),
                    },
                    endPoint.GetDistanceTo(pointStart));

            default:
                return default;
        }
    }

    /// <summary>
    /// Отрисовка суффикса (неполного излома в конце сегмента)
    /// </summary>
    private (List<Line>, Point2d) DrawSuffixBreak(Point2d[] limitBlockPoints, double remnant,
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
            case < 0.25:
                var point12Remnant = remnantEndPoint + (perpendicularVector * (2 * h * remnant / w));

                if (!EdgeLineVisible)
                {
                    return (new List<Line>
                        {
                            new (remnantPoint1.ToPoint3d(), point12Remnant.ToPoint3d()),
                        },
                        point12Remnant);
                }
                else
                {
                    return (new List<Line>
                        {
                            new (remnantPoint1.ToPoint3d(), remnantEndPoint.ToPoint3d()),
                        },
                        remnantEndPoint);
                }

            case >= 0.25 and < 0.50:
                if (!EdgeLineVisible)
                {
                    var point23Remnant = remnantEndPoint + (perpendicularVector * 2 * h / w * ((w / 2) - remnant));

                    return (new List<Line>
                        {
                            new (remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()),
                            new (remnantPoint2.ToPoint3d(), point23Remnant.ToPoint3d()),
                        },
                        point23Remnant);
                }
                else
                {
                    return (new List<Line>
                        {
                            new (remnantPoint1.ToPoint3d(), remnantEndPoint.ToPoint3d()),
                        },
                        remnantEndPoint);
                }

            case >= 0.50 and < 0.75:
                var linesNext50 = new List<Line>
                {
                    new (remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()),
                    new (remnantPoint2.ToPoint3d(), remnantPoint3.ToPoint3d()),
                };

                if (!EdgeLineVisible)
                {
                    var point34Remnant = remnantEndPoint +
                                         (perpendicularVector.Negate() * (2 * h * (remnant - (w / 2)) / w));

                    linesNext50.Add(new (remnantPoint3.ToPoint3d(), point34Remnant.ToPoint3d()));

                    return (linesNext50, point34Remnant);
                }
                else
                {
                    linesNext50.Add(new (remnantPoint3.ToPoint3d(), remnantEndPoint.ToPoint3d()));

                    return (linesNext50, remnantEndPoint);
                }

            case >= 0.75:
                var linesNext75 = new List<Line>
                {
                    new (remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()),
                };

                if (!EdgeLineVisible)
                {
                    var point45Remnant = remnantEndPoint +
                                         (perpendicularVector.Negate() * 2 * h / w * ((w / 2) - (remnant - (w / 2))));

                    linesNext75.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint4.ToPoint3d()));
                    linesNext75.Add(new Line(remnantPoint4.ToPoint3d(), point45Remnant.ToPoint3d()));

                    return (linesNext75, point45Remnant);
                }
                else
                {
                    linesNext75.Add(new (remnantPoint2.ToPoint3d(), remnantPoint3.ToPoint3d()));
                    linesNext75.Add(new (remnantPoint3.ToPoint3d(), remnantEndPoint.ToPoint3d()));

                    return (linesNext75, remnantEndPoint);
                }

            default:
                return (default, default);
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

    private static Point2dCollection GetPointsForMainPolyline(Point3d insertionPoint, List<Point3d> middlePoints,
        Point3d endPoint)
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
}