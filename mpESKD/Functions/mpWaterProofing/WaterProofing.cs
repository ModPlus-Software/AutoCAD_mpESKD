// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpWaterProofing;

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
/// Линия гидроизоляции
/// </summary>
[SmartEntityDisplayNameKey("h114")]
[SystemStyleDescriptionKey("h129")]
public class WaterProofing : SmartLinearEntity
{
    #region Entities

    /// <summary>
    /// Главная полилиния объекта
    /// </summary>
    private Polyline _mainPolyline;

    /// <summary>
    /// Вторая полилиния, являющаяся смещенной копией первой
    /// </summary>
    private readonly List<Entity> _offsetPolylineEntities = new List<Entity>();

    /// <summary>
    /// Список штрихов
    /// </summary>
    private readonly List<Polyline> _strokes = new List<Polyline>();

    #endregion
        
    /// <summary>
    /// Initializes a new instance of the <see cref="WaterProofing"/> class.
    /// </summary>
    public WaterProofing()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WaterProofing"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public WaterProofing(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 2.0;

    /// <summary>
    /// Отступ первого штриха в каждом сегменте полилинии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p36", WaterProofingFirstStrokeOffset.ByHalfStrokeOffset, descLocalKey: "d36-1", nameSymbol: "a")]
    [SaveToXData]
    public WaterProofingFirstStrokeOffset FirstStrokeOffset { get; set; } = WaterProofingFirstStrokeOffset.ByHalfStrokeOffset;

    /// <summary>
    /// Длина штриха
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p37", 8, 1, 20, nameSymbol: "l")]
    [SaveToXData]
    public int StrokeLength { get; set; } = 8;

    /// <summary>
    /// Расстояние между штрихами
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p38", 6, 1, 20, nameSymbol: "b")]
    [SaveToXData]
    public int StrokeOffset { get; set; } = 6;

    /// <summary>
    /// Толщина линии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p70", 2.0, 0.0, 10, nameSymbol: "t")]
    [SaveToXData]
    public double LineThickness { get; set; } = 2.0;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 4, "p35", "Continuous", descLocalKey: "d35")]
    public override string LineType { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.General, 5, "p6", 1.0, 0.0, 1.0000E+99, descLocalKey: "d6")]
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    /// Не используется!
    public override string TextStyle { get; set; }

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

            foreach (var offsetPolylineEntity in _offsetPolylineEntities)
            {
                SetChangeablePropertiesToNestedEntity(offsetPolylineEntity);
                entities.Add(offsetPolylineEntity);
            }

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
                    InsertionPointOCS.X + (10 * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
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
        for (var i = 0; i < points.Count; i++)
        {
            _mainPolyline.AddVertexAt(i, points[i], 0.0, 0.0, 0.0);
        }

        _offsetPolylineEntities.Clear();
        foreach (Entity offsetCurve in _mainPolyline.GetOffsetCurves(LineThickness * scale))
        {
            _offsetPolylineEntities.Add(offsetCurve);
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

                var segmentStartPoint = _mainPolyline.GetPoint3dAt(i - 1);
                var segmentEndPoint = _mainPolyline.GetPoint3dAt(i);
                Vector3d? previousSegmentVector = null;
                Vector3d? nextSegmentVector = null;
                if (i > 1)
                    previousSegmentVector = segmentStartPoint - _mainPolyline.GetPoint3dAt(i - 2);
                if (i < _mainPolyline.NumberOfVertices - 1)
                    nextSegmentVector = _mainPolyline.GetPoint3dAt(i + 1) - segmentEndPoint;

                _strokes.AddRange(CreateStrokesOnMainPolylineSegment(
                    segmentEndPoint, segmentStartPoint, scale, previousSegmentVector, nextSegmentVector));
            }
        }
    }

    private IEnumerable<Polyline> CreateStrokesOnMainPolylineSegment(
        Point3d segmentEndPoint, Point3d segmentStartPoint, double scale, Vector3d? previousSegmentVector, Vector3d? nextSegmentVector)
    {
        var strokes = new List<Polyline>();

        var lineThickness = LineThickness * scale;
        var segmentVector = segmentEndPoint - segmentStartPoint;

        var previousToCurrentCrossProductIndex = 1.0;
        if (previousSegmentVector != null)
        {
            previousToCurrentCrossProductIndex =
                previousSegmentVector.Value.CrossProduct(segmentVector).GetNormal().Z;
        }

        var currentToNextCrossProductIndex = 1.0;
        if (nextSegmentVector != null)
        {
            currentToNextCrossProductIndex = segmentVector.CrossProduct(nextSegmentVector.Value).GetNormal().Z;
        }
            
        var angleToPreviousSegment = previousSegmentVector != null
            ? previousSegmentVector.Value.GetAngleTo(segmentVector)
            : 0.0;
        var startBackOffset = 0.0;
        if (previousToCurrentCrossProductIndex < 0 && angleToPreviousSegment > 0.0)
        {
            startBackOffset = Math.Abs(lineThickness * Math.Tan(Math.PI - (angleToPreviousSegment / 2.0)));
        }

        var angleToNextSegment = nextSegmentVector != null
            ? segmentVector.GetAngleTo(nextSegmentVector.Value)
            : 0.0;
        var endBackOffset = 0.0;
        if (currentToNextCrossProductIndex < 0 && angleToNextSegment > 0.0)
        {
            endBackOffset = Math.Abs(lineThickness * Math.Tan(Math.PI - (angleToNextSegment / 2.0)));
        }

        var segmentLength = segmentVector.Length;
        var perpendicular = segmentVector.GetPerpendicularVector().Negate();
        var distanceAtSegmentStart = _mainPolyline.GetDistAtPoint(segmentStartPoint);

        var overflowIndex = 0;

        var sumDistanceAtSegment = 0.0;
        var isSpace = true;
        var isStart = true;
        while (true)
        {
            overflowIndex++;
            double distance;
            if (isStart)
            {
                if (FirstStrokeOffset == WaterProofingFirstStrokeOffset.ByHalfStrokeOffset)
                {
                    distance = StrokeOffset / 2.0 * scale;
                }
                else if (FirstStrokeOffset == WaterProofingFirstStrokeOffset.ByStrokeOffset)
                {
                    distance = StrokeOffset * scale;
                }
                else
                {
                    distance = 0.0;
                }

                distance += startBackOffset;

                isStart = false;
            }
            else
            {
                if (isSpace)
                {
                    distance = StrokeOffset * scale;
                }
                else
                {
                    distance = StrokeLength * scale;
                }
            }

            sumDistanceAtSegment += distance;

            if (!isSpace)
            {
                var firstStrokePoint = _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment - distance) +
                                       (perpendicular * lineThickness / 2.0);

                if ((sumDistanceAtSegment - distance) < (sumDistanceAtSegment - endBackOffset))
                {
                    // Если индекс, полученный из суммы векторов (текущий и следующий) отрицательный и последний штрих 
                    // попадает на конец сегмента полилинии, то его нужно построить так, чтобы он попал на точку не основной
                    // полилинии, а второстепенной

                    Point3d secondStrokePoint;
                    if (sumDistanceAtSegment >= segmentLength)
                    {
                        AcadUtils.WriteMessageInDebug($"segment vector: {segmentVector.GetNormal()}");
                        secondStrokePoint = 
                            segmentEndPoint - 
                            (segmentVector.GetNormal() * endBackOffset) +
                            (perpendicular * lineThickness / 2.0);
                        AcadUtils.WriteMessageInDebug($"{nameof(secondStrokePoint)}: {secondStrokePoint}");
                    }
                    else
                    {
                        secondStrokePoint =
                            _mainPolyline.GetPointAtDist(distanceAtSegmentStart + sumDistanceAtSegment) +
                            (perpendicular * lineThickness / 2.0);
                    }

                    var stroke = new Polyline(2);
                    stroke.AddVertexAt(0, firstStrokePoint.ToPoint2d(), 0.0, lineThickness,
                        lineThickness);
                    stroke.AddVertexAt(1, secondStrokePoint.ToPoint2d(), 0.0, lineThickness,
                        lineThickness);

                    SetImmutablePropertiesToNestedEntity(stroke);

                    strokes.Add(stroke);
                }
            }

            if (sumDistanceAtSegment >= segmentLength)
            {
                break;
            }

            if (overflowIndex >= 1000)
            {
                break;
            }

            isSpace = !isSpace;
        }

        return strokes;
    }
}