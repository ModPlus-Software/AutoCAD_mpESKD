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
[SmartEntityDisplayNameKey("h171")] //Отметка уровня на плане
[SystemStyleDescriptionKey("h172")] //Базовый стиль для обозначения отметки уровня на плане

public class LevelPlanMark : SmartEntity, ITextValueEntity, INumericValueEntity, IWithDoubleClickEditor
{

    #region Text entities

    private DBText _dbText;
    private Wipeout _dbTextMask;

    #endregion

    private Polyline _framePolyline;

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
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p82", FrameType.Rectangular)]
    [SaveToXData]
    public FrameType FrameType { get; set; } = FrameType.Rectangular;

    /// <summary>
    /// Размер стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p29", 5, 0, 100, descLocalKey: "d67")]
    [SaveToXData]
    public double ArrowSize { get; set; } = 3;

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double TextHeight { get; set; } = 3.5;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", false, descLocalKey: "d85")]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0)]
    [PropertyVisibilityDependency(new[] { nameof(FrameType.None) }, new[] { nameof(FrameType.Rectangular) })]
    [SaveToXData]
    public double TextMaskOffset { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 6, "p72", NumberSeparator.Dot, descLocalKey: "d72")]
    [SaveToXData]
    public NumberSeparator NumberSeparator { get; set; } = NumberSeparator.Dot;

    /// <summary>
    /// Показывать плюс
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 8, "p64", false, descLocalKey: "d64")]
    [SaveToXData]
    public bool ShowPlus { get; set; }

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
    [EntityProperty(PropertiesCategory.Content, 11, "p77", 12, 1, 65, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderWidth { get; set; }

    /// <summary>
    /// Высота рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 12, "p76", 5, 0, 10, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderHeight { get; set; }

    /// <summary>
    /// Обозначение плана
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 13, "p110", "", 0, 5, propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [SaveToXData]
    [ValueToSearchBy]
    public double PlanMark { get; set; } = 0.000; 

    /// <summary>
    /// Префикс уровня
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 14, "p111", "", propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Суффикс уровня
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 15, "p112", "", 0, 5, propertyScope: PropertyScope.Palette, stringMaxLength: 10)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationSuffix { get; set; } = string.Empty;

    private double _scale;

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

            entities.AddRange(_leaderEndLines);
            entities.AddRange(_hatches);

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    [SaveToXData]
    public List<Point3d> LeaderPoints { get; set; } = new List<Point3d>();

    /// <summary>
    /// Составные линии
    /// </summary>
    [SaveToXData]
    public List<int> LeaderTypes { get; set; } = new List<int>();

    private List<Line> _leaderLines = new();
    private List<Polyline> _leaderEndLines = new();
    private List<Hatch> _hatches = new();

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
        _dbText.Position = _dbText.Position - (Vector3d.XAxis * (_dbText.GetLength() / 2)) - (Vector3d.YAxis * (_dbText.GetHeight() / 2));

        if (BorderWidth == 0)
        {
            BorderWidth = MinDistanceBetweenPoints * _scale;
        }

        if (BorderHeight == 0)
        {
            BorderHeight = MinDistanceBetweenPoints * _scale;
        }

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
                _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * _scale);
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
                    _dbTextMask = _dbText.GetBackgroundMask(TextMaskOffset * _scale);
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
            _leaderLines.Add(CreateLeaders(LeaderPointsOCS[i], points2ds));
            Polyline pline = new Polyline();

            if (_leaderLines[i].Length - ArrowSize * _scale > 0)
            {
                if (LeaderTypes.Count <= 0)
                {
                    pline = CreateResectionArrow(_leaderLines[i]);
                }
                else
                {
                    switch (LeaderTypes[i])
                    {
                        case 0: //None
                            break;
                        case 1: //HalfArrow
                            _hatches.Add(CreateArrowHatch(CreateHalfArrow(_leaderLines[i])));
                            break;
                        case 2: //Point

                            _hatches.Add(CreatePointHatch(CreatePointArrow(_leaderLines[i])));
                            break;
                        case 3: //Resection
                            pline = CreateResectionArrow(_leaderLines[i]);
                            break;
                        case 4: //Angle
                            pline = CreateAngleArrow(_leaderLines[i], 45, false);
                            break;
                        case 5: //Arrow
                            _hatches.Add(CreateArrowHatch(CreateAngleArrow(_leaderLines[i], 10, true)));
                            break;
                        case 6: // OpenArrow
                            pline = CreateAngleArrow(_leaderLines[i], 10, false);
                            break;
                        case 7: //ClosedArrow
                            pline = CreateAngleArrow(_leaderLines[i], 10, true);
                            break;
                    }
                }
            }

            _leaderEndLines.Add(pline);
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

    #region Arrows
    private Polyline CreateResectionArrow(Line leaderLine)
    {
        var vector = new Vector3d(0, 0, 1);
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - ArrowSize / 2 * _scale);
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
        var tmpPoint = leaderLine.GetPointAtDist(leaderLine.Length - ArrowSize * _scale);
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
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - ArrowSize * _scale);
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
        Point2dCollection vertexCollection = new Point2dCollection();
        for (int index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        DoubleCollection bulgeCollection = new DoubleCollection()
        {
            0.0, 0.0, 0.0
        };

        Hatch hatch = new Hatch();
        hatch.SetHatchPattern((HatchPatternType)1, "SOLID");
        hatch.AppendLoop((HatchLoopTypes)0, vertexCollection, bulgeCollection);

        return hatch;
    }

    private Polyline CreatePointArrow(Line leaderLine)
    {
        var startPoint = leaderLine.GetPointAtDist(leaderLine.Length - ArrowSize / 2 * _scale);
        var endPoint = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(leaderLine.StartPoint, leaderLine.EndPoint, leaderLine.Length + ArrowSize / 2);

        var pline = new Polyline(2);
        pline.AddVertexAt(0, ModPlus.Helpers.GeometryHelpers.ConvertPoint3dToPoint2d(startPoint), 1, 0, 0);
        pline.AddVertexAt(1, endPoint, 1, 0, 0);
        pline.Closed = true;

        return pline;
    }

    private Hatch CreatePointHatch(Polyline pline)
    {
        Point2dCollection vertexCollection = new Point2dCollection();
        for (int index = 0; index < pline.NumberOfVertices; ++index)
        {
            vertexCollection.Add(pline.GetPoint2dAt(index));
        }

        vertexCollection.Add(pline.GetPoint2dAt(0));
        DoubleCollection bulgeCollection = new DoubleCollection()
        {
            1.0, 1.0
        };

        Hatch hatch = new Hatch();
        hatch.SetHatchPattern((HatchPatternType)1, "SOLID");
        hatch.AppendLoop((HatchLoopTypes)0, vertexCollection, bulgeCollection);

        return hatch;
    }

    #endregion
}