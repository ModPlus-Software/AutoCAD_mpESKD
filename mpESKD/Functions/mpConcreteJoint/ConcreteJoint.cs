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

    //private Logger _logger = new Logger(@"g:\Prog\AutoCAD API\Projects\ModPlus\ConcreteJoint\ConcreteJoint.log");




    #region Geometry

    /// <summary>
    /// Главная полилиния примитива
    /// </summary>
    //private Polyline _mainPolyline;

    /// <summary>
    /// Список штрихов
    /// </summary>
   // private readonly List<Line> _strokes = new List<Line>();

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
        //_logger.WriteToLogFile("UpdateEntities! start");
        try
        {
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);
            //_logger.WriteToLogFile($"UpdateEntities! length: {length}");

            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                /* var tmpEndPoint = new Point3d(
                InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                 CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);*/
            }
             else if (length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0)
             {
                 // Задание второй точки - случай когда расстояние между точками меньше минимального
                 var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                 InsertionPointOCS, EndPointOCS, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                 CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                 EndPoint = tmpEndPoint.TransformBy(BlockTransform);
             }
            else
            {
                // Задание любой другой точки
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS, scale);
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }




    private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
    {

        _lines.Clear();

        // _logger.WriteToLogFile($"CreateEntities! start");
        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);

        //int ii = 1;
        //foreach (var point in points)
        //{

        //    _logger.WriteToLogFile($"CreateEntities! point {ii}: {point}; ");
        //    ii++;
        //}

        //_mainPolyline = new Polyline(points.Count);

        // SetImmutablePropertiesToNestedEntity(_mainPolyline);

        // для каждой пары точек из points
        for (var i = 0; i < points.Count; i++)
        {
            //_mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
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

        // количество целых изломов
        int breakBlocksCount = (int)(length / BreakWidth / scale);

        // хвостик
        double remnant = length % BreakWidth;

        var normalVector = (point2 - point1).GetNormal();
        // var secondArrowPoint = insertionPoint + (normalVector * arrowLength);

        // массив крайних точек изломов
        Point2d[] limitBlockPoints = new Point2d[breakBlocksCount];
        for (int i = 0; i < breakBlocksCount; i++)
        {
            limitBlockPoints[i] = point1 + (normalVector * BreakWidth * (i + 1) * scale);
        }

        // угол поворота координат - против часовой от напр. оси Х
        var angleAxis = Vector2d.XAxis.GetAngleTo(normalVector); //.RadianToDegree();

       // _logger.WriteToLogFile($"CreateBreakBlocksBetween2Points! angleAxis: {angleAxis}");

        
        for (int i = 0; i < breakBlocksCount; i++)
        {
            if (i + 1 < breakBlocksCount)
            {
                var perpendicular = normalVector.GetPerpendicularVector();

                // точка на 1/4 отрезка излома
                var point12start = limitBlockPoints[i] + (normalVector * BreakWidth * scale / 4);
                // отложить от нее перпендикуляр на расст h/2
                var point12 = point12start + (perpendicular * BreakHeight * scale / 2);

                var point13start = limitBlockPoints[i] + (normalVector * BreakWidth * scale * 3 / 4);
                var point13 = point13start + (perpendicular.Negate() * BreakHeight * scale / 2);
                //var point12 = new Point2d(
                //    (limitBlockPoints[i].X + (BreakWidth * scale / 4)) * Math.Cos(angleAxis),
                //    (limitBlockPoints[i].Y + (BreakHeight * scale / 2)) * Math.Sin(angleAxis));


                /*
                var point13 = new Point2d(
                   (limitBlockPoints[i].X + (3 * BreakWidth * scale / 4)) * Math.Cos(angleAxis),
                   (limitBlockPoints[i].Y - (BreakHeight * scale / 2)) * Math.Sin(angleAxis));*/

                _lines.Add(new Line(limitBlockPoints[i].ToPoint3d(), point12.ToPoint3d()));
                _lines.Add(new Line(point12.ToPoint3d(), point13.ToPoint3d()));
                _lines.Add(new Line(point13.ToPoint3d(), limitBlockPoints[i + 1].ToPoint3d()));
            }
        }


        // кусочек, которого не хватило на полный пик, рисую линией



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