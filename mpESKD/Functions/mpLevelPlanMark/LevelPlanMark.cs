using System.Diagnostics;

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
    public override double MinDistanceBetweenPoints => 9;

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
    [EntityProperty(PropertiesCategory.Content, 6, "p51", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string PlanMark { get; set; } = "0.000";

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
        if (string.IsNullOrEmpty(PlanMark)) PlanMark = "0.000";
        _dbText = new DBText()
        {
            Position = insertionPoint,
            TextStyleId = textStyleId,
            TextString = PlanMark,
            Height = textHeight,
            Justify = AttachmentPoint.MiddleCenter,
            AlignmentPoint = insertionPoint
        };
        AcadUtils.WriteMessageInDebug($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");
        //_dbText.SetProperties(TextStyle, textHeight);
        Debug.Print($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");
        AcadUtils.WriteMessageInDebug($"_dbtext text {_dbText.TextString}");
        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _dbTextMask = _dbText.GetBackgroundMask(offset);
        }

        var width = _dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X;
        
        AcadUtils.WriteMessageInDebug($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");
        Debug.Print($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");
        var height = _dbText.GetHeight();
        if (width == 0)
        {
            width = (MinDistanceBetweenPoints * scale);
        }

        //if (width == -0.2196699141101135)
        //{
        //    width = -9.3106601717789772;
        //}

        if (height == 0)
        {
            height = MinDistanceBetweenPoints * scale;
        }

        AcadUtils.WriteMessageInDebug($"  width {width}, height {height} \n");
        AcadUtils.WriteMessageInDebug($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");

        var points = new[]
        {
                new Point2d(insertionPoint.X - width, insertionPoint.Y - height),
                new Point2d(insertionPoint.X - width, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width , insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width , insertionPoint.Y - height)
        };

        _framePolyline = new Polyline(points.Length);

        for (var i = 0; i < points.Length; i++)
        {
            _framePolyline.AddVertexAt(i, points[i], 0, 0.0, 0.0);
        }

        _framePolyline.Closed = true;

        AcadUtils.WriteMessageInDebug($" _dbText.Position {_dbText.Position} \n");
        AcadUtils.WriteMessageInDebug($" _____________________________ \n");
        Debug.Print($" _dbText.MaxPoint.X {_dbText.GeometricExtents.MaxPoint.X}  - _dbText.MinPointX {_dbText.GeometricExtents.MinPoint.X} = {_dbText.GeometricExtents.MaxPoint.X - _dbText.GeometricExtents.MinPoint.X}\n");
    }
}