namespace mpESKD.Functions.mpConcreteJoint;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using System;
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
    [EntityProperty(PropertiesCategory.Geometry, 1, "p116", 4, 1, 20, nameSymbol: "h")]
    [SaveToXData]
    public double BreakHeight { get; set; } = 4;

    #endregion

    //Logger _logger = new Logger(@"g:\Prog\AutoCAD API\Projects\ModPlus\ConcreteJoint\ConcreteJoint.log");

    #region Geometry

    /// <summary>
    /// Коллекция линий-изломов
    /// </summary>
    private List<Line> _lines = new List<Line>();

    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();
            entities.AddRange(_lines);
            foreach (var e in entities)
            {
                //SetImmutablePropertiesToNestedEntity(e);
                SetChangeablePropertiesToNestedEntity(e);
            }

            //SetChangeablePropertiesToNestedEntity(_mainPolyline);

            //entities.Add(_mainPolyline);

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

        (Point2d, double) pointStartBreak = (insertionPoint.ToPoint2d(), 0.0); // points[0];

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


    // рисую нужное количество изломов между 2 точками
    private (Point2d, double) CreateBreakBlocksBetween2Points(Point2d point1, Point2d point2, (Point2d, double) pointStartBreak, double scale)
    {
        var result = (point2, 0.0);

        var normalVector = (point2 - point1).GetNormal();
        var perpendicularVector = normalVector.GetPerpendicularVector();

        // вектор длиной, равной ширине излома, в направлении линии шва
        var breakVector = normalVector * BreakWidth * scale;

        // вектор длиной, равной высоте излома, перпендикулярный линии шва
        var breakVectorPerpendicular = perpendicularVector * BreakHeight * scale;

        Point2d pointStart = default(Point2d);

        // Если первая точка на средней линии шва совпадает с последней точкой предыдущего участка
        if (!point1.Equals(pointStartBreak.Item1))
        {
            // точка на средней линии, на кот. опускается перпендикуляр от точки pointStartBreak
            // =point1, если предыдущий отрезок закончился в нуле, т.е. на средней линии шва
            pointStart = GetIntersectBetweenVectors(pointStartBreak.Item1, perpendicularVector, point1, normalVector);

            // если точка неопределена, т.е. нет наклона текущего участка по отношению к предыдущему
            if (pointStart == default)
            {
                pointStart = point1;
                AcadUtils.WriteMessageInDebug($"X, Y -> IsNaN");
            }
        }
        else
        {
            pointStart = point1;
        }

        //pointStart = point1; // !!!!

        // длина отрезка между точками
        var length = pointStart.GetDistanceTo(point2);

        // количество целых изломов
        int breakBlocksCount = (int)(length / BreakWidth / scale);

        if (breakBlocksCount > 0)
        {
            // AcadUtils.WriteMessageInDebug($"\nbreakBlocksCount > 0");

            // массив граничных точек изломов
            Point2d[] limitBlockPoints = new Point2d[breakBlocksCount + 1];
            for (int i = 0; i < limitBlockPoints.Count(); i++)
            {
                limitBlockPoints[i] = i == 0 ? pointStart : pointStart + (normalVector * BreakWidth * i * scale);
            }

            for (int i = 0; i < limitBlockPoints.Count() - 1; i++)
            {
                // AcadUtils.WriteMessageInDebug($"\nin for {i}");
                // точка на 1/4 отрезка излома
                var point12start = limitBlockPoints[i] + (breakVector * 0.25);

                // отложить от нее перпендикуляр на расст h/2
                var point12 = point12start + (breakVectorPerpendicular * 0.50);

                // точка на 3/4 отрезка излома
                var point13start = limitBlockPoints[i] + (breakVector * 0.75);

                // отложить от нее перпендикуляр на расст -h/2
                var point13 = point13start + (breakVectorPerpendicular.Negate() * 0.50); //(perpendicularVector.Negate() * BreakHeight * scale / 2);

                _lines.Add(new Line(limitBlockPoints[i].ToPoint3d(), point12.ToPoint3d()));
                _lines.Add(new Line(point12.ToPoint3d(), point13.ToPoint3d()));
                _lines.Add(new Line(point13.ToPoint3d(), limitBlockPoints[i + 1].ToPoint3d()));
            }

            // хвостик, которого не хватило на полный излом
            double remnant = length - (breakBlocksCount * BreakWidth * scale);
            var revativeRemant = remnant / BreakWidth / scale;

            if (remnant > 0)
            {

                // граничные точки  хвостика 
                // первая точка
                var remnantPoint1 = limitBlockPoints.Last();

                // точка излома на 1/4 ширины изома от начала
                var remnantPoint12 = remnantPoint1 + (breakVector * 0.25);

                // точка излома на 1/4 ширины изома от начала, вверх на половину высоты
                var remnantPoint2 = remnantPoint12 + (breakVectorPerpendicular * 0.50);

                // точка на середине излома
                var remnantPoint3 = remnantPoint1 + (breakVector * 0.50);

                // точка излома на 3/4 ширины изома от начала
                var remnantPoint4Start = remnantPoint1 + (breakVector * 0.75);

                // точка излома на 3/4 ширины изома от начала, вниз на половину высоты
                var remnantPoint4 = remnantPoint4Start + (breakVectorPerpendicular.Negate() * 0.50);

                var remnantEndPoint = remnantPoint1 + (normalVector * remnant);

                var h = BreakHeight * scale;
                var w = BreakWidth * scale;

                switch (revativeRemant)
                {
                    case var x when x < 0.25:
                        // рисуем линию в диапазоне от точки 1 до точки 2
                        var point12Remnant = remnantEndPoint + (perpendicularVector * (2 * h * remnant / w));
                        _lines.Add(new Line(remnantPoint1.ToPoint3d(), point12Remnant.ToPoint3d()));
                        result = (point12Remnant, revativeRemant);
                        break;
                    case var x when x >= 0.25 && x < 0.50:
                        // рисуем сразу линию от 1 до 2
                        _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));
                        // затем рисуем остаток
                        var point23Remnant = remnantEndPoint + (perpendicularVector * 2 * h / w * ((w / 2) - remnant));
                        _lines.Add(new Line(remnantPoint2.ToPoint3d(), point23Remnant.ToPoint3d()));
                        result = (point23Remnant, revativeRemant);
                        break;
                    case var x when x >= 0.50 && x < 0.75:
                        // рисуем сразу линию от 1 до 2
                        _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));
                        // рисуем сразу линию от 2 до 3 (середина)
                        _lines.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint3.ToPoint3d()));
                        // затем рисуем остаток
                        var point34Remnant = remnantEndPoint + (perpendicularVector.Negate() * (2 * h * (remnant - (w / 2)) / w));
                        _lines.Add(new Line(remnantPoint3.ToPoint3d(), point34Remnant.ToPoint3d()));
                        result = (point34Remnant, revativeRemant);
                        break;
                    case var x when x >= 0.75:
                        // рисуем сразу линию от 1 до 2
                        _lines.Add(new Line(remnantPoint1.ToPoint3d(), remnantPoint2.ToPoint3d()));
                        // рисуем сразу линию от 2 до 4
                        _lines.Add(new Line(remnantPoint2.ToPoint3d(), remnantPoint4.ToPoint3d()));
                        // затем рисуем остаток
                        var point45Remnant = remnantEndPoint + (perpendicularVector.Negate() * 2 * h / w * ((w / 2) - (remnant - (w / 2))));
                        _lines.Add(new Line(remnantPoint4.ToPoint3d(), point45Remnant.ToPoint3d()));
                        result = (point45Remnant, revativeRemant);
                        break;
                    default:
                        break;
                }
            }
        }

        return result;
    }



    private static Point2dCollection GetPointsForMainPolyline(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var points = new Point2dCollection();

        points.Add(insertionPoint.ToPoint2d());
        middlePoints.ForEach(p => points.Add(p.ToPoint2d()));
        points.Add(endPoint.ToPoint2d());

        return points;
    }

    #endregion
}