﻿namespace mpESKD.Functions.mpRevisionMark;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using iTextSharp.awt.geom;
using ModPlusAPI.Windows;


/// <summary>
/// Маркер изменения
/// </summary>
[SmartEntityDisplayNameKey("h203")]
[SystemStyleDescriptionKey("h206")]
public class RevisionMark : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastRevisionNumber;
    private string _cachedRevisionNumber;

    private readonly List<Line> _leaderLines = new();
    private readonly List<Polyline> _revisionFramesAsPolilines= new();
    private readonly List<Circle> _revisionFramesAsCircles = new();
    private double _scale;

    #region Entities

    /// <summary>
    /// Рамка ревизии при типе "Прямоугольная"
    /// </summary>
    private Polyline _frameRevisionPolyline;

    /// <summary>
    /// Рамка узла при типе "Круглая"
    /// </summary>
    private Circle _frameRevisionCircle;

    /// <summary>
    /// Линия выноски
    /// </summary>
    private Line _leaderLine;

    /// <summary>
    /// Линия полки примечания
    /// </summary>
    private Line _noteShelfLine;

    /// <summary>
    /// Рамка номера ревизии
    /// </summary>
    private Polyline _frameRevisionTextPolyline;

    /// <summary>
    /// Текст номера ревизии
    /// </summary>
    private DBText _revisionDbText;

    /// <summary>
    /// Маскировка текста номера ревизии
    /// </summary>
    private Wipeout _revisionTextMask;

    /// <summary>
    /// Текст примечания
    /// </summary>
    private DBText _noteDbText;

    /// <summary>
    /// Маскировка текста примечания
    /// </summary>
    private Wipeout _noteTextMask;
    
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMark"/> class.
    /// </summary>
    public RevisionMark()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMark"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public RevisionMark(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMark"/> class.
    /// </summary>
    /// <param name="lastRevisionNumber">Номер ревизии последнего созданного маркера изменения</param>
    public RevisionMark(string lastRevisionNumber)
    {
        _lastRevisionNumber = lastRevisionNumber;
    }

   /// <summary>
    /// Точки выносок 
    /// </summary>
    [SaveToXData]
    public List<Point3d> LeaderPoints { get; set; } = new ();


   // todo
   /*
    /// <summary>
    /// Типы рамок ревизии для каждой точки выноски
    /// </summary>
    [SaveToXData]
    public List<RevisionFrameType> RevisionFrameTypes { get; set; } = new ();
   */

    // ReSharper disable once InconsistentNaming
    private List<Point3d> LeaderPointsOCS => LeaderPoints.Select(p => p.TransformBy(BlockTransform.Inverse())).ToList();

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 5;

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _noteShelfLine,
                _frameRevisionTextPolyline,
                _revisionDbText,
                _revisionTextMask,
                _noteDbText,
                _noteTextMask,
            };

            entities.AddRange(_leaderLines);
            entities.AddRange(_revisionFramesAsPolilines);
            entities.AddRange(_revisionFramesAsCircles);

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

    #region Geometry

    /// <summary>
    /// Для контроля видимости пункта Радиус скругления в палитре
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 10, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(CornerRadius) })]
    [SaveToXData]
    public bool CornerRadiusVisibilityDependency { get;  set; }

    // todo
    /*
    /// <summary>
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p82", RevisionFrameType.Rectangular)]
    [SaveToXData]
    public RevisionFrameType RevisionFrameType { get; set; } = RevisionFrameType.Rectangular;
    */


    /// <summary>
    /// Радиус скругления углов прямоугольной рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p83", 2, 1, 20, descLocalKey: "d83", nameSymbol: "r")]
    [SaveToXData]
    public int CornerRadius { get; set; } = 2;

    /// <summary>
    /// Облачный стиль рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p127", false)]
    [PropertyVisibilityDependency(new[] { nameof(RevisionCloudArcLength) })]
    [SaveToXData]
    public bool IsRevisionCloud { get; set; }

    /// <summary>
    /// Длина дуги облака
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p128", 5.0, 1.0, 300.0)]
    [SaveToXData]
    public double RevisionCloudArcLength { get; set; } = 5.0;

    /// <summary>
    /// Отступ текста номера ревизии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p124", 1.0, 0.0, 3.0, nameSymbol: "t")]
    [SaveToXData]
    public double RevisionTextIndent { get; set; } = 1.0;

    /// <summary>
    /// Отступ текста примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p125", 1.0, 0.0, 3.0, nameSymbol: "n")]
    [SaveToXData]
    public double NoteTextIndent { get; set; } = 1.0;

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;
    
    /// <summary>
    /// Положение маркера
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p126", MarkPosition.Right)]
    [SaveToXData]
    public MarkPosition MarkPosition { get; set; } = MarkPosition.Right;

    [SaveToXData]
    public List<Point3d> NoteShelfLinePoints { get; set; }

    #endregion

    #region Content

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста номера изменения
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p129", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double RevisionTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота текста примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p130", 2.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
    [SaveToXData]
    public double NoteTextHeight { get; set; } = 2.5;

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
    /// Текст номера изменения 
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 6, "p131", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string RevisionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Текст примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p132", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string Note { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Точки рамки текста номера ревизии, относительно точки вставки блока
    /// </summary>
    [SaveToXData] 
    public List<Point3d> FrameRevisionTextPoints { get; set; } = new () ; //Array.Empty<Point2d>();

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: InitiGetPointsForOsnapalize");
        yield return InsertionPoint;
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: UpdateEntities");

        try
        {
            _scale = GetScale();
            CreateEntities(InsertionPointOCS);
        }
        catch (Exception exception)
        {
            // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: UpdateEntities => ERROR(!)");
           // ExceptionBox.Show(exception);
        }

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: UpdateEntities => END");
    }

    private void CreateEntities(Point3d insertionPoint)
    {
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities");

        _leaderLines.Clear();
        _revisionFramesAsCircles.Clear();
        _revisionFramesAsPolilines.Clear();

        // todo Тест построения области ревизии
        /*
        this.CreateRevisionFrame(
            insertionPoint,
            new Point3d(insertionPoint.X + 20, insertionPoint.Y + 20, 0),
            RevisionFrameType.Rectangular,
            _revisionFramesAsPolilines,
            _revisionFramesAsCircles,
            _scale
        );
        */

        // todo
         SetRevisionNumberOnCreation();

        var revisionTextHeight = RevisionTextHeight * _scale;
        var noteTextHeight = NoteTextHeight * _scale;
        var revisionTextIndent = RevisionTextIndent * _scale;
        var noteTextIndent = NoteTextIndent * _scale;
        var textVerticalOffset = TextVerticalOffset * _scale;
        var isRight = MarkPosition == MarkPosition.Right;

        var revisionTextLength = 0.0;
        var noteTextLength = 0.0;

        if (!string.IsNullOrEmpty(RevisionNumber))
        {
            _revisionDbText = new DBText { TextString = RevisionNumber };
            _revisionDbText.SetProperties(TextStyle, revisionTextHeight);
            _revisionDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            revisionTextLength = _revisionDbText.GetLength();
        }
        else
        {
            _revisionDbText = null;
        }

        if (!string.IsNullOrEmpty(Note))
        {
            _noteDbText = new DBText { TextString = $"{Note}" };
            _noteDbText.SetProperties(TextStyle, noteTextHeight);
            _noteDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid,
                AttachmentPoint.MiddleCenter);
            noteTextLength = _noteDbText.GetLength();
        }
        else
        {
            _noteDbText = null;
        }

        var fullRevisionTextLength = revisionTextLength + (revisionTextIndent * 2);
        var fullNoteTextLength = _noteDbText != null ? noteTextLength + (noteTextIndent * 2) : 0;

        var fullHeight = revisionTextHeight + (textVerticalOffset * 2);
        var diffXaxis = fullHeight / Math.Tan(60.DegreeToRadian());

        var noteTextPosition = default(Point3d);
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 381");

        if (_revisionDbText != null)
        {
            // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 385");
            _revisionDbText.Position = insertionPoint;
            _revisionDbText.AlignmentPoint = insertionPoint;

            if (_noteDbText != null)
            {
                if (isRight)
                {
                    noteTextPosition = new Point3d(
                        insertionPoint.X + (fullRevisionTextLength / 2) + diffXaxis + (fullNoteTextLength / 2),
                        insertionPoint.Y - (revisionTextHeight / 2) + (noteTextHeight / 2),
                        0);
                }
                else
                {
                    noteTextPosition = new Point3d(
                        insertionPoint.X - (fullRevisionTextLength / 2) - diffXaxis - (fullNoteTextLength / 2),
                        insertionPoint.Y - (revisionTextHeight / 2) + (noteTextHeight / 2),
                        0);
                }

                _noteDbText.Position = noteTextPosition;
                _noteDbText.AlignmentPoint = noteTextPosition;
            }
        }

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => before MirrorIfNeed()");

        MirrorIfNeed(new[] { _revisionDbText, _noteDbText });

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => after MirrorIfNeed()");

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * _scale;
            _revisionTextMask = _revisionDbText.GetBackgroundMask(offset, insertionPoint);
            _noteTextMask = _noteDbText.GetBackgroundMask(offset, noteTextPosition);
        }

        var frameRevisionTextPolyline = new Polyline();

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 436");

        var toLeftBottomPoint = -(Vector2d.XAxis * ( (fullRevisionTextLength / 2) + diffXaxis) ) - (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
        var toRightBottomPoint = Vector2d.XAxis * (fullRevisionTextLength / 2) - (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
        var toRightTopPoint = Vector2d.XAxis * ((fullRevisionTextLength / 2) + diffXaxis) + (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
        var toLeftTopPoint = -(Vector2d.XAxis * (fullRevisionTextLength / 2)) + (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));

        var zeroPoint = new Point2d(0, 0);
        FrameRevisionTextPoints.Clear();
        FrameRevisionTextPoints.AddRange(new List<Point3d>
        {
           (zeroPoint + toLeftBottomPoint).ToPoint3d(),
           (zeroPoint + toRightBottomPoint).ToPoint3d(),
           (zeroPoint + toRightTopPoint).ToPoint3d(),
           (zeroPoint + toLeftTopPoint).ToPoint3d(),
        });

        var leftBottomPoint = insertionPoint.ToPoint2d() + toLeftBottomPoint;
        var rightBottomPoint = insertionPoint.ToPoint2d() + toRightBottomPoint;
        var rightTopPoint = insertionPoint.ToPoint2d() + toRightTopPoint;
        var leftTopPoint = insertionPoint.ToPoint2d() + toLeftTopPoint;


        if (_noteDbText != null)
        {
            if (isRight)
            {
                _noteShelfLine = new Line(
                    rightBottomPoint.ToPoint3d(),
                    rightBottomPoint.ToPoint3d() + (Vector3d.XAxis * (fullNoteTextLength + diffXaxis)));
            }
            else
            {
                _noteShelfLine = new Line(leftBottomPoint.ToPoint3d(),
                    leftBottomPoint.ToPoint3d() - (Vector3d.XAxis * (fullNoteTextLength)));
            }

            NoteShelfLinePoints = new List<Point3d> { _noteShelfLine.StartPoint, _noteShelfLine.EndPoint };
        }

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 459");

        frameRevisionTextPolyline.AddVertexAt(0, leftBottomPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(1, rightBottomPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(2, rightTopPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(3, leftTopPoint, 0, 0, 0);
        frameRevisionTextPolyline.Closed = true;

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 498");

        _frameRevisionTextPolyline = frameRevisionTextPolyline;

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => 502");

        /*
        FrameRevisionTextPoints.Add((new Point2d(default) + toLeftBottomPoint).ToPoint3d());
        FrameRevisionTextPoints.Add((new Point2d(default) + toRightBottomPoint).ToPoint3d());
        FrameRevisionTextPoints.Add((new Point2d(default) + toRightTopPoint).ToPoint3d());
        FrameRevisionTextPoints.Add((new Point2d(default) + toLeftTopPoint).ToPoint3d());*/

        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: CreateEntities => END");

        _leaderLines.Clear();
        for (var i = 0; i < LeaderPointsOCS.Count; i++)
        {
            if (this._noteDbText != null)
            {
                var curLeader = new Line(_noteShelfLine.EndPoint, LeaderPointsOCS[i]);
                _leaderLines.Add(curLeader);
            }
            else
            {
                /*
                List<Point2d> point2ds = new List<Point2d>();

                for (int j = 0; j < frameRevisionTextPolyline.NumberOfVertices; j++)
                {
                    point2ds.Add(frameRevisionTextPolyline.GetPoint2dAt(j));
                }
                var points2ds = this.FrameRevisionTextPoints.Select(x => x.ToPoint2d()).ToList();
                */

                var points2ds = this.FrameRevisionTextPoints.Select(x => x.ToPoint2d()).ToList();

                var curLeader = CreateLeaders(LeaderPointsOCS[i], points2ds);
                _leaderLines.Add(curLeader);
            }

            /*
            var mainNormal = (curLeader.StartPoint - curLeader.EndPoint).GetNormal();

            var arrowTypes = new ArrowBuilder(mainNormal, ArrowSize, _scale);

            if (_leaderLines[i].Length - (ArrowSize * _scale) > 0)
            {
                arrowTypes.BuildArrow((LeaderEndType)LeaderTypes[i], _leaderLines[i].EndPoint, _hatches, _leaderEndLines);
            }
            */
        }
    }

    private Line CreateLeaders(Point3d point, IEnumerable<Point2d> points)
    {
        var nearestPoint = points.OrderBy(p => p.GetDistanceTo(point.ToPoint2d())).First();
        var line = new Line(nearestPoint.ToPoint3d(), point);

        return line;
    }

    private void SetRevisionNumberOnCreation()
    {
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: SetRevisionNumberOnCreation");

        if (!IsValueCreated)
            return;

        RevisionNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastRevisionNumber, ref _cachedRevisionNumber);
        // AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMark; metod: SetRevisionNumberOnCreation => END");
    }
}