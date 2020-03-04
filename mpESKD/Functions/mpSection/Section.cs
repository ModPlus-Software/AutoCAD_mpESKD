﻿// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpSection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Base.Attributes;
    using Base.Enums;
    using Base.Helpers;
    using ModPlusAPI.Windows;
    using Overrules.Grips;

    [IntellectualEntityDisplayNameKey("h79")]
    public class Section : IntellectualEntity
    {
        #region Constructors

        public Section()
        {
        }

        /// <inheritdoc />
        public Section(ObjectId objectId) : base(objectId)
        {
        }

        public Section(string lastIntegerValue, string lastLetterValue)
        {
            LastIntegerValue = lastIntegerValue;
            LastLetterValue = lastLetterValue;
        }

        #endregion

        #region Points and Grips

        /// <summary>
        /// Промежуточные точки
        /// </summary>
        [SaveToXData]
        public List<Point3d> MiddlePoints { get; set; } = new List<Point3d>();

        private List<Point3d> MiddlePointsOCS
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                MiddlePoints.ForEach(p => points.Add(p.TransformBy(BlockTransform.Inverse())));
                return points;
            }
        }

        /// <summary>
        /// Точка вставки верхнего текста обозначения
        /// </summary>
        [SaveToXData]
        public Point3d TopDesignationPoint { get; private set; } = Point3d.Origin;

        /// <summary>
        /// Точка вставки нижнего текста обозначения
        /// </summary>
        [SaveToXData]
        public Point3d BottomDesignationPoint { get; private set; } = Point3d.Origin;

        #endregion

        #region Properties

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string LineType { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.Content, 1, "p41", "d41", "Standard", null, null)]
        [SaveToXData]
        public override string TextStyle { get; set; }

        /// <summary>
        /// Минимальная длина
        /// </summary>
        public double SectionMinLength => 0.2;

        /// <summary>
        /// Длина среднего штриха (половина длины полилинии на переломе)
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "h42", "d42", 8, 1, 20)]
        [PropertyNameKeyInStyleEditor("p42-1")]
        [SaveToXData]
        public int MiddleStrokeLength { get; set; } = 8;

        /// <summary>
        /// Толщина штрихов
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p43", "d43", 0.5, 0, 2)]
        [PropertyNameKeyInStyleEditor("p43-1")]
        [SaveToXData]
        public double StrokeWidth { get; set; } = 0.5;

        /// <summary>
        /// Длина верхнего и нижнего штриха
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p44", "d44", 10, 5, 10)]
        [PropertyNameKeyInStyleEditor("p44-1")]
        [SaveToXData]
        public int StrokeLength { get; set; } = 10;

        /// <summary>
        /// Отступ полки по длине штриха в процентах
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p45", "d45", 80, 0, 100)]
        [PropertyNameKeyInStyleEditor("p45-1")]
        [SaveToXData]
        public int ShelfOffset { get; set; } = 80;

        /// <summary>
        /// Длина полки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 5, "p46", "d46", 10, 5, 15)]
        [PropertyNameKeyInStyleEditor("p46-1")]
        [SaveToXData]
        public int ShelfLength { get; set; } = 10;

        /// <summary>
        /// Длина стрелки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 6, "p47", "d47", 5, 1, 8)]
        [PropertyNameKeyInStyleEditor("p47-1")]
        [SaveToXData]
        public int ShelfArrowLength { get; set; } = 5;

        /// <summary>
        /// Толщина стрелки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 7, "p48", "d48", 1.5, 0.1, 5)]
        [PropertyNameKeyInStyleEditor("p48-1")]
        [SaveToXData]
        public double ShelfArrowWidth { get; set; } = 1.5;

        /// <summary>
        /// Высота текста
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 2, "p49", "d49", 3.5, 0.000000001, 1.0000E+99)]
        [PropertyNameKeyInStyleEditor("p49-1")]
        [SaveToXData]
        public double MainTextHeight { get; set; } = 3.5;

        /// <summary>
        /// Высота малого текста
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 3, "p50", "d50", 2.5, 0.000000001, 1.0000E+99)]
        [PropertyNameKeyInStyleEditor("p50-1")]
        [SaveToXData]
        public double SecondTextHeight { get; set; } = 2.5;

        /// <summary>
        /// Обозначение разреза
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 4, "p51", "d51", "", null, null, PropertyScope.Palette)]
        [SaveToXData]
        public string Designation { get; set; } = string.Empty;

        /// <summary>
        /// Префикс обозначения
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 5, "p52", "d52", "", null, null, PropertyScope.Palette)]
        [SaveToXData]
        public string DesignationPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Номер листа (пишется в скобках после обозначения)
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 6, "p53", "d53", "", null, null, PropertyScope.Palette)]
        [SaveToXData]
        public string SheetNumber { get; set; } = string.Empty;

        /// <summary>
        /// Позиция номера листа
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 7, "p54", "d54", AxisMarkersPosition.Both, null, null)]
        [SaveToXData]
        public AxisMarkersPosition SheetNumberPosition { get; set; } = AxisMarkersPosition.Both;

        private readonly string LastIntegerValue = string.Empty;

        private readonly string LastLetterValue = string.Empty;

        /// <summary>
        /// Отступ средней точки верхнего текста вдоль верхней полки
        /// </summary>
        [SaveToXData]
        public double AlongTopShelfTextOffset { get; set; } = double.NaN;

        /// <summary>
        /// Отступ средней точки верхнего текста от верхней полки (вдоль верхнего штриха)
        /// </summary>
        [SaveToXData]
        public double AcrossTopShelfTextOffset { get; set; } = double.NaN;

        /// <summary>
        /// Отступ средней точки нижнего текста вдоль нижней полки
        /// </summary>
        [SaveToXData]
        public double AlongBottomShelfTextOffset { get; set; } = double.NaN;

        /// <summary>
        /// Отступ средней точки нижнего текста от нижней полки (вдоль верхнего штриха)
        /// </summary>
        [SaveToXData]
        public double AcrossBottomShelfTextOffset { get; set; } = double.NaN;

        [SaveToXData]
        public Point3d TopShelfEndPoint { get; private set; }

        [SaveToXData]
        public Point3d BottomShelfEndPoint { get; private set; }

        /// <summary>
        /// Направление разреза: слева на право или справа на лево. Меняется при работе ручки (<see cref="SectionReverseGrip.OnHotGrip"/>)
        /// Используется для определения положения ручки (<see cref="Overrules.SectionGripPointOverrule"/>)
        /// </summary>
        [SaveToXData]
        public EntityDirection EntityDirection { get; set; } = EntityDirection.LeftToRight;

        #endregion

        #region Geometry

        /// <summary>
        /// Средние штрихи - штрихи, создаваемые в средних точках
        /// </summary>
        public readonly List<Polyline> _middleStrokes = new List<Polyline>();

        /// <summary>
        /// Верхняя полка
        /// </summary>
        private Line _topShelfLine;

        /// <summary>
        /// Стрелка верхней полки
        /// </summary>
        private Polyline _topShelfArrow;

        /// <summary>
        /// Верхний штрих
        /// </summary>
        private Polyline _topStroke;

        /// <summary>
        /// Нижняя полка
        /// </summary>
        private Line _bottomShelfLine;

        /// <summary>
        /// Стрелка нижней полки
        /// </summary>
        private Polyline _bottomShelfArrow;

        /// <summary>
        /// Нижний штрих
        /// </summary>
        private Polyline _bottomStroke;

        #region Text entities

        private MText _topMText;

        private MText _bottomMText;

        #endregion

        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _topShelfLine,
                    _topShelfArrow,
                    _topStroke,
                    _bottomShelfLine,
                    _bottomShelfArrow,
                    _bottomStroke,
                    _topMText,
                    _bottomMText
                };
                entities.AddRange(_middleStrokes);
                foreach (var e in entities)
                {
                    if (e != null)
                    {
                        SetImmutablePropertiesToNestedEntity(e);
                    }
                }

                return entities;
            }
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
                    // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint, scale);
                }
                else if (length < SectionMinLength * scale && MiddlePoints.Count == 0)
                {
                    // Задание второй точки - случай когда расстояние между точками меньше минимального
                    MakeSimplyEntity(UpdateVariant.SetEndPointMinLength, scale);
                }
                else
                {
                    // Задание любой другой точки
                    CreateEntities(InsertionPointOCS, MiddlePointsOCS, EndPointOCS, scale);
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Перестроение точек - помещение EndPoint в список
        /// </summary>
        public void RebasePoints()
        {
            if (!MiddlePoints.Contains(EndPoint))
            {
                MiddlePoints.Add(EndPoint);
            }
        }

        private void MakeSimplyEntity(UpdateVariant variant, double scale)
        {
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(InsertionPointOCS.X, InsertionPointOCS.Y - (SectionMinLength * scale), InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength)
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(InsertionPoint, EndPoint, InsertionPointOCS, SectionMinLength * scale);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        /// <summary>
        /// Создание примитивов ЕСКД элемента
        /// </summary>
        /// <param name="insertionPoint"></param>
        /// <param name="middlePoints"></param>
        /// <param name="endPoint"></param>
        /// <param name="scale"></param>
        private void CreateEntities(
            Point3d insertionPoint, List<Point3d> middlePoints,
            Point3d endPoint, double scale)
        {
            var strokesWidth = StrokeWidth * scale;

            // top and bottom strokes
            var topStrokeEndPoint = GetTopStrokeEndPoint(insertionPoint, endPoint, middlePoints, scale);
            var bottomStrokeEndPoint = GetBottomStrokeEndPoint(insertionPoint, endPoint, middlePoints, scale);

            _topStroke = new Polyline(2);
            _topStroke.AddVertexAt(0, topStrokeEndPoint.ConvertPoint3dToPoint2d(), 0.0, strokesWidth, strokesWidth);
            _topStroke.AddVertexAt(1, insertionPoint.ConvertPoint3dToPoint2d(), 0.0, strokesWidth, strokesWidth);

            _bottomStroke = new Polyline(2);
            _bottomStroke.AddVertexAt(0, endPoint.ConvertPoint3dToPoint2d(), 0.0, strokesWidth, strokesWidth);
            _bottomStroke.AddVertexAt(1, bottomStrokeEndPoint.ConvertPoint3dToPoint2d(), 0.0, strokesWidth, strokesWidth);

            var topStrokeNormalVector = (topStrokeEndPoint - insertionPoint).GetNormal();
            var bottomStrokeNormalVector = (bottomStrokeEndPoint - endPoint).GetNormal();

            // shelf lines
            var topShelfStartPoint = insertionPoint + (topStrokeNormalVector * GetShelfOffset() * scale);
            var topShelfEndPoint = topShelfStartPoint + (topStrokeNormalVector.GetPerpendicularVector() * ShelfLength * scale);
            TopShelfEndPoint = topShelfEndPoint.TransformBy(BlockTransform);
            _topShelfLine = new Line
            {
                StartPoint = topShelfStartPoint,
                EndPoint = topShelfEndPoint
            };

            var bottomShelfStartPoint = endPoint + (bottomStrokeNormalVector * GetShelfOffset() * scale);
            var bottomShelfEndPoint = bottomShelfStartPoint + (bottomStrokeNormalVector.GetPerpendicularVector().Negate() * ShelfLength * scale);
            BottomShelfEndPoint = bottomShelfEndPoint.TransformBy(BlockTransform);
            _bottomShelfLine = new Line
            {
                StartPoint = bottomShelfStartPoint,
                EndPoint = bottomShelfEndPoint
            };

            // shelf arrows
            var topShelfArrowStartPoint = topShelfStartPoint + (topStrokeNormalVector.GetPerpendicularVector() * ShelfArrowLength * scale);
            _topShelfArrow = new Polyline(2);
            _topShelfArrow.AddVertexAt(0, topShelfArrowStartPoint.ConvertPoint3dToPoint2d(), 0.0, ShelfArrowWidth * scale, 0.0);
            _topShelfArrow.AddVertexAt(1, topShelfStartPoint.ConvertPoint3dToPoint2d(), 0.0, 0.0, 0.0);

            var bottomShelfArrowStartPoint =
                bottomShelfStartPoint + (bottomStrokeNormalVector.GetPerpendicularVector().Negate() * ShelfArrowLength * scale);
            _bottomShelfArrow = new Polyline(2);
            _bottomShelfArrow.AddVertexAt(0, bottomShelfArrowStartPoint.ConvertPoint3dToPoint2d(), 0.0, ShelfArrowWidth * scale, 0.0);
            _bottomShelfArrow.AddVertexAt(1, bottomShelfStartPoint.ConvertPoint3dToPoint2d(), 0.0, 0.0, 0.0);

            // text
            var textContentsForTopText = GetTextContents(true);
            var textContentsForBottomText = GetTextContents(false);
            if (!string.IsNullOrEmpty(textContentsForTopText) && !string.IsNullOrEmpty(textContentsForBottomText))
            {
                var textStyleId = AcadHelpers.GetTextStyleIdByName(TextStyle);
                var textHeight = MainTextHeight * scale;
                _topMText = new MText
                {
                    TextStyleId = textStyleId,
                    Contents = textContentsForTopText,
                    TextHeight = textHeight,
                    Attachment = AttachmentPoint.MiddleCenter
                };

                _bottomMText = new MText
                {
                    TextStyleId = textStyleId,
                    Contents = textContentsForBottomText,
                    TextHeight = textHeight,
                    Attachment = AttachmentPoint.MiddleCenter
                };

                // TextActualHeight = _topMText.ActualHeight;
                // TextActualWidth = _topMText.ActualWidth;

                var check = 1 / Math.Sqrt(2);

                // top
                var alongShelfTextOffset = _topMText.ActualWidth / 2;
                var acrossShelfTextOffset = _topMText.ActualHeight / 2;
                if (double.IsNaN(AlongTopShelfTextOffset) && double.IsNaN(AcrossTopShelfTextOffset))
                {
                    if ((topStrokeNormalVector.X > check || topStrokeNormalVector.X < -check) &&
                        (topStrokeNormalVector.Y < check || topStrokeNormalVector.Y > -check))
                    {
                        alongShelfTextOffset = _topMText.ActualHeight / 2;
                        acrossShelfTextOffset = _topMText.ActualWidth / 2;
                    }

                    var tempPoint = topShelfEndPoint + ((topShelfStartPoint - topShelfEndPoint).GetNormal() * alongShelfTextOffset);
                    var topTextCenterPoint = tempPoint + (topStrokeNormalVector * ((2 * scale) + acrossShelfTextOffset));
                    _topMText.Location = topTextCenterPoint;
                }
                else
                {
                    var tempPoint = topShelfEndPoint +
                                    ((topShelfStartPoint - topShelfEndPoint).GetNormal() * (AlongTopShelfTextOffset + (_topMText.ActualWidth / 2)));
                    var topTextCenterPoint = tempPoint + (topStrokeNormalVector * ((2 * scale) + (AcrossTopShelfTextOffset + (_topMText.ActualHeight / 2))));
                    _topMText.Location = topTextCenterPoint;
                }

                TopDesignationPoint = _topMText.Bounds.Value.MinPoint.TransformBy(BlockTransform);

                // bottom
                alongShelfTextOffset = _bottomMText.ActualWidth / 2;
                acrossShelfTextOffset = _bottomMText.ActualHeight / 2;
                if (double.IsNaN(AlongBottomShelfTextOffset) && double.IsNaN(AcrossBottomShelfTextOffset))
                {
                    if ((bottomStrokeNormalVector.X > check || bottomStrokeNormalVector.X < -check) &&
                        (bottomStrokeNormalVector.Y < check || bottomStrokeNormalVector.Y > -check))
                    {
                        alongShelfTextOffset = _topMText.ActualHeight / 2;
                        acrossShelfTextOffset = _topMText.ActualWidth / 2;
                    }

                    var tempPoint = bottomShelfEndPoint + ((bottomShelfStartPoint - bottomShelfEndPoint).GetNormal() * alongShelfTextOffset);
                    var bottomTextCenterPoint = tempPoint + (bottomStrokeNormalVector * ((2 * scale) + acrossShelfTextOffset));
                    _bottomMText.Location = bottomTextCenterPoint;
                }
                else
                {
                    var tempPoint = bottomShelfEndPoint + ((bottomShelfStartPoint - bottomShelfEndPoint).GetNormal() *
                                    (AlongBottomShelfTextOffset + (_bottomMText.ActualWidth / 2)));
                    var bottomTextCenterPoint =
                        tempPoint + (bottomStrokeNormalVector * ((2 * scale) + (AcrossBottomShelfTextOffset + (_bottomMText.ActualHeight / 2))));
                    _bottomMText.Location = bottomTextCenterPoint;
                }

                BottomDesignationPoint = _bottomMText.Bounds.Value.MinPoint.TransformBy(BlockTransform);
            }

            _middleStrokes.Clear();

            // middle strokes
            if (MiddlePoints.Any())
            {
                var middleStrokeLength = MiddleStrokeLength * scale;

                var points = new List<Point3d> { insertionPoint };
                points.AddRange(middlePoints);
                points.Add(endPoint);

                for (var i = 1; i < points.Count - 1; i++)
                {
                    var previousPoint = points[i - 1];
                    var currentPoint = points[i];
                    var nextPoint = points[i + 1];

                    var middleStrokePolyline = new Polyline(3);
                    middleStrokePolyline.AddVertexAt(
                        0,
                        (currentPoint + ((previousPoint - currentPoint).GetNormal() * middleStrokeLength)).ConvertPoint3dToPoint2d(),
                        0, strokesWidth, strokesWidth);
                    middleStrokePolyline.AddVertexAt(1, currentPoint.ConvertPoint3dToPoint2d(), 0, strokesWidth, strokesWidth);
                    middleStrokePolyline.AddVertexAt(
                        2,
                        (currentPoint + ((nextPoint - currentPoint).GetNormal() * middleStrokeLength)).ConvertPoint3dToPoint2d(),
                        0, strokesWidth, strokesWidth);

                    _middleStrokes.Add(middleStrokePolyline);
                }
            }
        }

        /// <summary>
        /// True - есть хоть какое-то строковое значение
        /// </summary>
        public bool HasTextValue()
        {
            if (string.IsNullOrEmpty(DesignationPrefix) &&
                string.IsNullOrEmpty(Designation) &&
                string.IsNullOrEmpty(SheetNumber))
            {
                return false;
            }

            return true;
        }

        private double GetShelfOffset()
        {
            return StrokeLength * ShelfOffset / 100.0;
        }

        private Point3d GetBottomStrokeEndPoint(Point3d insertionPoint, Point3d endPoint, List<Point3d> middlePoints, double scale)
        {
            if (MiddlePoints.Any())
            {
                return endPoint + ((endPoint - middlePoints.Last()).GetNormal() * StrokeLength * scale);
            }

            return endPoint + ((endPoint - insertionPoint).GetNormal() * StrokeLength * scale);
        }

        private Point3d GetTopStrokeEndPoint(Point3d insertionPoint, Point3d endPoint, List<Point3d> middlePoints, double scale)
        {
            if (MiddlePoints.Any())
            {
                return insertionPoint + ((insertionPoint - middlePoints.First()).GetNormal() * StrokeLength * scale);
            }

            return insertionPoint + ((insertionPoint - endPoint).GetNormal() * StrokeLength * scale);
        }

        private void SetFirstTextOnCreation()
        {
            if (IsValueCreated)
            {
                bool setStandard = true;
                if (!string.IsNullOrEmpty(LastIntegerValue))
                {
                    if (int.TryParse(LastIntegerValue, out var i))
                    {
                        Designation = (i + 1).ToString();
                        setStandard = false;
                    }
                }
                else if (!string.IsNullOrEmpty(LastLetterValue))
                {
                    if (Invariables.AxisRusAlphabet.Contains(LastLetterValue))
                    {
                        var index = Invariables.AxisRusAlphabet.IndexOf(LastLetterValue);
                        if (index == Invariables.AxisRusAlphabet.Count - 1)
                        {
                            Designation = Invariables.AxisRusAlphabet[0];
                        }
                        else
                        {
                            Designation = Invariables.AxisRusAlphabet[index + 1];
                        }

                        setStandard = false;
                    }
                }

                if (setStandard)
                {
                    Designation = "А";
                }
            }
        }

        /// <summary>
        /// Содержимое для MText в зависимости от значений
        /// </summary>
        /// <param name="isForTopText">True - содержимое для верхнего текста. False - содержимое для нижнего текста</param>
        /// <returns></returns>
        private string GetTextContents(bool isForTopText)
        {
            SetFirstTextOnCreation();

            if (!HasTextValue())
            {
                return string.Empty;
            }

            var prefixAndDesignation = DesignationPrefix + Designation;
            var allWithSameHeight = $"{DesignationPrefix}{Designation} ({SheetNumber})";
            var allWithDifferentHeight = $"{DesignationPrefix}{Designation}{{\\H{SecondTextHeight / MainTextHeight}x;({SheetNumber})";
            var isSameTextHeight = Math.Abs(MainTextHeight - SecondTextHeight) < 0.0001;

            // Если номер не указан, то обычный текст
            if (string.IsNullOrEmpty(SheetNumber))
            {
                return prefixAndDesignation;
            }

            // Иначе форматированный текст для многострочного текста

            if (isForTopText)
            {
                if (SheetNumberPosition == AxisMarkersPosition.Both || SheetNumberPosition == AxisMarkersPosition.Top)
                {
                    // Если номер указан, но высоты текста одинаковые, то обычный текст с номером
                    if (isSameTextHeight)
                    {
                        return allWithSameHeight;
                    }

                    return allWithDifferentHeight;
                }

                return prefixAndDesignation;
            }

            if (SheetNumberPosition == AxisMarkersPosition.Both || SheetNumberPosition == AxisMarkersPosition.Bottom)
            {
                if (isSameTextHeight)
                {
                    return allWithSameHeight;
                }

                return allWithDifferentHeight;
            }

            return prefixAndDesignation;
        }

        #endregion
    }
}