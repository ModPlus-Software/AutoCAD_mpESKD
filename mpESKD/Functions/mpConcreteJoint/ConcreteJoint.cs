namespace mpESKD.Functions.mpConcreteJoint;

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using mpESKD.Functions.mpGroundLine;

/// <summary>
/// Шов бетонирования
/// </summary>
[SmartEntityDisplayNameKey("h73")]
[SystemStyleDescriptionKey("h78")]
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
    #endregion






    #region Geometry

    /// <summary>
    /// Главная полилиния примитива
    /// </summary>
    private Polyline _mainPolyline;

    /// <summary>
    /// Список штрихов
    /// </summary>
    private readonly List<Line> _strokes = new List<Line>();



    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>();
            entities.AddRange(_strokes);
            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            SetChangeablePropertiesToNestedEntity(_mainPolyline);

            entities.Add(_mainPolyline);

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
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
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
        
        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);
        _mainPolyline = new Polyline(points.Count);
        SetImmutablePropertiesToNestedEntity(_mainPolyline);
        for (var i = 0; i < points.Count; i++)
        {
            _mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
        }
        /*
        // create strokes
        _strokes.Clear();
        if (_mainPolyline.Length >= MinDistanceBetweenPoints * scale)
        {
            for (var i = 1; i < _mainPolyline.NumberOfVertices; i++)
            {
                var previousPoint = _mainPolyline.GetPoint3dAt(i - 1);
                var currentPoint = _mainPolyline.GetPoint3dAt(i);
                //_strokes.AddRange(CreateStrokesOnMainPolylineSegment(currentPoint, previousPoint, scale));
                _strokes.AddRange(new List<Line> {
                    new Line(currentPoint, previousPoint)
                });
            }
        }*/

        /*
        _mainPolyline= new Polyline();

        _mainPolyline.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0.0, 0.0);
        _mainPolyline.AddVertexAt(1, endPoint.ToPoint2d(), 0.0, 0.0, 0.0);
        */

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