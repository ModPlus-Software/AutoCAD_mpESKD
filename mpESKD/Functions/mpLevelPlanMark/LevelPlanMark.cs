namespace mpESKD.Functions.mpLevelPlanMark;

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
/// Отметка на плане
/// </summary>
[SmartEntityDisplayNameKey("h151")] //TODO localization
[SystemStyleDescriptionKey("h152")] //TODO localization

public class LevelPlanMark : SmartEntity, ITextValueEntity, INumericValueEntity, IWithDoubleClickEditor
{

    #region Text entities

    private DBText _dbText;
    private Wipeout _dbTextMask;
    private Polyline _framePolyline;

    #endregion

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
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 1;

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double TextHeight { get; set; } = 3.5;

    /// <summary>
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p82", FrameType.Rectangular)]
    [SaveToXData]
    public FrameType FrameType { get; set; } = FrameType.Rectangular;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", false, descLocalKey: "d85")]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0)]
    [PropertyVisibilityDependency(new[] { nameof(FrameType.None) }, new[] { nameof(FrameType.Rectangular) })]
    [SaveToXData]
    public double TextMaskOffset { get; set; }

    /// <summary>
    /// Обозначение плана
    /// </summary>
    [SaveToXData]
    [ValueToSearchBy]
    public double PlanMark { get; set; } = 0.000;

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 6, "p72", NumberSeparator.Dot, descLocalKey: "d72")]
    [SaveToXData]
    public NumberSeparator NumberSeparator { get; set; } = NumberSeparator.Dot;

    /// <summary>
    /// Показывать плюс
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p64", true, descLocalKey: "d64")]
    [SaveToXData]
    public bool ShowPlus { get; set; } = true;

    /// <summary>
    /// Добавление звездочки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 9, "p75", false, descLocalKey: "d75")]
    [SaveToXData]
    public bool AddAsterisk { get; set; }

    /// <summary>
    /// Точность
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 10, "p67", 3, 0, 5, descLocalKey: "d67")]
    [SaveToXData]
    public int Accuracy { get; set; } = 3;

    /// <summary>
    /// Ширина рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 11, "p77", 9, 1, 10, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderWidth { get; set; }

    /// <summary>
    /// Высота рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 12, "p76", 3, 0, 10, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderHeight { get; set; }

    /// <summary>
    /// Размер стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 13, "p76", 5, 0, 100, descLocalKey: "d67")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 5;

    /// <summary>
    /// Префикс уровня
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 14, "p110", "", propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Суффикс уровня
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 15, "p111", "", 0, 5, propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationSuffix { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое значение
    /// </summary>
    [ValueToSearchBy]
    public string DisplayedValue
    {
        get
        {
            var prefix = string.IsNullOrEmpty(DesignationPrefix) ? string.Empty : DesignationPrefix;
            var suffix = string.IsNullOrEmpty(DesignationSuffix) ? string.Empty : DesignationSuffix;
            var asterisk = AddAsterisk ? "*" : string.Empty;
            var plus = ShowPlus ? "+" : string.Empty;
            return ReplaceSeparator($"{plus}{prefix}{Math.Round(PlanMark, Accuracy).ToString($"F{Accuracy}")}{suffix}{asterisk}");
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


            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <summary>
    /// Точки мтекста
    /// </summary>
    private List<Point3d> _leaderPoints = new();

    [SaveToXData]
    public List<Point3d> LeaderPoints { get; set; } = new List<Point3d>();

    /// <summary>
    /// Составные линии
    /// </summary>
    private List<Line> _leaderTypes = new();

    private List<Line> _leaderLines = new();

    private List<Point3d> LeaderPointsOCS
    {
        get
        {
            var points = new List<Point3d>();
            LeaderPoints.ForEach(p => points.Add(p.TransformBy(BlockTransform.Inverse())));
            return points;
        }

    }

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
            AcadUtils.WriteMessageInDebug($" insertionPointOCS {InsertionPointOCS} \n");
            var scale = GetScale();
            CreateEntities(InsertionPointOCS, scale);
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint, double scale)
    {
        _dbText = new DBText { TextString = DisplayedValue, Position = insertionPoint };
        _dbText.SetProperties(TextStyle, TextHeight);
        _dbText.Position = _dbText.Position - (Vector3d.XAxis * (_dbText.GetLength() / 2)) - (Vector3d.YAxis * (_dbText.GetHeight() / 2));

        // TODO

        if (BorderWidth == 0)
        {
            BorderWidth = MinDistanceBetweenPoints * scale;
        }

        if (BorderHeight == 0)
        {
            BorderHeight = MinDistanceBetweenPoints * scale;
        }

        var borderHalfLength = BorderWidth / 2 * scale;
        var borderHalfHeight = BorderHeight / 2 * scale;

        var points = new[]
        {
            new Point2d(insertionPoint.X - borderHalfLength, insertionPoint.Y - borderHalfHeight),
            new Point2d(insertionPoint.X + borderHalfLength, insertionPoint.Y - borderHalfHeight),
            new Point2d(insertionPoint.X + borderHalfLength, insertionPoint.Y + borderHalfHeight),
            new Point2d(insertionPoint.X - borderHalfLength, insertionPoint.Y + borderHalfHeight)

        };

        if (FrameType == FrameType.None)
        {
            if (HideTextBackground)
            {
                AcadUtils.WriteMessageInDebug($"dbText.GeometricExtents {_dbText.GeometricExtents}");

                _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * scale);
            }
        }
        else
        {
            if (FrameType == FrameType.Line)
            {
                _framePolyline = new Polyline(points.Length - 2);
                for (var i = 0; i < points.Length - 2; i++)
                {
                    _framePolyline.AddVertexAt(i, points[i], 0, 0.0, 0.0);
                }

                if (HideTextBackground)
                {
                    AcadUtils.WriteMessageInDebug($"dbText.GeometricExtents {_dbText.GeometricExtents}");
                    _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * scale);
                }
            }
            else
            {
                _framePolyline = new Polyline(points.Length);
                for (var i = 0; i < points.Length; i++)
                {
                    _framePolyline.AddVertexAt(i, points[i], 0, 0.0, 0.0);
                }

                _framePolyline.Closed = true;

                if (HideTextBackground)
                {
                    _dbTextMask = _framePolyline.GetBackgroundMask();
                }
            }
        }

        _leaderLines.AddRange(CreateLeaders(LeaderPointsOCS));
    }

    private IEnumerable<Line> CreateLeaders(List<Point3d> points)
    {
        var lines = new List<Line>();
        foreach (Point3d point in points)
        {
            var line = new Line(InsertionPoint, point);
            lines.Add(line);
        }

        return lines;
    }

    private string ReplaceSeparator(string numericValue)
    {
        var c = NumberSeparator == NumberSeparator.Comma ? ',' : '.';
        return numericValue.Replace(',', '.').Replace('.', c);
    }

    private void CreateHalfArrow(Line line, Point3d point3d)
    {
        Polyline()
    }
}