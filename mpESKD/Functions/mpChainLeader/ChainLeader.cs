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
    private readonly List<Hatch> _hatches = new();
    private readonly List<Polyline> _leaderEndLines = new();
    private double _scale;
    private Vector3d _mainNormal;
    private Line _shelfLineFromEndPoint;

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
    public override double MinDistanceBetweenPoints => 1;

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
    /// Отступ текста
    /// </summary>
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

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
    /// Точки стрелок
    /// </summary>
    [SaveToXData]
    public List<double> ArrowPoints { get; set; } = new();

    /// <summary>
    /// Расстояние от Endpoint для отображения при растягивании
    /// </summary>
    public double TempNewArrowPoint { get; set; } = double.NaN;

    /// <summary>
    /// Первая точка основной полилинии
    /// </summary>
    [SaveToXData]
    public Point3d FirstPoint { get; set; }

    /// <summary>
    /// Вторая точка основной полилинии
    /// </summary>
    [SaveToXData]
    public Point3d SecondPoint { get; set; } = Point3d.Origin;

    /// <summary>
    /// Длина полки
    /// </summary>
    [SaveToXData]
    public double ShelfLength { get; set; }

    /// <summary>
    /// Свойство определяющая сторону выноски
    /// </summary>
    public bool IsLeft { get; set; }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        foreach (var arrowPoint in ArrowPoints)
        {
            yield return EndPoint + (EndPoint - InsertionPoint).GetNormal() * arrowPoint;
        }
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            _scale = GetScale();

            // Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == ChainLeaderJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * _scale),
                    InsertionPointOCS.Y + (MinDistanceBetweenPoints * _scale),
                    InsertionPointOCS.Z);

                CreateEntities(InsertionPointOCS, tempEndPoint, _scale);
            }
            else
            {
                // Если конечная точка на расстоянии, менее допустимого
                if (EndPointOCS.DistanceTo(InsertionPointOCS) < MinDistanceBetweenPoints * _scale)
                {
                    var v = (EndPointOCS - InsertionPointOCS).GetNormal();
                    var tempEndPoint = InsertionPointOCS + (MinDistanceBetweenPoints * _scale * v);

                    CreateEntities(InsertionPointOCS, tempEndPoint, _scale);
                }
                else
                {
                    // Прочие случаи
                    CreateEntities(InsertionPointOCS, EndPointOCS, _scale);
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
        _mainNormal = (endPoint - insertionPoint).GetNormal();

        var leaderMinPoint = insertionPoint + (_mainNormal * arrowSize);
        if (leaderMinPoint.DistanceTo(endPoint) > 0.0)
            _leaderLine = new Line(insertionPoint, endPoint);

        if (!double.IsNaN(TempNewArrowPoint))
        {
            var tempPoint = endPoint + (_mainNormal * TempNewArrowPoint);
            if (TempNewArrowPoint > 0)
            {
                FirstPoint = insertionPoint;
                SecondPoint = tempPoint;
            }
            else
            {
                FirstPoint = tempPoint;
                SecondPoint = endPoint;
            }

            CreateArrows(tempPoint, _mainNormal, ArrowSize, _scale);
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
                tempPoints.Add(endPoint + (_mainNormal * arrowPoint));
            }

            var furthestPoints = tempPoints.GetFurthestPoints();

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

        CreateArrows(insertionPoint, _mainNormal, ArrowSize, _scale);

        foreach (var arrowPoint in ArrowPoints)
        {
            var tempPoint1 = endPoint + (_mainNormal * arrowPoint);

            CreateArrows(tempPoint1, _mainNormal, ArrowSize, _scale);
        }

        // Дальше код идентичен коду в NodalLeader! Учесть при внесении изменений

        SetNodeNumberOnCreation();

        var mainTextHeight = MainTextHeight * scale;
        var secondTextHeight = SecondTextHeight * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var shelfLedge = ShelfLedge * scale;
        var isRight = ShelfPosition == ShelfPosition.Right;

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
        ShelfLength = TextIndent + largestTextLength + shelfLedge;

        Point3d topTextPosition;
        Point3d bottomTextPosition;

        if (isRight)
        {
            topTextPosition = new Point3d(
                endPoint.X + TextIndent + largestTextLength / 2,
                endPoint.Y + textVerticalOffset + (mainTextHeight / 2),
                0);
            bottomTextPosition = new Point3d(
                endPoint.X + TextIndent + largestTextLength / 2,
                endPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

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
                endPoint.X - TextIndent - largestTextLength / 2,
                endPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
            bottomTextPosition = new Point3d(
                endPoint.X - TextIndent - largestTextLength / 2,
                endPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

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

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? endPoint + (Vector3d.XAxis * ShelfLength)
            : endPoint - (Vector3d.XAxis * ShelfLength);

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _topTextMask = _topDbText.GetBackgroundMask(offset, topTextPosition);
            _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(endPoint);
            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topDbText?.TransformBy(backRotationMatrix);
            _topTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        _shelfLineFromEndPoint = new Line(endPoint, shelfEndPoint);

        MirrorIfNeed(new[] { _topDbText, _bottomDbText });
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
}