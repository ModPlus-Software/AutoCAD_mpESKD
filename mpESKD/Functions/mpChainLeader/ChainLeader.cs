namespace mpESKD.Functions.mpChainLeader;

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

/// <summary>
/// Цепная выноска
/// </summary>
[SmartEntityDisplayNameKey("h175")] // Цепная выноска 
[SystemStyleDescriptionKey("h176")] // Базовый стиль для обозначения цепной выноски
public class ChainLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;
    private readonly List<Hatch> _hatches = new ();
    private readonly List<Polyline> _leaderEndLines = new ();
    private double _scale;
    
    private Line _shelfLineFromEndPoint;

    #region Entities

    /// <summary>
    /// Линия выноски
    /// </summary>
    private Line _leaderLine;

    /// <summary>
    /// Полка выноски
    /// </summary>
    private Line _shelfLine;

    /// <summary>
    /// Верхний первый текст (номер узла)
    /// </summary>
    private DBText _topFirstDbText;

    /// <summary>
    /// Маскировка фона верхнего первого текста (номер узла)
    /// </summary>
    private Wipeout _topFirstTextMask;

    /// <summary>
    /// Верхний второй текст (номер листа)
    /// </summary>
    private DBText _topSecondDbText;

    /// <summary>
    /// Маскировка фона верхнего второго текста (номер листа)
    /// </summary>
    private Wipeout _topSecondTextMask;

    /// <summary>
    /// Нижний текст (адрес узла)
    /// </summary>
    private DBText _bottomDbText;

    /// <summary>
    /// Маскировка нижнего текста (адрес узла)
    /// </summary>
    private Wipeout _bottomTextMask;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeader"/> class.
    /// </summary>
    public ChainLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public ChainLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public ChainLeader(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public ChainLeaderJigState? JigState { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => ArrowSize + 1;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topFirstTextMask,
                _topSecondTextMask,
                _bottomTextMask,
                _leaderLine,
                _shelfLineFromEndPoint,
                _shelfLine,
                _topFirstDbText,
                _topSecondDbText,
                _bottomDbText,
            };

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
    /// Основной единичный вектор
    /// </summary>
    public Vector3d MainNormal { get; set; }

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p61", 1.0, 0.0, 10.0, nameSymbol: "o")]
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки 
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p63", 10, 0, 100, descLocalKey: "d63", nameSymbol: "l")]
    [SaveToXData]
    public double ShelfLedge { get; set; }

    /// <summary>
    /// Положение полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p78", ShelfPosition.Right)]
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Тип стрелки
    /// </summary> 
    [EntityProperty(PropertiesCategory.Geometry, 6, "gp7", LeaderEndType.Point)] //TODO
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
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 2.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
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
    /// Номер узла
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p79", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string NodeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Номер листа
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p80", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SheetNumber { get; set; } = string.Empty;

    /// <summary>
    /// Адрес узла
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p81", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string NodeAddress { get; set; } = string.Empty;

    /// <summary>
    /// Точки стрелок
    /// </summary>
    [SaveToXData]
    public List<double> ArrowPoints { get; set; } = new ();
    
    public double TempNewArrowPoint { get; set; } = double.NaN;

    [SaveToXData]
    public Point3d FirstPoint { get; set; }

    [SaveToXData]
    public Point3d SecondPoint { get; set; } = Point3d.Origin;

    [SaveToXData]
    public double ShelfLength { get; set; }

    public bool IsLeft { get; set; }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            _scale = GetScale();

            // Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == ChainLeaderJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale),
                    InsertionPointOCS.Y + (MinDistanceBetweenPoints * scale),
                    InsertionPointOCS.Z);

                CreateEntities(InsertionPointOCS, tempEndPoint, scale);
            }
            else
            {
                // Если конечная точка на расстоянии, менее допустимого
                if (EndPointOCS.DistanceTo(InsertionPointOCS) < MinDistanceBetweenPoints * scale)
                {
                    var v = (EndPointOCS - InsertionPointOCS).GetNormal();
                    var tempEndPoint = InsertionPointOCS + (MinDistanceBetweenPoints * scale * v);

                    CreateEntities(InsertionPointOCS, tempEndPoint, scale);
                }
                else
                {
                    // Прочие случаи
                    CreateEntities(InsertionPointOCS, EndPointOCS, scale);
                }
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint, Point3d endPoint, double scale)
    {
        _leaderEndLines.Clear();
        _hatches.Clear();

        var arrowSize = ArrowSize * scale;
        MainNormal = (endPoint - insertionPoint).GetNormal();

        var leaderMinPoint = insertionPoint + (MainNormal * arrowSize);
        if (leaderMinPoint.DistanceTo(endPoint) > 0.0)
            _leaderLine = new Line(insertionPoint, endPoint);
        
        if (!double.IsNaN(TempNewArrowPoint))
        {
            var tempPoint = endPoint + (MainNormal * TempNewArrowPoint);
            if (TempNewArrowPoint > 0)
            {
                FirstPoint = insertionPoint;
                SecondPoint = tempPoint;
                CreateArrows(tempPoint);
            }
            else
            {
                FirstPoint = tempPoint;
                SecondPoint = endPoint;
                CreateArrows(tempPoint);
            }
        }
        else if (ArrowPoints.Count > 0)
        {
            var tempPoints = new List<Point3d>
            {
                insertionPoint,
                endPoint
            };

            foreach (var arrowPoint in ArrowPoints)
            {
                tempPoints.Add(endPoint + (MainNormal * arrowPoint));
            }

            var furthestPoints = GetFurthestPoints(tempPoints);

            FirstPoint = furthestPoints.Item1;
            SecondPoint = furthestPoints.Item2;
        }
        else
        {
            // только первый запуск
            FirstPoint = insertionPoint;
            SecondPoint = endPoint;
        }

        _leaderLine = new Line(FirstPoint, SecondPoint);
        
        CreateArrows(insertionPoint);

        foreach (var arrowPoint in ArrowPoints)
        {
            var tempPoint1 = endPoint + (MainNormal * arrowPoint);

            CreateArrows(tempPoint1);
        }

        // Дальше код идентичен коду в NodalLeader! Учесть при внесении изменений

        SetNodeNumberOnCreation();

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textIndent = TextIndent * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var shelfLedge = ShelfLedge * scale;
        var isRight = ShelfPosition == ShelfPosition.Right;

        var topFirstTextLength = 0.0;
        var topSecondTextLength = 0.0;
        var bottomTextLength = 0.0;
        var bottomTextHeight = 0.0;

        if (!string.IsNullOrEmpty(NodeNumber))
        {
            _topFirstDbText = new DBText { TextString = NodeNumber };
            _topFirstDbText.SetProperties(TextStyle, mainTextHeight);
            _topFirstDbText.SetPosition(
                TextHorizontalMode.TextCenter, 
                TextVerticalMode.TextVerticalMid, 
                AttachmentPoint.MiddleCenter);
            topFirstTextLength = _topFirstDbText.GetLength();
        }
        else
        {
            _topFirstDbText = null;
        }

        if (!string.IsNullOrEmpty(SheetNumber))
        {
            _topSecondDbText = new DBText { TextString = $"({SheetNumber})" };
            _topSecondDbText.SetProperties(TextStyle, secondTextHeight);
            _topSecondDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
            topSecondTextLength = _topSecondDbText.GetLength();
        }
        else
        {
            _topSecondDbText = null;
        }

        if (!string.IsNullOrEmpty(NodeAddress))
        {
            _bottomDbText = new DBText { TextString = NodeAddress };
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

        var topTextLength = topFirstTextLength + topSecondTextLength;
        var largestTextLength = Math.Max(topTextLength, bottomTextLength);
        ShelfLength = textIndent + largestTextLength + shelfLedge;

        Point3d topFirstTextPosition;
        var topSecondTextPosition = default(Point3d);
        Point3d bottomTextPosition;

        if (isRight)
        {
            topFirstTextPosition = new Point3d(
                endPoint.X + (topFirstTextLength / 2) + ((ShelfLength - topTextLength) / 2),
                endPoint.Y + textVerticalOffset + (mainTextHeight / 2),
                0);
            bottomTextPosition = new Point3d(
                endPoint.X + (bottomTextLength / 2) + ((ShelfLength - bottomTextLength) / 2),
                endPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topFirstDbText != null)
            {
                _topFirstDbText.Position = topFirstTextPosition;
                _topFirstDbText.AlignmentPoint = topFirstTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }
        else
        {
            topFirstTextPosition = new Point3d(
                endPoint.X - (topFirstTextLength / 2) - topSecondTextLength - ((ShelfLength - topTextLength) / 2),
                endPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
            bottomTextPosition = new Point3d(
                endPoint.X - (bottomTextLength / 2) - ((ShelfLength - bottomTextLength) / 2),
                endPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

            if (_topFirstDbText != null)
            {
                _topFirstDbText.Position = topFirstTextPosition;
                _topFirstDbText.AlignmentPoint = topFirstTextPosition;
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }

        if (_topSecondDbText != null)
        {
            topSecondTextPosition = new Point3d(
                topFirstTextPosition.X + (topFirstTextLength / 2) + (topSecondTextLength / 2), topFirstTextPosition.Y, 0);
            _topSecondDbText.Position = topSecondTextPosition;
            _topSecondDbText.AlignmentPoint = topSecondTextPosition;
        }

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? endPoint + (Vector3d.XAxis * ShelfLength)
            : endPoint - (Vector3d.XAxis * ShelfLength);

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _topFirstTextMask = _topFirstDbText.GetBackgroundMask(offset, topFirstTextPosition);
            _topSecondTextMask = _topSecondDbText.GetBackgroundMask(offset, topSecondTextPosition);
            _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(endPoint);
            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topFirstDbText?.TransformBy(backRotationMatrix);
            _topFirstTextMask?.TransformBy(backRotationMatrix);
            _topSecondDbText?.TransformBy(backRotationMatrix);
            _topSecondTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        _shelfLineFromEndPoint = new Line(endPoint, shelfEndPoint);
    }

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        NodeNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }

    #region Arrows

    private void CreateArrows(Point3d point3d)
    {
        var pline = new Polyline();
        switch (ArrowType)
        {
            case LeaderEndType.None:
                break;
            case LeaderEndType.HalfArrow:
                _hatches.Add(CreateArrowHatch(CreateHalfArrow(point3d)));
                break;
            case LeaderEndType.Point:
                _hatches.Add(CreatePointHatch(CreatePointArrow(point3d)));
                break;
            case LeaderEndType.Section:
                pline = CreateResectionArrow(point3d, 0);
                break;
            case LeaderEndType.Resection:
                pline = CreateResectionArrow(point3d, 0.3);
                break;
            case LeaderEndType.Angle:
                pline = CreateAngleArrow(point3d, 45, false);
                break;
            case LeaderEndType.Arrow:
                _hatches.Add(CreateArrowHatch(CreateAngleArrow(point3d, 10, true)));
                break;
            case LeaderEndType.OpenArrow:
                pline = CreateAngleArrow(point3d, 10, false);
                break;
            case LeaderEndType.ClosedArrow:
                pline = CreateAngleArrow(point3d, 10, true);
                break;
        }

        _leaderEndLines.Add(pline);
    }

    private Polyline CreateResectionArrow(Point3d arrowPoint, double plineWidth)
    {
        var vector = new Vector3d(0, 0, 1);

        var tmpPoint = arrowPoint - (MainNormal * ArrowSize / 2 * _scale);
        var startPoint = tmpPoint.RotateBy(45.DegreeToRadian(), vector, arrowPoint);
        var endPoint = tmpPoint.RotateBy(225.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(2);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, plineWidth, plineWidth);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, plineWidth, plineWidth);

        return pline;
    }

    private Polyline CreateAngleArrow(Point3d arrowPoint, int angle, bool closed)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = arrowPoint + (MainNormal * ArrowSize * _scale);
        var startPoint = tmpPoint.RotateBy(angle.DegreeToRadian(), vector, arrowPoint);
        var endPoint = tmpPoint.RotateBy((-1) * angle.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);

        pline.Closed = closed;

        return pline;
    }

    private Polyline CreateHalfArrow(Point3d arrowPoint)
    {
        var vector = new Vector3d(0, 0, 1);
        var arrowEndPoint = arrowPoint + (MainNormal * ArrowSize * _scale);
        var endPoint = arrowEndPoint.RotateBy(10.DegreeToRadian(), vector, arrowPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(arrowEndPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);
        pline.Closed = true;

        return pline;
    }

    private Hatch CreateArrowHatch(Polyline pline)
    {
        var vertexCollection = new Point2dCollection();
        for (var index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        var bulgeCollection = new DoubleCollection()
        {
            0.0, 0.0, 0.0
        };

        return CreateHatch(vertexCollection, bulgeCollection);
    }

    private Polyline CreatePointArrow(Point3d arrowPoint)
    {
        var startPoint = arrowPoint - (ArrowSize / 4 * MainNormal * _scale);
        var endPoint = arrowPoint + (ArrowSize / 4 * MainNormal * _scale);

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 1, 0, 0);
        pline.Closed = true;

        return pline;
    }

    private Hatch CreatePointHatch(Polyline pline)
    {
        var vertexCollection = new Point2dCollection();
        for (var index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        var bulgeCollection = new DoubleCollection()
        {
            1.0, 1.0
        };

        return CreateHatch(vertexCollection, bulgeCollection);
    }

    private Hatch CreateHatch(Point2dCollection vertexCollection, DoubleCollection bulgeCollection)
    {
        var hatch = new Hatch();
        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        hatch.AppendLoop(HatchLoopTypes.Default, vertexCollection, bulgeCollection);

        return hatch;
    }

    #endregion

    /// <summary>
    /// Возвращает пару наиболее удаленных друг от друга точек
    /// </summary>
    /// <param name="points">Коллекция точек</param>
    /// <returns></returns>
    public static Tuple<Point3d, Point3d> GetFurthestPoints(IList<Point3d> points)
    {
        Tuple<Point3d, Point3d> result = default;
        var dist = double.NaN;
        for (int i = 0; i < points.Count; i++)
        {
            var pt1 = points[i];
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j)
                    continue;
                var pt2 = points[j];
                var d = pt1.DistanceTo(pt2);
                if (double.IsNaN(dist) || d > dist)
                {
                    result = new Tuple<Point3d, Point3d>(pt1, pt2);
                    dist = d;
                }
            }
        }

        return result;
    }
}