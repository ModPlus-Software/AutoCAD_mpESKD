namespace mpESKD.Functions.mpRevisionMark;

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
using ModPlusAPI.Windows;

/// <summary>
/// Узловая выноска
/// </summary>
[SmartEntityDisplayNameKey("h203")]
[SystemStyleDescriptionKey("h206")]
public class RevisionMark : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;

    #region Entities

    /// <summary>
    /// Рамка ревизии при типе "Прямоугольная"
    /// </summary>
    private Polyline _frameRevisionPolyline;

    /// <summary>
    /// Рамка узла при типе "Круглая"
    /// </summary>
    private Circle _frameRevisionCircle;

    ///// <summary>
    ///// Рамка узла при типе "Круглая" и стиле облака ревизии
    ///// </summary>
    //private Polyline _frameRevisionЗщCircle;

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
    private Polyline _frameRevisionText;

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
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public RevisionMark(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    /// <summary>
    /// Точка рамки
    /// </summary>
    [SaveToXData]
    public Point3d LeaderPoint { get; set; }

    private Point3d LeaderPointOCS => LeaderPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public RevisionMarkJigState? JigState { get; set; }

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 5;

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _frameRevisionPolyline,
                _frameRevisionCircle,
                _leaderLine,
                _noteShelfLine,
                _frameRevisionText,
                _revisionDbText,
                _revisionTextMask,
                _noteDbText,
                _noteTextMask,
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


    #region Geometry
    /// <summary>
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p82", FrameType.Round)]
    [SaveToXData]
    public FrameType FrameType { get; set; } = FrameType.Round;

    /// <summary>
    /// Радиус скругления углов прямоугольной рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p83", 2, 1, 10, descLocalKey: "d83", nameSymbol: "r")]
    [SaveToXData]
    public int CornerRadius { get; set; } = 2;

    /// <summary>
    /// Отступ текста номера ревизии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p61", 1.0, 0.0, 3.0, nameSymbol: "o")]
    [SaveToXData]
    public double RevisionTextIndent { get; set; } = 1.0;


    /// <summary>
    /// Отступ текста примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p61", 1.0, 0.0, 3.0, nameSymbol: "o")]
    [SaveToXData]
    public double NoteTextIndent { get; set; } = 1.0;


    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p62", 1.0, 0.0, 3.0, nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 1.0;
    
    /// <summary>
    /// Положение маркера
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p78", MarkPosition.Right)]
    [SaveToXData]
    public MarkPosition MarkPosition { get; set; } = MarkPosition.Right;

    /// <summary>
    /// Стиль рамки ревизии как облака
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p123", false)]
    [PropertyVisibilityDependency(new[] { nameof(RevisionCloudArcRadius) })]
    [SaveToXData]
    public bool IsRevisionCloud { get; set; }

    /// <summary>
    /// Вертикальный отступ текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 8, "p62", 2, 2, 500, nameSymbol: "v")]
    [SaveToXData]
    public int RevisionCloudArcRadius { get; set; } = 2;



    #endregion

    #region Content

    /// <inheritdoc/>
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста номера изменения
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p49", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h1")]
    [SaveToXData]
    public double RevisionTextHeight { get; set; } = 3.5;

    /// <summary>
    /// Высота текста примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 2.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
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
    [EntityProperty(PropertiesCategory.Content, 6, "p79", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string RevisionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Текст примечания
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p81", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string Note { get; set; } = string.Empty;
    
    #endregion

    /// <inheritdoc/>
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
        yield return LeaderPoint;
    }

    /// <inheritdoc/>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();
            var length = EndPointOCS.DistanceTo(InsertionPointOCS);
            //// Задание первой точки (точки вставки). Она же точка начала отсчета
            if (JigState == RevisionMarkJigState.InsertionPoint)
            {
                var tempFramePoint = new Point3d(
                    InsertionPointOCS.X + (5 * scale),
                    InsertionPointOCS.Y + (5 * scale),
                    InsertionPointOCS.Z);
                PointsToCreatePolyline(scale, InsertionPointOCS, tempFramePoint);
                EndPoint = tempFramePoint.TransformBy(BlockTransform);
            }
            //// Задание второй точки - точки рамки. При этом в jig устанавливается EndPoint, которая по завершении
            //// будет перемещена в EndPoint
            else if (JigState == RevisionMarkJigState.EndPoint)
            {
                // Так как FramePoint тут еще не задана, то свойства FrameWidth и FrameHeight нужно высчитывать из EndPoint
                var frameHeight = Math.Abs(EndPointOCS.Y - InsertionPointOCS.Y);
                var frameWidth = Math.Abs(EndPointOCS.X - InsertionPointOCS.X);

                if (frameHeight <= MinDistanceBetweenPoints || frameWidth <= MinDistanceBetweenPoints || length <= MinDistanceBetweenPoints)
                {
                    MakeSimplyEntity(scale);
                }
                else
                {
                    PointsToCreatePolyline(scale, InsertionPointOCS, EndPoint);
                }
            }
            else if (JigState == RevisionMarkJigState.LeaderPoint)
            {
                CreateEntities(InsertionPointOCS, LeaderPointOCS, scale);
            }
            //// Прочие случаи (включая указание точки выноски)
            else
            {
                if (length <= MinDistanceBetweenPoints)
                {
                    MakeSimplyEntity(scale);
                }
                else
                {
                    PointsToCreatePolyline(scale, InsertionPointOCS, EndPointOCS);
                }

                AcadUtils.WriteMessageInDebug($"328");// todo

                CreateEntities(InsertionPointOCS, LeaderPointOCS, scale);

                AcadUtils.WriteMessageInDebug($"332");// todo
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);

            AcadUtils.WriteMessageInDebug($"{exception.StackTrace}");
        }
    }

    private void MakeSimplyEntity(double scale)
    {
        var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
        if (InsertionPointOCS.IsEqualTo(EndPointOCS))
        {
            tmpEndPoint = new Point3d(0, MinDistanceBetweenPoints * scale, 0);
        }

        PointsToCreatePolyline(scale, InsertionPointOCS, tmpEndPoint);
        EndPoint = tmpEndPoint.TransformBy(BlockTransform);
    }

    /// <summary>
    /// Получение точек для построения базовой полилинии
    /// </summary>
    private void PointsToCreatePolyline(double scale, Point3d insertionPoint, Point3d endPoint)
    {
        if (FrameType == FrameType.Round)
        {
            _frameRevisionPolyline = null;

            var send = "ERROR";
            var sendI = 0;

            try
            {
                var radius = endPoint.DistanceTo(insertionPoint);
                if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.0)
                    radius = MinDistanceBetweenPoints * scale;

                if (!IsRevisionCloud)
                {
                    _frameRevisionCircle = new Circle
                    {
                        Center = insertionPoint,
                        Radius = radius
                    };
                }
                else
                {
                    _frameRevisionCircle = null;
                    var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

                    var cloudArcPoints = RevisionCloud.GetArcPointsOfSegment(
                        insertionPoint,
                        radius,
                        RevisionCloudArcRadius * scale);

                    cloudArcPoints.Add(insertionPoint.ToPoint2d() + Vector2d.XAxis * radius);

                    AcadUtils.WriteMessageInDebug($"cloudArcPoints count: {cloudArcPoints.Count}");

                    if (cloudArcPoints != null)
                    {
                        _frameRevisionPolyline = new Polyline(cloudArcPoints.Count);

                        for (int i = 0; i < cloudArcPoints.Count; i++)
                        {
                            _frameRevisionPolyline.AddVertexAt(i, cloudArcPoints[i], bevelBulge, 0, 0);
                            AcadUtils.WriteMessageInDebug($"Vertex[{i}] created");
                            sendI++;
                        }
                    }

                    AcadUtils.WriteMessageInDebug($"_frameRevisionPolyline DONE");
                }

                AcadUtils.WriteMessageInDebug($"PointsToCreatePolyline DONE");
            }
            catch
            {
                AcadUtils.WriteMessageInDebug($"{send} - {sendI}");

                _frameRevisionCircle = null;
                _frameRevisionPolyline = null;
            }
        }
        else
        {
            _frameRevisionCircle = null;

            var width = Math.Abs(endPoint.X - insertionPoint.X);
            var height = Math.Abs(endPoint.Y - insertionPoint.Y);
            if (width == 0)
            {
                width = MinDistanceBetweenPoints * scale;
            }

            if (height == 0)
            {
                height = MinDistanceBetweenPoints * scale;
            }

            var cornerRadius = CornerRadius * scale;

            if (((width * 2) - (cornerRadius * 2)) < (1 * scale) ||
                ((height * 2) - (cornerRadius * 2)) < (1 * scale))
            {
                var minSize = Math.Min(width * 2, height * 2);
                cornerRadius = (int)((minSize - (1 * scale)) / 2);
            }

            var points = new[]
            {
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y - height),
                new Point2d(insertionPoint.X - width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X - width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X - width + cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y + height),
                new Point2d(insertionPoint.X + width, insertionPoint.Y + height - cornerRadius),
                new Point2d(insertionPoint.X + width, insertionPoint.Y - height + cornerRadius),
                new Point2d(insertionPoint.X + width - cornerRadius, insertionPoint.Y - height)
            };

            var bevelBulge = Math.Tan((90 / 4).DegreeToRadian());

            if (!IsRevisionCloud)
            {
                var bulges = new[]
                {
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0,
                    -bevelBulge,
                    0.0
                };

                _frameRevisionPolyline = new Polyline(points.Length);

                for (var i = 0; i < points.Length; i++)
                {
                    _frameRevisionPolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
                }

                _frameRevisionPolyline.Closed = true;
            }
            else
            {
                _frameRevisionPolyline = null;
                var arcFramePoints = new List<Point2d>();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var segmentStartPoint = points[i];
                    var segmentEndPoint = points[i + 1];    

                    arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                        segmentStartPoint,
                        segmentEndPoint,
                        RevisionCloudArcRadius * scale));
                }
                
                arcFramePoints.AddRange(RevisionCloud.GetArcPointsOfSegment(
                    arcFramePoints.Last(),
                    arcFramePoints.First(),
                    RevisionCloudArcRadius * scale));

                var arcFramePointsDistinct = arcFramePoints.Skip(1).Distinct();


                arcFramePoints = Enumerable.Repeat(arcFramePoints[0], 1)
                    .Concat(arcFramePointsDistinct).ToList();

                var correctFramePoints = new List<Point2d>();
                var isContinue = false;

                for (int i = 0; i < arcFramePoints.Count -1 ; i++)
                {
                    if (isContinue)
                    {
                        isContinue= false;
                        continue;
                    }

                    var currentPoint= arcFramePoints[i];
                    var nextPoint = arcFramePoints[i + 1];

                    var distance = currentPoint.GetDistanceTo(nextPoint);

                    if (distance < 2 * RevisionCloudArcRadius)
                    {
                        var middlePoint = GeometryUtils.GetMiddlePoint2d(currentPoint, nextPoint);
                        correctFramePoints.Add(middlePoint);
                        isContinue = true;
                    }
                    else
                    {
                        correctFramePoints.Add(arcFramePoints[i]);
                        isContinue = true;
                    }

                }

                correctFramePoints.Add(correctFramePoints[0]);

                _frameRevisionPolyline = new Polyline(correctFramePoints.Count);

                for (int i = 0; i < correctFramePoints.Count; i++)
                {
                    _frameRevisionPolyline.AddVertexAt(i, correctFramePoints[i], -bevelBulge, 0, 0);
                }
            }
        }
    }

    private void CreateEntities(Point3d insertionPoint, Point3d leaderPoint, double scale)
    {
        var leaderLine = new Line(insertionPoint, leaderPoint);
        var pts = new Point3dCollection();

        if (FrameType == FrameType.Round && !IsRevisionCloud)
        {
            _frameRevisionCircle.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
            _frameRevisionPolyline.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
        }

        
        _leaderLine = pts.Count > 0 ? new Line(pts[0], leaderPoint) : leaderLine;

        SetNodeNumberOnCreation();

        var revisionTextHeight = RevisionTextHeight * scale;
        var noteTextHeight = NoteTextHeight * scale;
        var revisionTextIndent = RevisionTextIndent * scale;
        var noteTextIndent = NoteTextIndent * scale;
        var textVerticalOffset = TextVerticalOffset * scale;
        var isRight = MarkPosition == MarkPosition.Right;

        var revisionTextLength = 0.0;
        var noteTextLength = 0.0;

        if (!string.IsNullOrEmpty(RevisionNumber))
        {
            _revisionDbText = new DBText { TextString = RevisionNumber };
            _revisionDbText.SetProperties(TextStyle, revisionTextHeight);
            _revisionDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
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
            _noteDbText.SetPosition(TextHorizontalMode.TextCenter, TextVerticalMode.TextVerticalMid, AttachmentPoint.MiddleCenter);
            noteTextLength = _noteDbText.GetLength();
        }
        else
        {
            _noteDbText = null;
        }

        double fullRevisionTextLength = revisionTextLength + revisionTextIndent * 2;
        double fullNoteTextLength = _noteDbText != null ? noteTextLength + noteTextIndent * 2 : 0;

        double fullHeight = revisionTextHeight + textVerticalOffset * 2;
        double diffXaxis = fullHeight / Math.Tan(60.DegreeToRadian());

        Point3d revisionTextPosition;
        var noteTextosition = default(Point3d);

        if (isRight)
        {
            //AcadUtils.WriteMessageInDebug("IS RIGHT");
            revisionTextPosition = new Point3d(leaderPoint.X + fullRevisionTextLength / 2 + fullNoteTextLength + diffXaxis, leaderPoint.Y + fullHeight / 2, 0);

           if (_revisionDbText != null)
            {
                _revisionDbText.Position = revisionTextPosition;
                _revisionDbText.AlignmentPoint = revisionTextPosition;
            }

            if (_noteDbText != null)
            {
                noteTextosition = new Point3d(leaderPoint.X + fullNoteTextLength / 2, revisionTextPosition.Y, 0);

               _noteDbText.Position = noteTextosition;
                _noteDbText.AlignmentPoint = noteTextosition;
            }
        }
        else
        {
            if (_noteDbText != null)
            {
                revisionTextPosition = new Point3d(leaderPoint.X - fullRevisionTextLength / 2 - fullNoteTextLength - diffXaxis, leaderPoint.Y + fullHeight / 2, 0);
            }   
            else
            {
                revisionTextPosition = new Point3d(leaderPoint.X - fullRevisionTextLength / 2 - fullNoteTextLength , leaderPoint.Y + fullHeight / 2, 0);
            }

            if (_revisionDbText != null)
            {
                _revisionDbText.Position = revisionTextPosition;
                _revisionDbText.AlignmentPoint = revisionTextPosition;
            }

            if (_noteDbText != null)
            {
                noteTextosition = new Point3d(leaderPoint.X - fullNoteTextLength / 2, revisionTextPosition.Y, 0);

                _noteDbText.Position = noteTextosition;
                _noteDbText.AlignmentPoint = noteTextosition;
            }
        }

        MirrorIfNeed(new[] { _revisionDbText, _noteDbText });

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _revisionTextMask = _revisionDbText.GetBackgroundMask(offset, revisionTextPosition);
            _noteTextMask = _noteDbText.GetBackgroundMask(offset, noteTextosition);
        }

        var polylineRevisionFrame = new Polyline();
        Point2d leftBottomPoint;
        Point2d rightBottomPoint;
        Point2d rightTopPoint;
        Point2d leftTopPoint;

        if (isRight)
        {
            leftBottomPoint = leaderPoint.ToPoint2d() + Vector2d.XAxis * fullNoteTextLength;
            rightBottomPoint = leftBottomPoint + Vector2d.XAxis * (fullRevisionTextLength + diffXaxis);
            rightTopPoint = (rightBottomPoint + (Vector2d.XAxis * diffXaxis)) + Vector2d.YAxis * fullHeight;
            leftTopPoint = (leftBottomPoint + (Vector2d.XAxis * diffXaxis)) + Vector2d.YAxis * fullHeight;

            _noteShelfLine = new Line(leaderPoint, leftBottomPoint.ToPoint3d());
        }
        else
        {
            if (_noteDbText != null)
            {
                rightBottomPoint = leaderPoint.ToPoint2d() - Vector2d.XAxis * (fullNoteTextLength + diffXaxis);
            }
            else
            {
                rightBottomPoint = leaderPoint.ToPoint2d() - Vector2d.XAxis * fullNoteTextLength;
            }

            rightTopPoint = (rightBottomPoint + Vector2d.XAxis * (diffXaxis)) + (Vector2d.YAxis * fullHeight);
            leftTopPoint = rightTopPoint - Vector2d.XAxis * (fullRevisionTextLength + diffXaxis);
            leftBottomPoint = rightBottomPoint - Vector2d.XAxis * (fullRevisionTextLength + diffXaxis);

            _noteShelfLine = new Line(leaderPoint, rightBottomPoint.ToPoint3d());
        }

        polylineRevisionFrame.AddVertexAt(0, leftBottomPoint, 0, 0, 0);
        polylineRevisionFrame.AddVertexAt(1, rightBottomPoint, 0, 0, 0);
        polylineRevisionFrame.AddVertexAt(2, rightTopPoint, 0, 0, 0);
        polylineRevisionFrame.AddVertexAt(3, leftTopPoint, 0, 0, 0);
        polylineRevisionFrame.Closed = true;

        _frameRevisionText = polylineRevisionFrame;
    }

    private void SetNodeNumberOnCreation()
    {
        if (!IsValueCreated)
            return;

        RevisionNumber = EntityUtils.GetNodeNumberByLastNodeNumber(_lastNodeNumber, ref _cachedNodeNumber);
    }
}