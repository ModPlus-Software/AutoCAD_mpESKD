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
    public override double MinDistanceBetweenPoints => 9.3106601717789772;

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 3.5;

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 4, "p85", false, descLocalKey: "d85")]
    [PropertyVisibilityDependency(new[] { nameof(TextMaskOffset) })]
    [SaveToXData]
    public bool HideTextBackground { get; set; }

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 5, "p86", 0.5, 0.0, 5.0)]
    [SaveToXData]
    public double TextMaskOffset { get; set; } = 0.5;

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
    /// Длина рамки
    /// </summary> //TODO localization
    [EntityProperty(PropertiesCategory.Content, 11, "p67", 9, 1, 10, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderLength { get; set; }

    /// <summary>
    /// Высота рамки
    /// </summary> //TODO localization
    [EntityProperty(PropertiesCategory.Content, 12, "p67", 3, 0, 10, descLocalKey: "d67")]
    [SaveToXData]
    public double BorderHeight { get; set; }

    /// <summary>
    /// Префикс обозначения
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 13, "p52", "", propertyScope: PropertyScope.Palette)]
    [RegexInputRestriction("^.{0,10}$")]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Суффикс обозначения
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 14, "p52", "", 0, 5, propertyScope: PropertyScope.Palette)]
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
                //_mText,
                //_mTextMask,
                _framePolyline
            };

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
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
        // text
        var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
        var textHeight = MainTextHeight * scale;

        _dbText = new DBText()
        {
            Position = insertionPoint,
            TextStyleId = textStyleId,
            TextString = DisplayedValue,
            Height = textHeight,
            Justify = AttachmentPoint.MiddleCenter,
            AlignmentPoint = insertionPoint
        };

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _dbTextMask = _dbText.GetBackgroundMask(offset);
        }

        if (BorderLength == 0)
        {
            BorderLength = MinDistanceBetweenPoints * scale;
        }

        if (BorderHeight == 0)
        {
            BorderHeight = MinDistanceBetweenPoints * scale;
        }

        AcadUtils.WriteMessageInDebug($" length {BorderLength}, height {BorderHeight} \n");
        AcadUtils.WriteMessageInDebug($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");

        var points = new[]
        {
                new Point2d(insertionPoint.X - BorderLength, insertionPoint.Y - BorderHeight),
                new Point2d(insertionPoint.X - BorderLength, insertionPoint.Y + BorderHeight),
                new Point2d(insertionPoint.X + BorderLength, insertionPoint.Y + BorderHeight),
                new Point2d(insertionPoint.X + BorderLength, insertionPoint.Y - BorderHeight)
        };

        _framePolyline = new Polyline(points.Length);

        for (var i = 0; i < points.Length; i++)
        {
            _framePolyline.AddVertexAt(i, points[i], 0, 0.0, 0.0);
        }

        _framePolyline.Closed = true;

        AcadUtils.WriteMessageInDebug($" _dbText.Position {_dbText.Position} \n");
    }

    private string ReplaceSeparator(string numericValue)
    {
        var c = NumberSeparator == NumberSeparator.Comma ? ',' : '.';
        return numericValue.Replace(',', '.').Replace('.', c);
    }

    /// <summary>
    /// Содержимое для MText в зависимости от значений
    /// </summary>
    /// <param name="isForTopText">True - содержимое для верхнего текста. False - содержимое для нижнего текста</param>
    /// <returns></returns>
    //private string GetTextContents(bool isForTopText)
    //{

    //    if (!HasTextValue())
    //    {
    //        return string.Empty;
    //    }

    //    var prefixAndDesignation = DesignationPrefix + Designation;
    //    var allWithSameHeight = $"{DesignationPrefix}{Designation} ({SheetNumber})";
    //    var allWithDifferentHeight = $"{DesignationPrefix}{Designation}{{\\H{SecondTextHeight / MainTextHeight}x;({SheetNumber})";
    //    var isSameTextHeight = Math.Abs(MainTextHeight - SecondTextHeight) < 0.0001;

    //    // Если номер не указан, то обычный текст
    //    if (string.IsNullOrEmpty(SheetNumber))
    //    {
    //        return prefixAndDesignation;
    //    }

    //    // Иначе форматированный текст для многострочного текста
    //    if (isForTopText)
    //    {
    //        // Если номер указан, но высоты текста одинаковые, то обычный текст с номером
    //        return isSameTextHeight ? allWithSameHeight : allWithDifferentHeight;
    //    }

    //    return prefixAndDesignation;
    //}
}