namespace mpESKD.Functions.mpConcreteJoint;

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
    public override double MinDistanceBetweenPoints => 20.0;

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
    [EntityProperty(PropertiesCategory.Geometry, 1, "p116", 5, 1, 20, nameSymbol: "w")]
    [SaveToXData]
    public double BreakWidth { get; set; } = 5;

    /// <summary>
    /// Высота излома
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p116", 5, 1, 20, nameSymbol: "h")]
    [SaveToXData]
    public double BreakHeight { get; set; } = 5;

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
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);

            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                //_logger.WriteToLogFile($"UpdateEntities: 132");
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                //_logger.WriteToLogFile($"UpdateEntities: 134");
            }
            else if (length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0)
            {
                // Задание второй точки - случай когда расстояние между точками меньше минимального
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                InsertionPointOCS, EndPointOCS, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                //_logger.WriteToLogFile($"UpdateEntities: 135");
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                // _logger.WriteToLogFile($"UpdateEntities: 136");
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
                //_logger.WriteToLogFile($"UpdateEntities: 137");
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS, scale);
                //_logger.WriteToLogFile($"UpdateEntities: 151");
            }
        }
        catch (System.Exception exception)
        {
            //_logger.WriteToLogFile($"UpdateEntities: {exception.Message}");
            ExceptionBox.Show(exception);
        }
    }




    private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
    {
        _lines.Clear();

        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);

        // для каждой пары точек из points
        for (var i = 0; i < points.Count; i++)
        {
            if (i + 1 < points.Count)
                CreateBreakBlocksBetween2Points(points[i], points[i + 1], scale);
        }

        foreach (var e in _lines)
        {
            SetChangeablePropertiesToNestedEntity(e);
        }
    }

    // рисую нужное количество изломов между 2 точками
    private void CreateBreakBlocksBetween2Points(Point2d point1, Point2d point2, double scale)
    {
        // длина отрезка между точками
        var length = point1.GetDistanceTo(point2);

        var normalVector = (point2 - point1).GetNormal();
        var perpendicular = normalVector.GetPerpendicularVector();

        // количество целых изломов
        int breakBlocksCount = (int)(length / BreakWidth / scale);
        //_logger.WriteToLogFile($"CreateBreakBlocksBetween2Points: 189");

        if (breakBlocksCount > 0)
        {
            //_logger.WriteToLogFile($"CreateBreakBlocksBetween2Points: 193");
            // массив граничных точек изломов
            Point2d[] limitBlockPoints = new Point2d[breakBlocksCount + 1];
            for (int i = 0; i < limitBlockPoints.Count(); i++)
            {
                limitBlockPoints[i] = i == 0 ? point1 : point1 + (normalVector * BreakWidth * i * scale);
            }

            //_logger.WriteToLogFile($"CreateBreakBlocksBetween2Points: 201");

            for (int i = 0; i < limitBlockPoints.Count() - 1; i++)
            {

                // точка на 1/4 отрезка излома
                var point12start = limitBlockPoints[i] + (normalVector * BreakWidth * scale / 4);

                // отложить от нее перпендикуляр на расст h/2
                var point12 = point12start + (perpendicular * BreakHeight * scale / 2);

                // точка на 3/4 отрезка излома
                var point13start = limitBlockPoints[i] + (normalVector * BreakWidth * scale * 3 / 4);

                // отложить от нее перпендикуляр на расст -h/2
                var point13 = point13start + (perpendicular.Negate() * BreakHeight * scale / 2);

                _lines.Add(new Line(limitBlockPoints[i].ToPoint3d(), point12.ToPoint3d()));
                _lines.Add(new Line(point12.ToPoint3d(), point13.ToPoint3d()));
                _lines.Add(new Line(point13.ToPoint3d(), limitBlockPoints[i + 1].ToPoint3d()));
            }

            //_logger.WriteToLogFile($"CreateBreakBlocksBetween2Points: 224");

            // хвостик, которого не хватило на полный излом
            double remnant = length - (breakBlocksCount * BreakWidth * scale); //length % (BreakWidth * scale);

            if (remnant != 0)
            {
                if (remnant < BreakWidth * scale / 2)
                {
                    _lines.Add(new Line(limitBlockPoints.Last().ToPoint3d(), point2.ToPoint3d()));
                }

                if (remnant >= BreakWidth * scale / 2)
                {
                    // точка на 1/4 отрезка излома
                    var point12start = limitBlockPoints.Last() + (normalVector * BreakWidth * scale / 4);

                    // отложить от нее перпендикуляр на расст h/2
                    var point12 = point12start + (perpendicular * BreakHeight * scale / 2);

                    var midPoint= limitBlockPoints.Last() + (normalVector * BreakWidth * scale / 2);

                    _lines.Add(new Line(limitBlockPoints.Last().ToPoint3d(), point12.ToPoint3d()));
                    _lines.Add(new Line(point12.ToPoint3d(), midPoint.ToPoint3d()));
                    _lines.Add(new Line(midPoint.ToPoint3d(),point2.ToPoint3d()));
                }
            }

            //_logger.WriteToLogFile($"CreateBreakBlocksBetween2Points: 231");
        }
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