// ReSharper disable InconsistentNaming

namespace mpESKD.Functions.mpFragmentMarker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Base.Abstractions;
    using Base.Attributes;
    using Base.Enums;
    using Base.Utils;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Линия фрагмента
    /// </summary>
    [SmartEntityDisplayNameKey("h145")]
    [SystemStyleDescriptionKey("h146")]
    public class FragmentMarker : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
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
        /// Initializes a new instance of the <see cref="FragmentMarker"/> class.
        /// </summary>
        public FragmentMarker()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentMarker"/> class.
        /// </summary>
        /// <param name="blockId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public FragmentMarker(ObjectId blockId)
            : base(blockId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeader"/> class.
        /// </summary>
        /// <param name="lastNodeNumber">Номер узла последней созданной узловой выноски</param>
        public FragmentMarker(string lastNodeNumber)
        {
            _lastNodeNumber = lastNodeNumber;
        }

        /// <summary>
        /// Возвращает локализованное описание для типа <see cref="FragmentMarker"/>
        /// </summary>
        public static IIntellectualEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(FragmentMarker));
        }

        #region Properties

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string LineType { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string TextStyle { get; set; }

        /// <inheritdoc />
        public override double MinDistanceBetweenPoints => 20;

        /// <summary>
        /// Радиус скругления
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "p83", 5, 1, 10, descLocalKey: "d83-1", nameSymbol: "r")]
        [SaveToXData]
        public int Radius { get; set; } = 5;

        [SaveToXData]
        public Point3d FramePoint { get; set; }

        /// <summary>
        /// Точка рамки в внутренней системе координат блока
        /// </summary>
        private Point3d FramePointOCS => FramePoint.TransformBy(BlockTransform.Inverse());

        /// <summary>
        /// Состояние Jig при создании узловой выноски
        /// </summary
        public FragmentMarkerJigState? JigState { get; set; }

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
        /// Адрес узла
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 9, "p81", "", propertyScope: PropertyScope.Palette)]
        [SaveToXData]
        [ValueToSearchBy]
        public string NodeAddress { get; set; } = string.Empty;

        #endregion

        #region Geometry

        /// <summary>Средняя точка. Нужна для перемещения  примитива</summary>
        public Point3d MiddlePoint => new Point3d(
            (InsertionPoint.X + EndPoint.X) / 2,
            (InsertionPoint.Y + EndPoint.Y) / 2,
            (InsertionPoint.Z + EndPoint.Z) / 2);

        /// <summary>
        /// Главная полилиния примитива
        /// </summary>
        private Polyline _mainPolyline;

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _topFirstTextMask,
                    _topSecondTextMask,
                    _bottomTextMask,
                    _mainPolyline,
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

        /// <inheritdoc />
        public override IEnumerable<Point3d> GetPointsForOsnap()
        {
            yield return InsertionPoint;
            yield return EndPoint;
            yield return EndPoint;
        }

        /// <inheritdoc />
        public override void UpdateEntities()
        {
            try
            {
                var length = EndPointOCS.DistanceTo(InsertionPointOCS);
                var scale = GetScale();
                if (EndPointOCS.Equals(Point3d.Origin))
                {
                    // Задание точки вставки (т.е. второй точки еще нет)
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint, scale);
                }
                else if (length < MinDistanceBetweenPoints * scale)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength, scale);
                }
                else
                {
                    // Задание второй точки
                    var pts = PointsToCreatePolyline(scale, InsertionPointOCS, EndPointOCS, out List<double> bulges);
                    FillMainPolylineWithPoints(pts, bulges);
                    CreateEntities(EndPointOCS, pts[3].ToPoint3d(), scale);
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Построение "базового" простого варианта ЕСКД примитива
        /// Тот вид, который висит на мышке при создании и указании точки вставки
        /// </summary>
        private void MakeSimplyEntity(UpdateVariant variant, double scale)
        {
            List<double> bulges;

            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);

                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, tmpEndPoint, out bulges);
                FillMainPolylineWithPoints(pts, bulges);
                CreateEntities(InsertionPoint, pts[3].ToPoint3d(), scale);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength) //// изменение вершин полилинии
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                var pts = PointsToCreatePolyline(scale, InsertionPointOCS, tmpEndPoint, out bulges);
                FillMainPolylineWithPoints(pts, bulges);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
                CreateEntities(InsertionPoint, pts[3].ToPoint3d(), scale);
                //CreateEntities(pts[2].ToPoint3d(), EndPointOCS, scale, true);
            }
        }

        /// <summary>
        /// Получение точек для построения базовой полилинии
        /// </summary>
        private Point2dCollection PointsToCreatePolyline(
            double scale, Point3d insertionPoint, Point3d endPoint, out List<double> bulges)
        {
            var length = endPoint.DistanceTo(insertionPoint);
            bulges = new List<double>();
            var pts = new Point2dCollection();

            // Первая точка начало дуги радиус Radius * scale
            // 1. От первой точки до второй проводим линию это будет вектор
            // 2. Чтобы получить точку от начала вектора, получаем нормаль и умножаем на нужную длину
            // 3. Поворачиваем полученный вектор на 90 градусов и отсчитываем необходимую высоту

            var lengthRadius = Radius * scale;

            var normal = (endPoint - insertionPoint).GetNormal();

            pts.Add(insertionPoint.ToPoint2d());
            bulges.Add(-0.4141);

            var vectorLength = normal * lengthRadius;

            var p2_v = insertionPoint + vectorLength;
            var p2 = p2_v + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p2.ToPoint2d());
            bulges.Add(0.0);

            var p3_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) - lengthRadius);
            var p3 = p3_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p3.ToPoint2d());
            bulges.Add(0.4141);

            var p4_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, length / 2);
            var p4 = p4_t.ToPoint3d() + (vectorLength * 2).RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p4.ToPoint2d());
            bulges.Add(0.4141);

            var p5_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, (length / 2) + lengthRadius);
            var p5 = p5_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p5.ToPoint2d());
            bulges.Add(0.0);

            var p6_t = ModPlus.Helpers.GeometryHelpers.GetPointToExtendLine(insertionPoint, endPoint, length - lengthRadius);
            var p6 = p6_t.ToPoint3d() + vectorLength.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
            pts.Add(p6.ToPoint2d());
            bulges.Add(-0.4141);

            pts.Add(ModPlus.Helpers.GeometryHelpers.Point2dAtDirection(insertionPoint, endPoint, insertionPoint, length));
            bulges.Add(0.0);

            return pts;
        }

        /// <summary>Изменение точек полилинии</summary>
        /// <param name="points">Коллекция 2Д точек</param>
        /// <param name="bulges">Список выпуклостей</param>
        private void FillMainPolylineWithPoints(Point2dCollection points, IList<double> bulges)
        {
            _mainPolyline = new Polyline(points.Count);
            SetImmutablePropertiesToNestedEntity(_mainPolyline);
            for (var i = 0; i < points.Count; i++)
            {
                _mainPolyline.AddVertexAt(i, points[i], bulges[i], 0.0, 0.0);
            }
        }

        private void CreateEntities(Point3d insertionPoint, Point3d leaderPoint, double scale)
        {
            //if (!drawLeader)
            //    return;

            //var leaderLine = new Line(insertionPoint, leaderPoint);
            //var pts = new Point3dCollection();
            //_frameCircle.IntersectWith(leaderLine, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            //_leaderLine = pts.Count > 0 ? new Line(pts[0], leaderPoint) : leaderLine;

            // Если drawLeader == false, то дальше код не выполнится

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


            if (!string.IsNullOrEmpty(NodeAddress))
            {
                _bottomDbText = new DBText { TextString = NodeAddress };
                _bottomDbText.SetProperties(TextStyle, secondTextHeight);
                bottomTextLength = _bottomDbText.GetLength();
                bottomTextHeight = _bottomDbText.GetHeight();
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
        #endregion
    }
}