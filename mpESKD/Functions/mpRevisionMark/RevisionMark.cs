namespace mpESKD.Functions.mpRevisionMark;

using ModPlusAPI.Windows;
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

/// <summary>
/// Маркер изменения
/// </summary>
[SmartEntityDisplayNameKey("h203")]
[SystemStyleDescriptionKey("h206")]
public class RevisionMark : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastRevisionNumber;
    private string _cachedRevisionNumber;

    private readonly List<Line> _leaderLines = new ();
    private readonly List<Polyline> _revisionFramesAsPolylines = new ();
    private readonly List<Circle> _revisionFramesAsCircles = new ();
    private double _scale;

    #region Entities
    
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
   
    /// <summary>
    /// Типы рамок ревизии для каждой выноски
    /// </summary>
    [SaveToXData]
    public List<int> RevisionFrameTypes { get; set; } = new ();

    /// <summary>
    /// Точки растягивания рамки каждой выноски
    /// </summary>
    [SaveToXData]
    public List<Point3d> RevisionFrameStretchPoints { get; set; } = new ();

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
            entities.AddRange(_revisionFramesAsPolylines);
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

    /// <summary>
    /// Точки полки примечаний
    /// </summary>
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
    public List<Point3d> FrameRevisionTextPoints { get; set; } = new ();

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
    }

    /// <inheritdoc/>
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
        _revisionFramesAsCircles.Clear();
        _revisionFramesAsPolylines.Clear();
        
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

        if (_revisionDbText != null)
        {
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

        MirrorIfNeed(new[] { _revisionDbText, _noteDbText });

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * _scale;
            _revisionTextMask = _revisionDbText.GetBackgroundMask(offset, insertionPoint);
            _noteTextMask = _noteDbText.GetBackgroundMask(offset, noteTextPosition);
        }

        var frameRevisionTextPolyline = new Polyline();

        var toLeftBottomPoint = -(Vector2d.XAxis * ((fullRevisionTextLength / 2) + diffXaxis)) - (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
        var toRightBottomPoint = (Vector2d.XAxis * (fullRevisionTextLength / 2)) - (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
        var toRightTopPoint = (Vector2d.XAxis * ((fullRevisionTextLength / 2) + diffXaxis)) + (Vector2d.YAxis * ((revisionTextHeight / 2) + textVerticalOffset));
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
                _noteShelfLine = new Line(
                    leftBottomPoint.ToPoint3d(),
                    leftBottomPoint.ToPoint3d() - (Vector3d.XAxis * fullNoteTextLength));
            }

            NoteShelfLinePoints = new List<Point3d> { _noteShelfLine.StartPoint, _noteShelfLine.EndPoint };
        }

        frameRevisionTextPolyline.AddVertexAt(0, leftBottomPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(1, rightBottomPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(2, rightTopPoint, 0, 0, 0);
        frameRevisionTextPolyline.AddVertexAt(3, leftTopPoint, 0, 0, 0);
        frameRevisionTextPolyline.Closed = true;

        _frameRevisionTextPolyline = frameRevisionTextPolyline;

        for (var i = 0; i < LeaderPointsOCS.Count; i++)
        {
            if (this._noteDbText != null)
            {
                _leaderLines.Add(new Line(_noteShelfLine.EndPoint, LeaderPointsOCS[i]));
            }
            else
            {
                List<Point2d> points2ds = new ();
                for (int j = 0; j < frameRevisionTextPolyline.NumberOfVertices; j++)
                {
                    points2ds.Add(frameRevisionTextPolyline.GetPoint2dAt(j));
                }

                _leaderLines.Add(CreateLeaders(LeaderPointsOCS[i], points2ds));
            }

            var frameType = (RevisionFrameType)Enum.GetValues(typeof(RevisionFrameType))
                .GetValue(RevisionFrameTypes[i]);

            if (frameType != RevisionFrameType.None)
            {
                this.CreateRevisionFrame(
                    LeaderPointsOCS[i],
                    LeaderPoints[i],
                    RevisionFrameStretchPoints[i],
                    frameType,
                    this._revisionFramesAsPolylines,
                    this._revisionFramesAsCircles,
                    this._scale);
            }
        }

        for (int i = 0; i < _leaderLines.Count; i++)
        {
            Point3d? intersection = null;

            if (RevisionFrameTypes[i] == 1)
            {
                intersection = IsRevisionCloud 
                    ? LeaderIntersection(_revisionFramesAsPolylines[i], _leaderLines[i]) 
                    : LeaderIntersection(_revisionFramesAsCircles[i], _leaderLines[i]);
            }
            else if (RevisionFrameTypes[i] == 2)
            {
                intersection = LeaderIntersection(_revisionFramesAsPolylines[i], _leaderLines[i]);
            }

            if (intersection != null)
            {
                _leaderLines[i] = new Line(_leaderLines[i].StartPoint, intersection.Value);
            }
        }
    }

    private Point3d? LeaderIntersection(Entity entity, Entity sectionEntity)
    {
        var intersectionPoints = new Point3dCollection();
        entity.IntersectWith(sectionEntity, Intersect.OnBothOperands, intersectionPoints, IntPtr.Zero, IntPtr.Zero);

        return intersectionPoints.Count == 1 ? intersectionPoints[0] : null;
    }

    private Line CreateLeaders(Point3d point, IEnumerable<Point2d> points)
    {
        var nearestPoint = points.OrderBy(p => p.GetDistanceTo(point.ToPoint2d())).First();
        var line = new Line(nearestPoint.ToPoint3d(), point);

        return line;
    }

    private void SetRevisionNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        RevisionNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastRevisionNumber, ref _cachedRevisionNumber);
    }
}