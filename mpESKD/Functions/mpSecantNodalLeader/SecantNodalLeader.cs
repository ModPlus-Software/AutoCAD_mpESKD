﻿namespace mpESKD.Functions.mpSecantNodalLeader;

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

/// <summary>
/// Секущая узловая выноска
/// </summary>
[SmartEntityDisplayNameKey("h133")]
[SystemStyleDescriptionKey("h138")]
public class SecantNodalLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
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

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SecantNodalLeader"/> class.
    /// </summary>
    public SecantNodalLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecantNodalLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public SecantNodalLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecantNodalLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public SecantNodalLeader(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <summary>
    /// Состояние Jig при создании секущей узловой выноски
    /// </summary>
    public SecantNodalLeaderJigState? JigState { get; set; }

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => SecantLength + 1;

    /// <inheritdoc/>
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

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc/>
    /// Не используется!
    public override string LineType { get; set; }

    /// <inheritdoc/>
    /// Не используется!
    public override double LineTypeScale { get; set; }

    /// <summary>
    /// Толщина секущего элемента
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p87", 1.0, 0.1, 3.0, nameSymbol: "t")]
    [SaveToXData]
    public double SecantThickness { get; set; } = 1.0;

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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();

            //// Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == SecantNodalLeaderJigState.InsertionPoint)
            {
                var tempEndPoint = new Point3d(
                    InsertionPointOCS.X,
                    InsertionPointOCS.Y + (MinDistanceBetweenPoints * scale),
                    InsertionPointOCS.Z);

                CreateEntities(InsertionPointOCS, tempEndPoint, scale);
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
        var secantThickness = SecantThickness * scale;
        var secantLength = SecantLength * scale;
        var v = (leaderPoint - insertionPoint).GetNormal();

        var secantEnd = insertionPoint + (v * secantLength);
        _secantPolyline = new Polyline(2);
        _secantPolyline.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, secantThickness, secantThickness);
        _secantPolyline.AddVertexAt(1, secantEnd.ToPoint2d(), 0.0, secantThickness, secantThickness);

        if (secantEnd.DistanceTo(leaderPoint) > 0.0)
            _leaderLine = new Line(secantEnd, leaderPoint);

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

        if (isRight)
        {
            var topFirstTextPosition = new Point3d(leaderPoint.X + topFirstTextLength / 2 + (shelfLength - topTextLength) / 2,
                leaderPoint.Y + textVerticalOffset + mainTextHeight / 2, 0);

            if (_topFirstDbText != null)
            {
                _topFirstDbText.Position = topFirstTextPosition;
                _topFirstDbText.AlignmentPoint = topFirstTextPosition;
            }

            if (_topSecondDbText != null)
            {
                var secondTextPosition = new Point3d(_topFirstDbText.Position.X + topFirstTextLength / 2 + topSecondTextLength / 2,
                    topFirstTextPosition.Y, 0);
                _topSecondDbText.Position = secondTextPosition;
                _topSecondDbText.AlignmentPoint = secondTextPosition;
            }

            if (_bottomDbText != null)
            {
                var bottomTextPosition = new Point3d(leaderPoint.X + bottomTextLength / 2 + (shelfLength - bottomTextLength) / 2,
                    leaderPoint.Y - textVerticalOffset - bottomTextHeight / 2, 0);
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }
        else
        {
            if (_topFirstDbText != null)
            {
                var topTextPosition = new Point3d(leaderPoint.X - topFirstTextLength / 2 - topSecondTextLength - (shelfLength - topTextLength) / 2,
                    leaderPoint.Y + textVerticalOffset + mainTextHeight / 2, 0);
                _topFirstDbText.Position = topTextPosition;
                _topFirstDbText.AlignmentPoint = topTextPosition;
            }

            if (_topSecondDbText != null)
            {
                var secondTextPosition = new Point3d(_topFirstDbText.Position.X + topFirstTextLength / 2 + topSecondTextLength / 2,
                    _topFirstDbText.Position.Y, 0);
                _topSecondDbText.Position = secondTextPosition;
                _topSecondDbText.AlignmentPoint = secondTextPosition;
            }

            if (_bottomDbText != null)
            {
                var bottomTextPosition = new Point3d(leaderPoint.X - bottomTextLength / 2 - (shelfLength - bottomTextLength) / 2,
                    leaderPoint.Y - textVerticalOffset - bottomTextHeight / 2, 0);
                _bottomDbText.Position = bottomTextPosition;
                _bottomDbText.AlignmentPoint = bottomTextPosition;
            }
        }

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? leaderPoint + (Vector3d.XAxis * shelfLength)
            : leaderPoint - (Vector3d.XAxis * shelfLength);

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            if (_topFirstDbText != null)
            {
                _topFirstTextMask = _topFirstDbText.GetBackgroundMask(offset, _topFirstDbText.Position);
            }

            if (_topSecondDbText != null)
            {
                _topSecondTextMask = _topSecondDbText.GetBackgroundMask(offset, _topSecondDbText.Position);
            }

            if (_bottomDbText != null)
            {
                _bottomTextMask = _bottomDbText.GetBackgroundMask(offset, _bottomDbText.Position);
            }
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

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        NodeNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }
}