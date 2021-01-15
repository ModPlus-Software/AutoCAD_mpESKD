// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpNodalLeader
{
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
    public class NodalLeader : SmartEntity, ITextValueEntity
    {
        private readonly string _lastNodeNumber;
        private string _cachedNodeNumber;

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
        /// Возвращает локализованное описание для типа <see cref="NodalLeader"/>
        /// </summary>
        public static IIntellectualEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(NodalLeader));
        }

        /// <summary>
        /// Точка рамки
        /// </summary>
        [SaveToXData]
        public Point3d FramePoint { get; set; }

        /// <summary>
        /// Точка рамки в внутренней системе координат блока
        /// </summary>
        private Point3d FramePointOCS => FramePoint.TransformBy(BlockTransform.Inverse());

        /// <summary>
        /// Состояние Jig при создании узловой выноски
        /// </summary>
        public NodalLeaderJigState? NodalLeaderJigState { get; set; }

        /// <inheritdoc/>
        public override double MinDistanceBetweenPoints => 5;
        
        /// <inheritdoc/>
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _framePolyline,
                    _frameCircle,
                    _leaderLine,
                    _shelfLine,
                    _topFirstDbText,
                    _topSecondDbText,
                    _bottomDbText
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
        /// Номер узла
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 4, "p79", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string NodeNumber { get; set; } = string.Empty;

        /// <summary>
        /// Номер листа
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 5, "p80", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string SheetNumber { get; set; } = string.Empty;

        /// <summary>
        /// Адрес узла
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 6, "p81", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string NodeAddress { get; set; } = string.Empty;

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
        /// Верхний второй текст (номер листа)
        /// </summary>
        private DBText _topSecondDbText;

        /// <summary>
        /// Нижний текст (адрес узла)
        /// </summary>
        private DBText _bottomDbText;

        /// <inheritdoc/>
        public override IEnumerable<Point3d> GetPointsForOsnap()
        {
            yield return InsertionPoint;
            yield return FramePoint;
            yield return EndPoint;
        }

        /// <inheritdoc/>
        public override void UpdateEntities()
        {
            try
            {
                var scale = GetScale();

                //// Задание первой точки (точки вставки). Она же точка начала отсчета
                if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.InsertionPoint)
                {
                    var tempFramePoint = new Point3d(
                        InsertionPointOCS.X + (5 * scale),
                        InsertionPointOCS.Y + (5 * scale),
                        InsertionPointOCS.Z);

                    AcadUtils.WriteMessageInDebug(
                        "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.InsertionPoint");

                    CreateEntities(InsertionPointOCS, tempFramePoint, Point3d.Origin, scale, false);
                }
                //// Задание второй точки - точки рамки. При этом в jig устанавливается EndPoint, которая по завершении
                //// будет перемещена в FramePoint
                else if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.FramePoint)
                {
                    // Так как FramePoint тут еще не задана, то свойства FrameWidth и FrameHeight нужно высчитывать из EndPoint
                    var frameHeight = Math.Abs(EndPointOCS.Y - InsertionPointOCS.Y);
                    var frameWidth = Math.Abs(EndPointOCS.X - InsertionPointOCS.X);

                    AcadUtils.WriteMessageInDebug($"On set FramePoint: frame width: {frameWidth}");
                    AcadUtils.WriteMessageInDebug($"On set FramePoint: frame height: {frameHeight}");

                    if (frameHeight <= MinDistanceBetweenPoints || frameWidth <= MinDistanceBetweenPoints)
                    {
                        var tempFramePoint = new Point3d(
                            InsertionPointOCS.X + (MinDistanceBetweenPoints * scale),
                            InsertionPointOCS.Y + (MinDistanceBetweenPoints * scale),
                            InsertionPointOCS.Z);

                        AcadUtils.WriteMessageInDebug(
                            "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.FramePoint and invalid distance");

                        CreateEntities(InsertionPointOCS, tempFramePoint, Point3d.Origin, scale, false);
                    }
                    else
                    {
                        AcadUtils.WriteMessageInDebug(
                            "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.FramePoint");

                        CreateEntities(InsertionPointOCS, EndPointOCS, Point3d.Origin, scale, false);
                    }
                }
                //// Прочие случаи
                else
                {
                    //// Если указывается EndPoint (она же точка выноски)
                    if (NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.LeaderPoint)
                    {
                        AcadUtils.WriteMessageInDebug(
                            "Create when NodalLeaderJigState == mpNodalLeader.NodalLeaderJigState.LeaderPoint");

                        CreateEntities(InsertionPointOCS, FramePointOCS, EndPointOCS, scale, true);
                    }
                    else
                    {
                        AcadUtils.WriteMessageInDebug("Create other variant");
                        CreateEntities(InsertionPointOCS, FramePointOCS, EndPointOCS, scale, true);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void CreateEntities(
            Point3d insertionPoint,
            Point3d framePoint,
            Point3d leaderPoint,
            double scale,
            bool drawLeader)
        {
            if (FrameType == FrameType.Round)
            {
                _framePolyline = null;

                try
                {
                    var radius = Math.Min(
                        Math.Abs(framePoint.X - insertionPoint.X), Math.Abs(framePoint.Y - insertionPoint.Y));
                    if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.0)
                        radius = 5 * scale;
                    
                    _frameCircle = new Circle
                    {
                        Center = insertionPoint,
                        Radius = radius
                    };

                    if (!drawLeader)
                        return;

                    var leaderLine = new Line(insertionPoint, leaderPoint);
                    var pts = new Point3dCollection();
                    _frameCircle.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                    _leaderLine = pts.Count > 0 ? new Line(pts[0], leaderPoint) : leaderLine;
                }
                catch
                {
                    _frameCircle = null;
                }
            }
            else
            {
                _frameCircle = null;

                var width = Math.Abs(framePoint.X - insertionPoint.X);
                var height = Math.Abs(framePoint.Y - insertionPoint.Y);
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

                if (!drawLeader)
                    return;

                var leaderLine = new Line(insertionPoint, leaderPoint);
                var pts = new Point3dCollection();
                _framePolyline.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                _leaderLine = pts.Count > 0 ? new Line(pts[0], leaderPoint) : leaderLine;
            }

            // Если drawLeader == false, то дальше код не выполнится

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

            if (!string.IsNullOrEmpty(NodeNumber))
            {
                _topFirstDbText = new DBText { TextString = NodeNumber };
                _topFirstDbText.SetProperties(TextStyle, mainTextHeight);
                topFirstTextLength = _topFirstDbText.GetLength();
            }

            if (!string.IsNullOrEmpty(SheetNumber))
            {
                _topSecondDbText = new DBText { TextString = $"({SheetNumber})" };
                _topSecondDbText.SetProperties(TextStyle, secondTextHeight);
                topSecondTextLength = _topSecondDbText.GetLength();
            }

            if (!string.IsNullOrEmpty(NodeAddress))
            {
                _bottomDbText = new DBText { TextString = NodeAddress };
                _bottomDbText.SetProperties(TextStyle, secondTextHeight);
                bottomTextLength = _bottomDbText.GetLength();
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
                    var sheetNumberPosition = nodeNumberPosition + (Vector3d.XAxis * topFirstTextLength);
                    _topSecondDbText.Position = sheetNumberPosition;
                }

                if (_bottomDbText != null)
                {
                    var nodeAddressPosition =
                        leaderPoint +
                        (Vector3d.XAxis * (shelfLength - bottomTextLength) / 2) -
                        (Vector3d.YAxis * textVerticalOffset);

                    _bottomDbText.Position = nodeAddressPosition;
                    _bottomDbText.SetPosition(TextHorizontalMode.TextLeft, TextVerticalMode.TextBottom, AttachmentPoint.TopLeft);
                    _bottomDbText.AlignmentPoint = nodeAddressPosition;
                }
            }
            else
            {
                var sheetNumberPosition =
                    leaderPoint -
                    (Vector3d.XAxis * (shelfLength - topTextLength) / 2) +
                    (Vector3d.YAxis * textVerticalOffset);

                if (_topSecondDbText != null)
                {
                    _topSecondDbText.Position = sheetNumberPosition;
                    _topSecondDbText.SetPosition(TextHorizontalMode.TextRight, attachmentPoint: AttachmentPoint.BaseRight);
                    _topSecondDbText.AlignmentPoint = sheetNumberPosition;
                }

                if (_topFirstDbText != null)
                {
                    var nodeNumberPosition = sheetNumberPosition - (Vector3d.XAxis * topSecondTextLength);
                    _topFirstDbText.SetPosition(TextHorizontalMode.TextRight, attachmentPoint: AttachmentPoint.BaseRight);
                    _topFirstDbText.AlignmentPoint = nodeNumberPosition;
                }

                if (_bottomDbText != null)
                {
                    var nodeAddressPosition =
                        leaderPoint -
                        (Vector3d.XAxis * (shelfLength - bottomTextLength) / 2) -
                        (Vector3d.YAxis * textVerticalOffset);

                    _bottomDbText.Position = nodeAddressPosition;
                    _bottomDbText.SetPosition(TextHorizontalMode.TextRight, TextVerticalMode.TextBottom, AttachmentPoint.TopRight);
                    _bottomDbText.AlignmentPoint = nodeAddressPosition;
                }
            }

            _shelfLine = new Line(
                leaderPoint,
                ShelfPosition == ShelfPosition.Right
                    ? leaderPoint + (Vector3d.XAxis * shelfLength)
                    : leaderPoint - (Vector3d.XAxis * shelfLength));
        }
        
        private void SetNodeNumberOnCreation()
        {
            if (!IsValueCreated)
                return;

            NodeNumber = GetNodeNumberByLastNodeNumber();
        }

        private string GetNodeNumberByLastNodeNumber()
        {
            var number = "1";

            if (!string.IsNullOrEmpty(_lastNodeNumber))
            {
                if (int.TryParse(_lastNodeNumber, out var i))
                {
                    _cachedNodeNumber = (i + 1).ToString();
                }
                else if (Invariables.AxisRusAlphabet.Contains(_lastNodeNumber))
                {
                    var index = Invariables.AxisRusAlphabet.IndexOf(_lastNodeNumber);
                    _cachedNodeNumber = index == Invariables.AxisRusAlphabet.Count - 1
                        ? Invariables.AxisRusAlphabet[0]
                        : Invariables.AxisRusAlphabet[index + 1];
                }
                else if (Invariables.AxisEngAlphabet.Contains(_lastNodeNumber))
                {
                    var index = Invariables.AxisEngAlphabet.IndexOf(_lastNodeNumber);
                    _cachedNodeNumber = index == Invariables.AxisEngAlphabet.Count - 1
                        ? Invariables.AxisEngAlphabet[0]
                        : Invariables.AxisEngAlphabet[index + 1];
                }
            }

            if (!string.IsNullOrEmpty(_cachedNodeNumber))
            {
                number = _cachedNodeNumber;
            }

            return number;
        }
    }
}
