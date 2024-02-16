namespace mpESKD.Functions.mpLevelPlanMark;

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
/// Отметка на плане
/// </summary>
[SmartEntityDisplayNameKey("h171")] // Отметка уровня на плане
[SystemStyleDescriptionKey("h172")] // Базовый стиль для обозначения отметки уровня на плане
public class LevelPlanMark : SmartEntity, ITextValueEntity, INumericValueEntity, IWithDoubleClickEditor
{
    #region Text entities

    private DBText _dbText;
    private Wipeout _dbTextMask;

    #endregion

    private readonly List<Line> _leaderLines = new ();
    private readonly List<Polyline> _leaderEndLines = new ();
    private readonly List<Hatch> _hatches = new ();
    private Polyline _framePolyline;
    private double _scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMark"/> class.
    /// </summary>
    public LevelPlanMark()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMark"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public LevelPlanMark(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 1;

    /// <summary>
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p82", FrameType.Rectangular)]
    [SaveToXData]
    public FrameType FrameType { get; set; } = FrameType.Rectangular;

    /// <summary>
    /// Размер стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p29", 5, 0.1, 10, nameSymbol: "d")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Ширина рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p77", 12, 1, 65, nameSymbol: "l")]
    [SaveToXData]
    public double BorderWidth { get; set; }

    /// <summary>
    /// Высота рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p76", 5, 0, 10, nameSymbol: "h2")]
    [SaveToXData]
    public double BorderHeight { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double TextHeight { get; set; } = 3.5;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", false, descLocalKey: "d85-1")]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0, descLocalKey: "d86")]
    [PropertyVisibilityDependency(new[] { nameof(FrameType.None) }, new[] { nameof(FrameType.Rectangular) })]
    [SaveToXData]
    public double TextMaskOffset { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 6, "p72", NumberSeparator.Dot, descLocalKey: "d72")]
    [SaveToXData]
    public NumberSeparator NumberSeparator { get; set; } = NumberSeparator.Dot;

    /// <summary>
    /// Точность
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p67", 3, 0, 5, descLocalKey: "d67")]
    [SaveToXData]
    public int Accuracy { get; set; } = 3;

    /// <summary>
    /// Отметка уровня
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 10, "p110", "", 0, 5, propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [SaveToXData]
    [ValueToSearchBy]
    public double PlanMark { get; set; } = 0.000;

    /// <summary>
    /// Префикс
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 11, "p111", "", propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Суффикс
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 12, "p112", "", 0, 5, propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string Suffix { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое значение
    /// </summary>
    [ValueToSearchBy]
    public string DisplayedValue
    {
        get
        {
            var prefix = string.IsNullOrEmpty(Prefix) ? string.Empty : Prefix;
            var suffix = string.IsNullOrEmpty(Suffix) ? string.Empty : Suffix;

            return ReplaceSeparator($"{prefix}{Math.Round(PlanMark, Accuracy).ToString($"F{Accuracy}")}{suffix}");
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _dbTextMask,
                _dbText,
                _framePolyline
            };
            if (_leaderLines?.Count > 0)
            {
                entities.AddRange(_leaderLines);
            }

            entities.AddRange(_leaderEndLines);
            entities.AddRange(_hatches);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <summary>
    /// Точки выносок
    /// </summary>
    [SaveToXData]
    public List<Point3d> LeaderPoints { get; set; } = new ();

    /// <summary>
    /// Типы выносок
    /// </summary>
    [SaveToXData]
    public List<int> LeaderTypes { get; set; } = new ();

    private List<Point3d> LeaderPointsOCS => LeaderPoints.Select(p => p.TransformBy(BlockTransform.Inverse())).ToList();

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
    }

    /// <inheritdoc />
    public override void UpdateEntities()
    {
        try
        {
            _scale = GetScale();
            CreateEntities(InsertionPointOCS);
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint)
    {
        _leaderLines.Clear();
        _leaderEndLines.Clear();
        _hatches.Clear();

        _dbText = new DBText { TextString = DisplayedValue, Position = insertionPoint };
        _dbText.SetProperties(TextStyle, TextHeight * _scale);
        _dbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
        _dbText.AlignmentPoint = insertionPoint;

        if (BorderWidth == 0)
        {
            BorderWidth = MinDistanceBetweenPoints * _scale;
        }

        if (BorderHeight == 0)
        {
            BorderHeight = MinDistanceBetweenPoints * _scale;
        }

        // Проверка на ширину текста, при изменении стиля на более широкий
        // todo Возможно, это лишнее, т.к. пользователь сам задает рамку через палитру
        var dbTextWidth = _dbText.GetLength();
        if (BorderWidth < dbTextWidth)
        {
            BorderWidth = dbTextWidth;
        }

        // todo Обратная проблема - если стиль меняется на более узкий, остается слишком широкая рамка

        var borderHalfLength = BorderWidth / 2 * _scale;
        var borderHalfHeight = BorderHeight / 2 * _scale;

        var points = new[]
        {
            new Point3d(insertionPoint.X - borderHalfLength, insertionPoint.Y - borderHalfHeight, 0),
            new Point3d(insertionPoint.X + borderHalfLength, insertionPoint.Y - borderHalfHeight, 0),
            new Point3d(insertionPoint.X + borderHalfLength, insertionPoint.Y + borderHalfHeight, 0),
            new Point3d(insertionPoint.X - borderHalfLength, insertionPoint.Y + borderHalfHeight, 0)
        };

        if (FrameType == FrameType.None)
        {
            if (HideTextBackground)
            {
                _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * _scale, insertionPoint);
            }
        }
        else
        {
            if (FrameType == FrameType.Line)
            {
                _framePolyline = new Polyline(points.Length - 2);
                for (var i = 0; i < points.Length - 2; i++)
                {
                    _framePolyline.AddVertexAt(i, points[i].ToPoint2d(), 0, 0.0, 0.0);
                }

                if (HideTextBackground)
                {
                    _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * _scale, insertionPoint);
                }
            }
            else
            {
                _framePolyline = new Polyline(points.Length);
                for (var i = 0; i < points.Length; i++)
                {
                    _framePolyline.AddVertexAt(i, points[i].ToPoint2d(), 0, 0.0, 0.0);
                }

                _framePolyline.Closed = true;

                if (HideTextBackground)
                {
                    _dbTextMask = _framePolyline.GetBackgroundMask();
                }
            }
        }

        for (var i = 0; i < LeaderPointsOCS.Count; i++)
        {
            var points2ds = from point in points
                            select point.ToPoint2d();
            var curLeader = CreateLeaders(LeaderPointsOCS[i], points2ds);
            _leaderLines.Add(curLeader);

            var mainNormal = (curLeader.StartPoint - curLeader.EndPoint).GetNormal();

            var arrowTypes = new ArrowBuilder(mainNormal, ArrowSize, _scale);

            if (_leaderLines[i].Length - (ArrowSize * _scale) > 0)
            {
                arrowTypes.BuildArrow((LeaderEndType)LeaderTypes[i], _leaderLines[i].EndPoint, _hatches, _leaderEndLines);
            }
        }
    }

    private Line CreateLeaders(Point3d point, IEnumerable<Point2d> points)
    {
        var nearestPoint = points.OrderBy(p => p.GetDistanceTo(point.ToPoint2d())).First();
        var line = new Line(nearestPoint.ToPoint3d(), point);

        return line;
    }

    private string ReplaceSeparator(string numericValue)
    {
        var c = NumberSeparator == NumberSeparator.Comma ? ',' : '.';
        return numericValue.Replace(',', '.').Replace('.', c);
    }
}