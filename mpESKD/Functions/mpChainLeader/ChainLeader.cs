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
using System.Linq;

/// <summary>
/// Цепная выноска
/// </summary>
[SmartEntityDisplayNameKey("h171")] // TODO Цепная выноска 
[SystemStyleDescriptionKey("h172")] // TODO Базовый стиль для обозначения цепной выноски
public class ChainLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;

    #region Entities

    /// <summary>
    /// Секущая часть
    /// </summary>
    private Polyline _secantPolyline;

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

    
    /// <summary>
    /// Главная полилиния примитива
    /// </summary>
    //private Polyline _mainPolyline;

    //private Point3d _leaderFirstPoint;

    //private readonly List<Line> _leaderLines = new();
    //private readonly List<Polyline> _leaderEndLines = new();
    //private readonly List<Hatch> _hatches = new();
    //private Polyline _framePolyline;
    private double _scale;

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

                _secantPolyline,
                _leaderLine,
                _shelfLine,
                _topFirstDbText,
                _topSecondDbText,
                _bottomDbText,
            };

            //entities.AddRange(_leaderEndLines);
            //entities.AddRange(_hatches);

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
    /// Длина секущего элемента
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p88", 10, 5, 20, nameSymbol: "s")]
    [SaveToXData]
    public int SecantLength { get; set; } = 10;

    /// <summary>
    /// Отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p61", 1.0, 0.0, 3.0, nameSymbol: "o")]
    [SaveToXData]
    public double TextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;

    /// <summary>
    /// Выступ полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p63", 1, 0, 3, descLocalKey: "d63", nameSymbol: "l")]
    [SaveToXData]
    public int ShelfLedge { get; set; } = 1;

    /// <summary>
    /// Положение полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p78", ShelfPosition.Right)]
    [SaveToXData]
    public ShelfPosition ShelfPosition { get; set; } = ShelfPosition.Right;

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
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Выноска
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 9, "p104", true, descLocalKey: "d104")]
    [SaveToXData]
    public bool Leader { get; set; } = true;

    /// <summary>
    /// Точка выноски
    /// </summary>
    [SaveToXData]
    public Point3d LeaderPoint{ get; set; } = new();

    /// <summary>
    /// Точка выноски
    /// </summary>
    [SaveToXData]
    public List<Point3d> LeaderPoints{ get; set; } = new();

    /// <summary>
    /// Типы выносок
    /// </summary>
    [SaveToXData]
    public List<int> LeaderTypes { get; set; } = new();

    public Vector3d MainNormal { get; set; } = new();

    

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return LeaderPoint;
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            _scale = GetScale();

            //// Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == ChainLeaderJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X,
                    InsertionPointOCS.Y + (MinDistanceBetweenPoints * scale),
                    InsertionPointOCS.Z);
                var normal = (tempEndPoint - InsertionPointOCS).GetNormal();
                var vectorLength = normal * MinDistanceBetweenPoints;
                var tempPoint2 = tempEndPoint + vectorLength.RotateBy(Math.PI*-0.25, Vector3d.ZAxis);

                AcadUtils.WriteMessageInDebug($"JigState == ChainLeaderJigState InsertionPointOCS {InsertionPointOCS} - {tempEndPoint}");
                CreateEntities(InsertionPointOCS, tempPoint2, scale);
            }
            //// Указание точки выноски
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
    private void CreateEntities(
        Point3d insertionPoint,
        Point3d leaderPoint,
        double scale)
    {
        
        var arrowSize = ArrowSize * scale;
        var v = (leaderPoint - insertionPoint).GetNormal();

        var secantEnd = insertionPoint + (v * arrowSize);
        //_secantPolyline = new Polyline(2);
        //_secantPolyline.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0, 0);
        //_secantPolyline.AddVertexAt(1, secantEnd.ToPoint2d(), 0.0, 0, 0);

        if (secantEnd.DistanceTo(leaderPoint) > 0.0)
            _leaderLine = new Line(insertionPoint, leaderPoint);

        //// Дальше код идентичен коду в NodalLeader! Учесть при внесении изменений

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
            _topFirstDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
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
            _bottomDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
            bottomTextLength = _bottomDbText.GetLength();
            bottomTextHeight = _bottomDbText.GetHeight();
        }
        else
        {
            _bottomDbText = null;
        }

        var topTextLength = topFirstTextLength + topSecondTextLength;
        var largestTextLength = Math.Max(topTextLength, bottomTextLength);
        var shelfLength = textIndent + largestTextLength + shelfLedge;
        Point3d topFirstTextPosition;
        var topSecondTextPosition = default(Point3d);
        Point3d bottomTextPosition;

        if (isRight)
        {
            topFirstTextPosition = new Point3d(
                leaderPoint.X + (topFirstTextLength / 2) + ((shelfLength - topTextLength) / 2),
                leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2),
                0);
            bottomTextPosition = new Point3d(
                leaderPoint.X + (bottomTextLength / 2) + ((shelfLength - bottomTextLength) / 2),
                leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

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
                leaderPoint.X - (topFirstTextLength / 2) - topSecondTextLength - ((shelfLength - topTextLength) / 2),
                leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
            bottomTextPosition = new Point3d(
                leaderPoint.X - (bottomTextLength / 2) - ((shelfLength - bottomTextLength) / 2),
                leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

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
            ? leaderPoint + (Vector3d.XAxis * shelfLength)
            : leaderPoint - (Vector3d.XAxis * shelfLength);

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _topFirstTextMask = _topFirstDbText.GetBackgroundMask(offset, topFirstTextPosition);
            _topSecondTextMask = _topSecondDbText.GetBackgroundMask(offset, topSecondTextPosition);
            _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
        }

        if (IsTextAlwaysHorizontal && IsRotated)
        {
            var backRotationMatrix = GetBackRotationMatrix(leaderPoint);
            shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
            _topFirstDbText?.TransformBy(backRotationMatrix);
            _topFirstTextMask?.TransformBy(backRotationMatrix);
            _topSecondDbText?.TransformBy(backRotationMatrix);
            _topSecondTextMask?.TransformBy(backRotationMatrix);
            _bottomDbText?.TransformBy(backRotationMatrix);
            _bottomTextMask?.TransformBy(backRotationMatrix);
        }

        _shelfLine = new Line(leaderPoint, shelfEndPoint);
    }
    //private void CreateEntities(Point3d insertionPoint, Point3d leaderPoint, double scale)
    //{
    //    //_leaderLines.Clear();
    //    //_leaderEndLines.Clear();
    //    //_hatches.Clear()

    //    var arrowSize = ArrowSize * scale;

    //    var mainNormal = (leaderPoint - insertionPoint).GetNormal();

        
    //    //var secantLength = ArrowSize * scale;
    //    //var v = (leaderPoint - insertionPoint).GetNormal();

    //    //var secantEnd = insertionPoint + (v * secantLength);

    //    //_mainPolyline = new Polyline(2);
    //    //_mainPolyline.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0, 0);
    //    //_mainPolyline.AddVertexAt(1, leaderPoint.ToPoint2d(), 0.0, 0, 0);
    //    //AcadUtils.WriteMessageInDebug($"startpoint {_mainPolyline.StartPoint} endpoint {_mainPolyline.EndPoint}");
    //    _leaderLine = new Line(insertionPoint, leaderPoint);
    //    //if (!Leader || (string.IsNullOrEmpty(MainText) && string.IsNullOrEmpty(SmallText)))
    //    //{
    //    //    _leaderLine = null;
    //    //    _shelfLine = null;
    //    //}

    //    //// Дальше код идентичен коду в SecantNodalLeader! Учесть при внесении изменений

    //    SetNodeNumberOnCreation();

    //    var mainTextHeight = MainTextHeight * scale;
    //    var secondTextHeight = SecondTextHeight * scale;
    //    var textIndent = TextIndent * scale;
    //    var textVerticalOffset = TextVerticalOffset * scale;
    //    var shelfLedge = ShelfLedge * scale;
    //    var isRight = ShelfPosition == ShelfPosition.Right;

    //    var topFirstTextLength = 0.0;
    //    var topSecondTextLength = 0.0;
    //    var bottomTextLength = 0.0;
    //    var bottomTextHeight = 0.0;

    //    if (!string.IsNullOrEmpty(NodeNumber))
    //    {
    //        _topFirstDbText = new DBText { TextString = NodeNumber };
    //        _topFirstDbText.SetProperties(TextStyle, mainTextHeight);
    //        _topFirstDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
    //        topFirstTextLength = _topFirstDbText.GetLength();
    //    }
    //    else
    //    {
    //        _topFirstDbText = null;
    //    }

    //    if (!string.IsNullOrEmpty(SheetNumber))
    //    {
    //        _topSecondDbText = new DBText { TextString = $"({SheetNumber})" };
    //        _topSecondDbText.SetProperties(TextStyle, secondTextHeight);
    //        _topSecondDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
    //        topSecondTextLength = _topSecondDbText.GetLength();
    //    }
    //    else
    //    {
    //        _topSecondDbText = null;
    //    }

    //    if (!string.IsNullOrEmpty(NodeAddress))
    //    {
    //        _bottomDbText = new DBText { TextString = NodeAddress };
    //        _bottomDbText.SetProperties(TextStyle, secondTextHeight);
    //        _bottomDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
    //        bottomTextLength = _bottomDbText.GetLength();
    //        bottomTextHeight = _bottomDbText.GetHeight();
    //    }
    //    else
    //    {
    //        _bottomDbText = null;
    //    }

    //    var topTextLength = topFirstTextLength + topSecondTextLength;
    //    var largestTextLength = Math.Max(topTextLength, bottomTextLength);
    //    var shelfLength = textIndent + largestTextLength + shelfLedge;
    //    Point3d topFirstTextPosition;
    //    var topSecondTextPosition = default(Point3d);
    //    Point3d bottomTextPosition;

    //    if (isRight)
    //    {
    //        topFirstTextPosition = new Point3d(
    //            leaderPoint.X + (topFirstTextLength / 2) + ((shelfLength - topTextLength) / 2),
    //            leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2),
    //            0);
    //        bottomTextPosition = new Point3d(
    //            leaderPoint.X + (bottomTextLength / 2) + ((shelfLength - bottomTextLength) / 2),
    //            leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

    //        if (_topFirstDbText != null)
    //        {
    //            _topFirstDbText.Position = topFirstTextPosition;
    //            _topFirstDbText.AlignmentPoint = topFirstTextPosition;
    //        }
            
    //        if (_bottomDbText != null)
    //        {
    //            _bottomDbText.Position = bottomTextPosition;
    //            _bottomDbText.AlignmentPoint = bottomTextPosition;
    //        }
    //    }
    //    else
    //    {
    //        topFirstTextPosition = new Point3d(
    //            leaderPoint.X - (topFirstTextLength / 2) - topSecondTextLength - ((shelfLength - topTextLength) / 2),
    //            leaderPoint.Y + textVerticalOffset + (mainTextHeight / 2), 0);
    //        bottomTextPosition = new Point3d(
    //            leaderPoint.X - (bottomTextLength / 2) - ((shelfLength - bottomTextLength) / 2),
    //            leaderPoint.Y - textVerticalOffset - (bottomTextHeight / 2), 0);

    //        if (_topFirstDbText != null)
    //        {
    //            _topFirstDbText.Position = topFirstTextPosition;
    //            _topFirstDbText.AlignmentPoint = topFirstTextPosition;
    //        }

    //        if (_bottomDbText != null)
    //        {
    //            _bottomDbText.Position = bottomTextPosition;
    //            _bottomDbText.AlignmentPoint = bottomTextPosition;
    //        }
    //    }

    //    if (_topSecondDbText != null)
    //    {
    //        topSecondTextPosition = new Point3d(
    //            topFirstTextPosition.X + (topFirstTextLength / 2) + (topSecondTextLength / 2), topFirstTextPosition.Y, 0);
    //        _topSecondDbText.Position = topSecondTextPosition;
    //        _topSecondDbText.AlignmentPoint = topSecondTextPosition;
    //    }

    //    var shelfEndPoint = ShelfPosition == ShelfPosition.Right
    //        ? leaderPoint + (Vector3d.XAxis * shelfLength)
    //        : leaderPoint - (Vector3d.XAxis * shelfLength);

    //    if (HideTextBackground)
    //    {
    //        var offset = TextMaskOffset * scale;
    //        _topFirstTextMask = _topFirstDbText.GetBackgroundMask(offset, topFirstTextPosition);
    //        _topSecondTextMask = _topSecondDbText.GetBackgroundMask(offset, topSecondTextPosition);
    //        _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, bottomTextPosition);
    //    }

    //    if (IsTextAlwaysHorizontal && IsRotated)
    //    {
    //        var backRotationMatrix = GetBackRotationMatrix(leaderPoint);
    //        shelfEndPoint = shelfEndPoint.TransformBy(backRotationMatrix);
    //        _topFirstDbText?.TransformBy(backRotationMatrix);
    //        _topFirstTextMask?.TransformBy(backRotationMatrix);
    //        _topSecondDbText?.TransformBy(backRotationMatrix);
    //        _topSecondTextMask?.TransformBy(backRotationMatrix);
    //        _bottomDbText?.TransformBy(backRotationMatrix);
    //        _bottomTextMask?.TransformBy(backRotationMatrix);
    //    }

    //    //if (_leaderLine != null && (_bottomDbText != null || _topDbText != null))
    //    //{
    //        _shelfLine = new Line(EndPointOCS, shelfEndPoint);
    //    //}

    //    MirrorIfNeed(new[] {_topFirstDbText, _topSecondDbText, _bottomDbText });

    //    //for (var i = 0; i < LeaderPointsOCS.Count; i++)
    //    //{

    //    //    var pline = new Polyline();

    //    //    if (_leaderLines[i].Length - (ArrowSize * _scale) > 0)
    //    //    {
    //    //        if (LeaderTypes.Count <= 0)
    //    //        {
    //    //            pline = CreateResectionArrow(_leaderLines[i]);
    //    //        }
    //    //        else
    //    //        {
    //    //            switch ((LeaderEndType)LeaderTypes[i])
    //    //            {
    //    //                case LeaderEndType.None:
    //    //                    break;
    //    //                case LeaderEndType.HalfArrow:
    //    //                    _hatches.Add(CreateArrowHatch(CreateHalfArrow(_leaderLines[i])));
    //    //                    break;
    //    //                case LeaderEndType.Point:
    //    //                    _hatches.Add(CreatePointHatch(CreatePointArrow(_leaderLines[i])));
    //    //                    break;
    //    //                case LeaderEndType.Resection:
    //    //                    pline = CreateResectionArrow(_leaderLines[i]);
    //    //                    break;
    //    //                case LeaderEndType.Angle:
    //    //                    pline = CreateAngleArrow(_leaderLines[i], 45, false);
    //    //                    break;
    //    //                case LeaderEndType.Arrow:
    //    //                    _hatches.Add(CreateArrowHatch(CreateAngleArrow(_leaderLines[i], 10, true)));
    //    //                    break;
    //    //                case LeaderEndType.OpenArrow:
    //    //                    pline = CreateAngleArrow(_leaderLines[i], 10, false);
    //    //                    break;
    //    //                case LeaderEndType.ClosedArrow:
    //    //                    pline = CreateAngleArrow(_leaderLines[i], 10, true);
    //    //                    break;
    //    //            }
    //    //        }
    //    //    }

    //    //    _leaderEndLines.Add(pline);
    //    //}
    //}

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        NodeNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }

    #region Arrows
    private Polyline CreateResectionArrow(Line leaderLine)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize / 2 * _scale));
        var startPoint = tmpPoint.RotateBy(45.DegreeToRadian(), vector, leaderLine.EndPoint);
        var endPoint = tmpPoint.RotateBy(225.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(2);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0.3, 0.3);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0.3, 0.3);

        return pline;
    }

    private Polyline CreateAngleArrow(Line leaderLine, int angle, bool closed)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize * _scale));
        var startPoint = tmpPoint.RotateBy(angle.DegreeToRadian(), vector, leaderLine.EndPoint);
        var endPoint = tmpPoint.RotateBy((-1) * angle.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(leaderLine.EndPoint), 0, 0, 0);
        pline.AddVertexAt(2, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(endPoint), 0, 0, 0);

        pline.Closed = closed;

        return pline;
    }

    private Polyline CreateHalfArrow(Line leaderLine)
    {
        var vector = new Vector3d(0, 0, 1);
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize * _scale));
        var endPoint = startPoint.RotateBy(10.DegreeToRadian(), vector, leaderLine.EndPoint);

        var pline = new Polyline(3);

        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 0, 0, 0);
        pline.AddVertexAt(1, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(leaderLine.EndPoint), 0, 0, 0);
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

    private Polyline CreatePointArrow(Line leaderLine)
    {
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - (ArrowSize / 2 * _scale));
        var endPoint = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(leaderLine.StartPoint, leaderLine.EndPoint, leaderLine.Length + (ArrowSize / 2 * _scale));

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, endPoint, 1, 0, 0);
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
}