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

public class LevelPlanMark : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{

    #region Text entities

    private MText _mText;
    private Wipeout _mTextMask;

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

    ///// <summary>
    ///// Initializes a new instance of the <see cref="LevelPlanMark"/> class.
    ///// </summary>
    ///// <param name="lastIntegerValue">Числовое значение последней созданной оси</param>
    ///// <param name="lastLetterValue">Буквенное значение последней созданной оси</param>
    //public LevelPlanMark(string lastIntegerValue, string lastLetterValue)
    //{
       
    //}

    /// <inheritdoc />
    public override string LineType { get; set; }

    /// <inheritdoc />
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => double.NaN;

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
    [EntityProperty(PropertiesCategory.Content, 6, "p51", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string Designation { get; set; } = string.Empty;

    /// <summary>
    /// Префикс обозначения
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p52", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string DesignationPrefix { get; set; } = string.Empty;

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _mTextMask,
                _mText,
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
        var textContents = "+0.000";
        if (string.IsNullOrEmpty(textContents)) 
            return;
        var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
        var textHeight = MainTextHeight * scale;
        _mText = new MText
        {
            TextStyleId = textStyleId,
            Contents = textContents,
            TextHeight = textHeight,
            Attachment = AttachmentPoint.MiddleCenter,
            Location = insertionPoint
        };
            
        if (!HideTextBackground) 
            return;
        var maskOffset = TextMaskOffset * scale;
        _mTextMask = _mText.GetBackgroundMask(maskOffset);
    }
}