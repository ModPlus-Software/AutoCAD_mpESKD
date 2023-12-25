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
    public override string LineType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override double LineTypeScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override string TextStyle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
        /*
        var points = GetPointsForMainPolyline(insertionPoint, middlePoints, endPoint);
        _mainPolyline = new Polyline(points.Count);
        SetImmutablePropertiesToNestedEntity(_mainPolyline);
        for (var i = 0; i < points.Count; i++)
        {
            _mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
        }

        // create strokes
        _strokes.Clear();
        if (_mainPolyline.Length >= MinDistanceBetweenPoints * scale)
        {
            for (var i = 1; i < _mainPolyline.NumberOfVertices; i++)
            {
                var previousPoint = _mainPolyline.GetPoint3dAt(i - 1);
                var currentPoint = _mainPolyline.GetPoint3dAt(i);
                _strokes.AddRange(CreateStrokesOnMainPolylineSegment(currentPoint, previousPoint, scale));
            }
        }*/

        _mainPolyline= new Polyline();

        _mainPolyline.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0.0, 0.0);
        _mainPolyline.AddVertexAt(1, endPoint.ToPoint2d(), 0.0, 0.0, 0.0);


    }



    #endregion
}