// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpNodalLeader;

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
/// Узловая выноска
/// </summary>
[SmartEntityDisplayNameKey("h126")]
[SystemStyleDescriptionKey("h130")]
public class NodalLeader : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _cachedNodeNumber;

    #region Entities

    /// <summary>
    /// Рамка узла при типе "Прямоугольная"
    /// </summary>
    private Polyline _framePolyline;

    /// <summary>
    /// Рамка узла при типе "Круглая"
    /// </summary>
    private Circle _frameCircle;

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
    /// Initializes a new instance of the <see cref="NodalLeader"/> class.
    /// </summary>
    public NodalLeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodalLeader"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public NodalLeader(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodalLeader"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
    public NodalLeader(string lastNodeNumber)
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
    /// Точка рамки в внутренней системе координат блока
    /// </summary>
    private Point3d EndPointOCS => EndPoint.TransformBy(BlockTransform.Inverse());

    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public NodalLeaderJigState? JigState { get; set; }

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 5;

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

                _framePolyline,
                _frameCircle,
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
    /// Тип рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p82", FrameType.Round)]
    [SaveToXData]
    public FrameType FrameType { get; set; } = FrameType.Round;

    /// <summary>
    /// Радиус скругления углов прямоугольной рамки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p83", 2, 1, 10, descLocalKey: "d83", nameSymbol: "r")]
    [SaveToXData]
    public int CornerRadius { get; set; } = 2;

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
            if (JigState == NodalLeaderJigState.InsertionPoint)
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
            else if (JigState == NodalLeaderJigState.EndPoint)
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
            else if (JigState == NodalLeaderJigState.LeaderPoint)
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

                CreateEntities(InsertionPointOCS, LeaderPointOCS, scale);
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void MakeSimplyEntity(double scale)
    {
        var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);

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
            _framePolyline = null;

            try
            {
                var radius = endPoint.DistanceTo(insertionPoint);
                if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.0)
                    radius = 5 * scale;

                _frameCircle = new Circle
                {
                    Center = insertionPoint,
                    Radius = radius
                };
            }
            catch
            {
                _frameCircle = null;
            }
        }
        else
        {
            _frameCircle = null;

            var width = Math.Abs(endPoint.X - insertionPoint.X);
            var height = Math.Abs(endPoint.Y - insertionPoint.Y);
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

            _framePolyline = new Polyline(points.Length);

            for (var i = 0; i < points.Length; i++)
            {
                _framePolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
            }

            _framePolyline.Closed = true;
        }
    }

    private void CreateEntities(Point3d insertionPoint, Point3d leaderPoint, double scale)
    {
        var leaderLine = new Line(insertionPoint, leaderPoint);
        var pts = new Point3dCollection();

        if (FrameType == FrameType.Round)
        {
            _frameCircle.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
            _framePolyline.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
        }

        _leaderLine = pts.Count > 0 ? new Line(pts[0], leaderPoint) : leaderLine;


        //// Дальше код идентичен коду в SecantNodalLeader! Учесть при внесении изменений

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
            var nodeNumberPosition =
                leaderPoint +
                (Vector3d.XAxis * (shelfLength - topTextLength) / 2) +
                (Vector3d.YAxis * textVerticalOffset);

            if (_topFirstDbText != null)
            {
                _topFirstDbText.Position = nodeNumberPosition;
            }

            if (_topSecondDbText != null)
            {
                _topSecondDbText.Position = nodeNumberPosition + (Vector3d.XAxis * topFirstTextLength);
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = leaderPoint +
                                         (Vector3d.XAxis * (shelfLength - bottomTextLength) / 2) -
                                         (Vector3d.YAxis * (textVerticalOffset + bottomTextHeight));
            }
        }
        else
        {
            var sheetNumberEndPoint =
                leaderPoint -
                (Vector3d.XAxis * (shelfLength - topTextLength) / 2) +
                (Vector3d.YAxis * textVerticalOffset);

            if (_topFirstDbText != null)
            {
                _topFirstDbText.Position = sheetNumberEndPoint -
                                           (Vector3d.XAxis * (topSecondTextLength + topFirstTextLength));
            }

            if (_topSecondDbText != null)
            {
                _topSecondDbText.Position = sheetNumberEndPoint -
                                            (Vector3d.XAxis * topSecondTextLength);
            }

            if (_bottomDbText != null)
            {
                _bottomDbText.Position = leaderPoint -
                                         (Vector3d.XAxis * shelfLength) +
                                         (Vector3d.XAxis * (shelfLength - bottomTextLength) / 2) -
                                         (Vector3d.YAxis * (textVerticalOffset + bottomTextHeight));
            }
        }

        var shelfEndPoint = ShelfPosition == ShelfPosition.Right
            ? leaderPoint + (Vector3d.XAxis * shelfLength)
            : leaderPoint - (Vector3d.XAxis * shelfLength);

        if (HideTextBackground)
        {
            var offset = TextMaskOffset * scale;
            _topFirstTextMask = _topFirstDbText.GetBackgroundMask(offset);
            _topSecondTextMask = _topSecondDbText.GetBackgroundMask(offset);
            _bottomTextMask = _bottomDbText.GetBackgroundMask(offset);
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