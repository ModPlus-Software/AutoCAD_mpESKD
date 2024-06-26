﻿// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpGroundLine;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;

/// <summary>
/// Линия грунта
/// </summary>
[SmartEntityDisplayNameKey("h73")]
[SystemStyleDescriptionKey("h78")]
public class GroundLine : SmartLinearEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroundLine"/> class.
    /// </summary>
    public GroundLine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroundLine"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public GroundLine(ObjectId objectId) 
        : base(objectId)
    {
    }
        
    #region Properties

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 2.0;

    /// <summary>
    /// Отступ первого штриха в каждом сегменте полилинии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p36", GroundLineFirstStrokeOffset.ByHalfSpace, descLocalKey: "d36", nameSymbol: "a")]
    [SaveToXData]
    public GroundLineFirstStrokeOffset FirstStrokeOffset { get; set; } = GroundLineFirstStrokeOffset.ByHalfSpace;

    /// <summary>
    /// Длина штриха
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p37", 8, 1, 10, nameSymbol: "l")]
    [SaveToXData]
    public int StrokeLength { get; set; } = 8;

    /// <summary>
    /// Расстояние между штрихами
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p38", 4, 1, 10, nameSymbol: "b")]
    [SaveToXData]
    public int StrokeOffset { get; set; } = 4;

    /// <summary>
    /// Угол наклона штриха в градусах
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p39", 60, 30, 90, nameSymbol: "α")]
    [SaveToXData]
    public int StrokeAngle { get; set; } = 60;

    /// <summary>
    /// Отступ группы штрихов
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p40", 10, 1, 20, nameSymbol: "c")]
    [SaveToXData]
    public int Space { get; set; } = 10;

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

    /// <inheritdoc />
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
        
    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var middlePoint in MiddlePoints)
        {
            yield return middlePoint;
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (20 * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                CreateEntities(tmpEndPoint, scale);
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(null, scale);
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }
    
    private void CreateEntities(Point3d? endPoint, double scale)
    {
        var points = GetOcsAll3dPointsForDraw(endPoint).Select(p => p.ToPoint2d()).ToList();
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
                // При "легком" создании обрабатываем только последний сегмент
                if (IsLightCreation && i < _mainPolyline.NumberOfVertices - 1)
                    continue;

                var previousPoint = _mainPolyline.GetPoint3dAt(i - 1);
                var currentPoint = _mainPolyline.GetPoint3dAt(i);
                _strokes.AddRange(CreateStrokesOnMainPolylineSegment(currentPoint, previousPoint, scale));
            }
        }
    }

    private IEnumerable<Line> CreateStrokesOnMainPolylineSegment(
        Point3d currentPoint, Point3d previousPoint, double scale)
    {
        var segmentStrokeDependencies = new List<Line>();

        var segmentVector = currentPoint - previousPoint;
        var segmentLength = segmentVector.Length;
        var perpendicular = segmentVector.GetPerpendicularVector().Negate();
        var distanceAtSegmentStart = _mainPolyline.GetDistAtPoint(previousPoint);

        var overflowIndex = 0;

        // Индекс штриха. Возможные значения - 0, 1, 2
        var strokeIndex = 0;
        var sumDistanceAtSegment = 0.0;
        while (true)
        {
            overflowIndex++;
            var distance = 0.0;
            if (Math.Abs(sumDistanceAtSegment) < 0.0001)
            {
                if (FirstStrokeOffset == GroundLineFirstStrokeOffset.ByHalfSpace)
                {
                    distance = Space / 2.0 * scale;
                }
                else if (FirstStrokeOffset == GroundLineFirstStrokeOffset.BySpace)
                {
                    distance = Space * scale;
                }
                else
                {
                    distance = StrokeOffset * scale;
                }
            }
            else
            {
                if (strokeIndex == 0)
                {
                    distance = Space * scale;
                }

                if (strokeIndex == 1 || strokeIndex == 2)
                {
                    distance = StrokeOffset * scale;
                }
            }

            if (strokeIndex == 2)
            {
                strokeIndex = 0;
            }
            else
            {
                strokeIndex++;
            }

            sumDistanceAtSegment += distance;

            if (sumDistanceAtSegment >= segmentLength)
            {
                break;
            }

            var firstStrokePoint = _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment);
            var helpPoint =
                firstStrokePoint + (segmentVector.Negate().GetNormal() * StrokeLength * scale * Math.Cos(StrokeAngle.DegreeToRadian()));
            var secondStrokePoint =
                helpPoint + (perpendicular * StrokeLength * scale * Math.Sin(StrokeAngle.DegreeToRadian()));
            var stroke = new Line(firstStrokePoint, secondStrokePoint);
            SetImmutablePropertiesToNestedEntity(stroke);

            // индекс сегмента равен "левой" вершине
            segmentStrokeDependencies.Add(stroke);

            if (overflowIndex >= 1000)
            {
                break;
            }
        }

        return segmentStrokeDependencies;
    }

    #endregion
}