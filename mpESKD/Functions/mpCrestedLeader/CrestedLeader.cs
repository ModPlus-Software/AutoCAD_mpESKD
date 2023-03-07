﻿namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Цепная выноска Todo
/// </summary>
[SmartEntityDisplayNameKey("h175")] // Цепная выноска Todo
[SystemStyleDescriptionKey("h176")] // Базовый стиль для обозначения цепной выноски Todo
public class CrestedLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;
    private readonly List<Hatch> _hatches = new();
    private readonly List<Polyline> _leaderEndLines = new();
    private double _scale;

    /// <summary>
    /// нормаль выноски
    /// </summary>
    private Vector3d _mainNormal;
    private Line _shelfLineFromEndPoint;
    private readonly List<Line> _leaderLines = new();

    #region Entities

    /// <summary>
    /// Линия выноски
    /// </summary>
    private Line _leaderLine;

    /// <summary>
    /// Значение
    /// </summary>
    private DBText _topDbText;

    /// <summary>
    /// Маскировка фона значения
    /// </summary>
    private Wipeout _topTextMask;

    /// <summary>
    /// Примечание
    /// </summary>
    private DBText _bottomDbText;

    /// <summary>
    /// Маскировка фона примечания
    /// </summary>
    private Wipeout _bottomTextMask;

    private Line _leaderMainLine;
    
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    public CrestedLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public CrestedLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public CrestedLeader(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public CrestedLeaderJigState? JigState { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => ArrowSize + 1;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topTextMask,
                _bottomTextMask,
                _leaderLine,
                _shelfLineFromEndPoint,
                _topDbText,
                _bottomDbText,
                _leaderMainLine
            };
            entities.AddRange(_leaderLines);
            entities.AddRange(_hatches);
            entities.AddRange(_leaderEndLines);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p62", 1.0, 0.0, 3.0, nameSymbol: "h2")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки 
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p63", 0, 0, 100, descLocalKey: "d63", nameSymbol: "l")]
    [SaveToXData]
    public double ShelfLedge { get; set; }

    /// <summary>
    /// Свойство определяющая сторону выноски
    /// </summary>
    public bool IsRight
    {
        get
        {
            if (EndPointOCS.IsEqualTo(InsertionPointOCS))
            {
                return true;
            }

            return (EndPointOCS - InsertionPointOCS).GetNormal().X > 0;
        }
    }

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Тип стрелки
    /// </summary> 
    [EntityProperty(PropertiesCategory.Geometry, 6, "gp7", LeaderEndType.Point)]
    [SaveToXData]
    public LeaderEndType ArrowType { get; set; } = LeaderEndType.Point;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота малого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 2.5, 0.000000001, 1.0000E+99)]
    [SaveToXData]
    public double SecondTextHeight { get; set; } = 2.5;

    /// <summary>
    /// Текст всегда горизонтально
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 4, "p84", false, descLocalKey: "d84")]
    [SaveToXData]
    public bool IsTextAlwaysHorizontal { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 6, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

    /// <summary>
    /// Значение
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p113", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string LeaderTextValue { get; set; } = string.Empty;

    /// <summary>
    /// Примечание
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p68", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string LeaderTextComment { get; set; } = string.Empty;

    /// <summary>
    /// Выравнивание текста по горизонтали
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 10, "p73", TextHorizontalAlignment.Left, descLocalKey: "d73")]
    [SaveToXData]
    public TextHorizontalAlignment ValueHorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    /// <summary>
    /// Точки стрелок
    /// </summary>
    [SaveToXData]
    public List<Point3d> ArrowPoints { get; set; } = new();

    private List<Point3d> ArrowPointsOCS => ArrowPoints.Select(p => p.TransformBy(BlockTransform.Inverse())).ToList();

    /// <summary>
    /// Расстояние от Endpoint для отображения при горизонтальном растягивании
    /// </summary>
    public Point3d TempNewArrowPoint { get; set; } = new Point3d(Double.NaN, Double.NaN, Double.NaN);

    private Point3d TempNewArrowPointOCS => TempNewArrowPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Расстояние от Endpoint для отображения при вертикальном растягивании
    /// </summary>
    public Point3d TempNewStretchPoint { get; set; } = new Point3d(Double.NaN, Double.NaN, Double.NaN);

    private Point3d TempNewStretchPointOCS => TempNewStretchPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Длина полки
    /// </summary>
    [SaveToXData]
    public double ShelfLength { get; set; }

    /// <summary>
    /// Первая точка для первой стрелки, для определения нормали
    /// </summary>
    [SaveToXData]
    public Point3d FirstArrowFirstPoint { get; set; }

    /// <summary>
    /// Вторая точка для первой стрелки, для определения нормали
    /// </summary>
    [SaveToXData]
    public Point3d FirstArrowSecondPoint { get; set; }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var arrowPoint in ArrowPoints)
        {
            yield return arrowPoint;
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            _scale = GetScale();

            var length = EndPointOCS.DistanceTo(InsertionPointOCS);
            _mainNormal = new Vector3d(1, 0, 0);
            if (!IsRight)
            {
                _mainNormal = new Vector3d(-1, 0, 0);
            }

            // Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == CrestedLeaderJigState.InsertionPoint)
            {
                var arrows = new List<Point3d>()
            {
                InsertionPointOCS
            };

                var leaderStart = new Point3d(InsertionPointOCS.X, InsertionPointOCS.Y, 0);

                var tempEndPoint = new Point3d(
                                InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale),
                                InsertionPointOCS.Y + (MinDistanceBetweenPoints * _scale),
                                InsertionPointOCS.Z);

                CreateEntities(InsertionPointOCS, tempEndPoint, arrows, _scale);
                var tempNormal = (tempEndPoint - leaderStart).GetNormal();
                CreateArrows(InsertionPointOCS, tempNormal, ArrowSize, _scale);
            }
            else if (JigState == CrestedLeaderJigState.LeaderStart & InsertionPointOCS.Y != EndPointOCS.Y)
            {
                FirstArrowFirstPoint = InsertionPointOCS;
                FirstArrowSecondPoint = EndPointOCS;

                var arrows = new List<Point3d> { InsertionPointOCS };

                var leaderStart = InsertionPointOCS;

                if (length < MinDistanceBetweenPoints * _scale)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    if (ArrowPoints.Count == 0)
                    {
                        if (arrows[0].DistanceTo(leaderStart) < MinDistanceBetweenPoints)
                        {
                            leaderStart = arrows[0];
                        }
                    }

                    var tempEndPoint = InsertionPointOCS + _mainNormal * MinDistanceBetweenPoints * _scale;
                    CreateEntities(leaderStart, tempEndPoint, arrows, _scale);
                }
                else
                {
                    CreateEntities(leaderStart, EndPointOCS, arrows, _scale);
                }
            }
            else
            {
                var tempEndPoint = new Point3d(EndPointOCS.X, InsertionPointOCS.Y, 0);
                if (InsertionPointOCS.DistanceTo(EndPointOCS) < MinDistanceBetweenPoints * _scale | InsertionPointOCS.X.Equals(EndPointOCS.X))
                {
                    tempEndPoint = InsertionPointOCS + _mainNormal * MinDistanceBetweenPoints * _scale;
                }

                CreateEntities(InsertionPointOCS, tempEndPoint, ArrowPointsOCS, _scale);
            }

        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d leaderStart, Point3d leaderEnd, List<Point3d> arrows, double scale)
    {
        _leaderLines.Clear();
        _leaderEndLines.Clear();
        _hatches.Clear();

        var leaderNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();

        var leaderEndPoint = leaderStart + Math.Abs(leaderEnd.X - leaderStart.X) * _mainNormal;

        _leaderMainLine = new Line(leaderStart, leaderEnd);

        var leaderMinPoint = leaderStart + (leaderNormal * MinDistanceBetweenPoints * _scale);

        // отрисовка джиги при горизонтальном изменении
        if (!double.IsNaN(TempNewArrowPointOCS.X))
        {
            var pointOnPolyline = CreateLeadersWithArrows(TempNewArrowPointOCS);

            var distToEndPoint = pointOnPolyline.DistanceTo(leaderStart);
            var distToLeaderPoint = pointOnPolyline.DistanceTo(leaderEnd);
            if (IsRight)
            {
                if (distToLeaderPoint < distToEndPoint)
                {
                    if (pointOnPolyline.X > leaderEnd.X)
                        leaderEnd = pointOnPolyline;
                }
                else
                {
                    leaderStart = pointOnPolyline;
                }
            }
            else
            {
                if (distToLeaderPoint > distToEndPoint)
                {
                    leaderStart = pointOnPolyline;
                }
                else
                {
                    if (pointOnPolyline.X < leaderEnd.X)
                        leaderEnd = pointOnPolyline;
                }
            }

            _leaderMainLine = new Line(leaderStart, leaderEnd);
        }

        // отрисовка джиги при вертикальном смешении полки
        else if (!double.IsNaN(TempNewStretchPointOCS.X) & double.IsNaN(TempNewArrowPointOCS.X))
        {
            var tempMainLine = new Line(TempNewStretchPointOCS, new Point3d(TempNewStretchPointOCS.X + 1, TempNewStretchPointOCS.Y, 0));
            var distFromEndPointToInsPoint = Math.Abs(EndPointOCS.X - InsertionPointOCS.X);
            var firstPoint = ArrowPointsOCS[0];
            leaderStart = GetPointOnPolyline(firstPoint, tempMainLine, leaderNormal);
            leaderEnd = leaderStart + distFromEndPointToInsPoint * _mainNormal;
            if (!IsRight)
            {
                leaderEnd = new Point3d(leaderStart.X - distFromEndPointToInsPoint, TempNewStretchPointOCS.Y, 0);
            }

            _leaderMainLine = new Line(leaderStart, leaderEnd);
        }

        // первое построение прямых первая точка
        if (ArrowPoints.Count == 0 & double.IsNaN(TempNewArrowPointOCS.X))
        {
            if (leaderStart.DistanceTo(leaderEnd) < MinDistanceBetweenPoints)
            {
                leaderEnd = leaderMinPoint;
            }

            _leaderLine = new Line(leaderStart, leaderMinPoint);
            _leaderMainLine = new Line(leaderStart, leaderEnd);

            CreateArrows(leaderStart, _mainNormal, ArrowSize, _scale);
        }

        // второе построение прямых первая точка
        else if (ArrowPoints.Count == 1 & double.IsNaN(TempNewArrowPointOCS.X))
        {
            _leaderLine = new Line(arrows[0], leaderStart);
            var normal = (_leaderLine.EndPoint - _leaderLine.StartPoint).GetNormal();
            CreateArrows(arrows[0], normal, ArrowSize, _scale);
        }
        else
        {
            foreach (var arrowPoint in ArrowPointsOCS)
            {
                var templine = new Line(arrowPoint, arrowPoint + leaderNormal);
                var pts = new Point3dCollection();

                _leaderMainLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count > 0)
                {
                    templine = new Line(arrowPoint, pts[0]);
                    _leaderLines.Add(templine);
                    var normal = (templine.EndPoint - templine.StartPoint).GetNormal();
                    if (normal == default)
                    {
                        normal = _mainNormal;
                    }

                    CreateArrows(arrowPoint, normal, ArrowSize, _scale);
                }
            }
        }

        // Дальше код идентичен коду в NodalLeader! Учесть при внесении изменений

        #region TextCreation

        SetNodeNumberOnCreation();

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var shelfLedge = ShelfLedge * scale;

        var topTextLength = 0.0;
        var bottomTextLength = 0.0;
        var bottomTextHeight = 0.0;

        if (!string.IsNullOrEmpty(LeaderTextValue))
        {
            _topDbText = new DBText { TextString = LeaderTextValue };
            _topDbText.SetProperties(TextStyle, mainTextHeight);
            _topDbText.SetPosition(
                TextHorizontalMode.TextCenter,
                TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            topTextLength = _topDbText.GetLength();
        }
        else
        {
            _topDbText = null;
        }

        if (!string.IsNullOrEmpty(LeaderTextComment))
        {
            _bottomDbText = new DBText { TextString = LeaderTextComment };
            _bottomDbText.SetProperties(TextStyle, secondTextHeight);
            _bottomDbText.SetPosition(
                TextHorizontalMode.TextCenter,
                TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            bottomTextLength = _bottomDbText.GetLength();
            bottomTextHeight = _bottomDbText.GetHeight();
        }
        else
        {
            _bottomDbText = null;
        }

        var largestTextLength = Math.Max(topTextLength, bottomTextLength);
        ShelfLength = largestTextLength + shelfLedge;

        Point3d topTextPosition;
        Point3d bottomTextPosition;

        if (IsRight)
        {
            topTextPosition = new Point3d(
                leaderEnd.X + (largestTextLength / 2),
                leaderEnd.Y + textVerticalOffset + (mainTextHeight / 2),
                0);
            bottomTextPosition = new Point3d(
                leaderEnd.X + (largestTextLength / 2),
                leaderEnd.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topDbText != null)
            {
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }
        else
        {
            topTextPosition = new Point3d(
                leaderEnd.X - (largestTextLength / 2),
                leaderEnd.Y + textVerticalOffset + (mainTextHeight / 2), 0);
            bottomTextPosition = new Point3d(
                leaderEnd.X - (largestTextLength / 2),
                leaderEnd.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topDbText != null)
            {
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }

        var shelfEndPoint = IsRight
            ? leaderEnd + (Vector3d.XAxis * ShelfLength)
            : leaderEnd - (Vector3d.XAxis * ShelfLength);

        if (_bottomDbText != null && _topDbText != null)
        {
            var horV = (shelfEndPoint - leaderEnd).GetNormal();
            var diff = Math.Abs(topTextLength - bottomTextLength);
            var textHalfMovementHorV = diff / 2 * horV;
            var movingPosition = EntityUtils.GetMovementPositionVector(ValueHorizontalAlignment, IsRight, textHalfMovementHorV, ScaleFactorX);
            if (topTextLength > bottomTextLength)
            {
                bottomTextPosition += movingPosition;
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
            else
            {
                topTextPosition += movingPosition;
                _topDbText.Position = topTextPosition;
                _topDbText.AlignmentPoint = topTextPosition;
            }
        }

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            if (_topDbText != null)
                _topTextMask = _topDbText.GetBackgroundMask(offset, topTextPosition);
            if (_bottomDbText != null)
                _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(leaderStart);
            if (ScaleFactorX < 0)
            {
                backRotationMatrix = GetBackMirroredRotationMatrix(leaderStart);
            }

            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topDbText?.TransformBy(backRotationMatrix);
            _topTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        _shelfLineFromEndPoint = new Line(leaderEnd, shelfEndPoint);

        MirrorIfNeed(new[] { _topDbText, _bottomDbText });
        #endregion
    }

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        LeaderTextValue = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }

    private void CreateArrows(Point3d point3d, Vector3d mainNormal, double arrowSize, double scale)
    {
        new ArrowBuilder(mainNormal, arrowSize, scale).BuildArrow(ArrowType, point3d, _hatches, _leaderEndLines);
    }

    private Point3d CreateLeadersWithArrows(Point3d arrowPoint)
    {
        var secondNormal = (FirstArrowSecondPoint - FirstArrowFirstPoint).GetNormal();
        var templine = new Line(arrowPoint, arrowPoint + secondNormal);
        var pts = new Point3dCollection();

        _leaderMainLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
        if (pts.Count > 0)
        {
            templine = new Line(arrowPoint, pts[0]);
        }

        _leaderLines.Add(templine);
        var tempNormal = (pts[0] - arrowPoint).GetNormal();
        CreateArrows(arrowPoint, tempNormal, ArrowSize, _scale);
        return pts[0];
    }

    private Point3d GetPointOnPolyline(Point3d point, Line line, Vector3d mainNormal)
    {
        var templine = new Line(point, point + mainNormal);
        var pts = new Point3dCollection();

        line.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
        var pointOnPolyline = new Point3d();

        if (pts.Count > 0)
        {
            pointOnPolyline = pts[0];
        }

        return pointOnPolyline;

    }
}