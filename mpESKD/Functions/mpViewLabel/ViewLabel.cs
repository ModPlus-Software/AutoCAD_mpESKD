namespace mpESKD.Functions.mpViewLabel
{
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
    //using Grips;
    using ModPlusAPI.Windows;
    using mpAxis;

    /// <summary>
    /// Разрез
    /// </summary>
    [SmartEntityDisplayNameKey("h79")]
    [SystemStyleDescriptionKey("h96")]
    public class ViewLabel : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
    {
        private readonly string _lastIntegerValue = string.Empty;

        private readonly string _lastLetterValue = string.Empty;

        #region Entities

        /// <summary>
        /// Средние штрихи - штрихи, создаваемые в средних точках
        /// </summary>

        #region Entities

        /// <summary>
        /// Средние штрихи - штрихи, создаваемые в средних точках
        /// </summary>
        private readonly List<Polyline> _middleStrokes = new List<Polyline>();

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
        private Wipeout _topTextMask;

        private MText _bottomMText;
        private Wipeout _bottomTextMask;

        #endregion

        #endregion

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewLabel"/> class.
        /// </summary>
        public ViewLabel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewLabel"/> class.
        /// </summary>
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public ViewLabel(ObjectId objectId)
            : base(objectId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewLabel"/> class.
        /// </summary>
        /// <param name="lastIntegerValue">Числовое значение последней созданной оси</param>
        /// <param name="lastLetterValue">Буквенное значение последней созданной оси</param>
        public ViewLabel(string lastIntegerValue, string lastLetterValue)
        {
            _lastIntegerValue = lastIntegerValue;
            _lastLetterValue = lastLetterValue;
        }

        /// <summary>
        /// Промежуточные точки
        /// </summary>
        [SaveToXData]
        public List<Point3d> MiddlePoints { get; set; } = new List<Point3d>();

        // ReSharper disable once InconsistentNaming
        private List<Point3d> MiddlePointsOCS
        {
            get
            {
                var points = new List<Point3d>();
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

        /// <inheritdoc />
        /// В примитиве не используется!
        public override string LineType { get; set; }

        /// <inheritdoc />
        /// В примитиве не используется!
        public override double LineTypeScale { get; set; }

        /// <inheritdoc />
        [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
        [SaveToXData]
        public override string TextStyle { get; set; }

        /// <inheritdoc />
        public override double MinDistanceBetweenPoints => 0.2;

        /// <summary>
        /// Длина среднего штриха (половина длины полилинии на переломе)
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 1, "h42", 8, 1, 20, descLocalKey: "d42", nameSymbol: "a")]
        [SaveToXData]
        public int MiddleStrokeLength { get; set; } = 8;

        /// <summary>
        /// Толщина штрихов
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 2, "p43", 0.5, 0, 2, descLocalKey: "d43", nameSymbol: "w")]
        [SaveToXData]
        public double StrokeWidth { get; set; } = 0.5;

        /// <summary>
        /// Длина верхнего и нижнего штриха
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 3, "p44", 10, 5, 10, descLocalKey: "d44", nameSymbol: "b")]
        [SaveToXData]
        public int StrokeLength { get; set; } = 10;

        /// <summary>
        /// Отступ полки по длине штриха в процентах
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 4, "p45", 80, 0, 100, descLocalKey: "d45", nameSymbol: "c")]
        [SaveToXData]
        public int ShelfOffset { get; set; } = 80;

        /// <summary>
        /// Длина полки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 5, "p46", 10, 5, 15, nameSymbol: "d")]
        [SaveToXData]
        public int ShelfLength { get; set; } = 10;

        /// <summary>
        /// Длина стрелки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 6, "p47", 5, 1, 8, nameSymbol: "e")]
        [SaveToXData]
        public int ShelfArrowLength { get; set; } = 5;

        /// <summary>
        /// Толщина стрелки
        /// </summary>
        [EntityProperty(PropertiesCategory.Geometry, 7, "p48", 1.5, 0.1, 5, nameSymbol: "t")]
        [SaveToXData]
        public double ShelfArrowWidth { get; set; } = 1.5;

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
        /// Обозначение разреза
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

        /// <summary>
        /// Номер листа (пишется в скобках после обозначения)
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 8, "p53", "", propertyScope: PropertyScope.Palette, descLocalKey: "d53")]
        [SaveToXData]
        [ValueToSearchBy]
        public string SheetNumber { get; set; } = string.Empty;

        /// <summary>
        /// Позиция номера листа
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 9, "p54", AxisMarkersPosition.Both, descLocalKey: "d54")]
        [SaveToXData]
        public AxisMarkersPosition SheetNumberPosition { get; set; } = AxisMarkersPosition.Both;

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

        /// <summary>
        /// Конечная точка верхней полки
        /// </summary>
        [SaveToXData]
        public Point3d TopShelfEndPoint { get; private set; }

        /// <summary>
        /// Конечная точка нижней полки
        /// </summary>
        [SaveToXData]
        public Point3d BottomShelfEndPoint { get; private set; }

        /// <summary>
        /// Направление разреза: слева на право или справа на лево. Меняется при работе ручки (<see cref="SectionReverseGrip.OnHotGrip"/>)
        /// Используется для определения положения ручки (<see cref="SectionGripPointOverrule"/>)
        /// </summary>
        [SaveToXData]
        public EntityDirection EntityDirection { get; set; } = EntityDirection.LeftToRight;

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    //_topTextMask,
                    //_bottomTextMask,
                    //_topMText,
                    //_bottomMText

                    _topTextMask,
                    _bottomTextMask,

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
                    SetImmutablePropertiesToNestedEntity(e);
                }

                return entities;
            }
        }

        /// <summary>
        /// Возвращает локализованное описание для типа <see cref="ViewLabel"/>
        /// </summary>
        public static ISmartEntityDescriptor GetDescriptor()
        {
            return TypeFactory.Instance.GetDescriptor(typeof(ViewLabel));
        }

        /// <inheritdoc />
        public override IEnumerable<Point3d> GetPointsForOsnap()
        {
            foreach (var middlePoint in MiddlePoints)
            {
                yield return middlePoint;
            }
        }

        /// <inheritdoc />
        public override void UpdateEntities()
        {
            try
            {
                var scale = GetScale();
                if (EndPointOCS.Equals(Point3d.Origin))
                {
                    // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                    MakeSimplyEntity(UpdateVariant.SetInsertionPoint, scale);
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
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X, InsertionPointOCS.Y - (MinDistanceBetweenPoints * scale), InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);


        }

        private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
        {

            // text
            var textContentsForTopText = GetTextContents(true);
            var textContentsForBottomText = GetTextContents(false);
            if (!string.IsNullOrEmpty(textContentsForTopText) && !string.IsNullOrEmpty(textContentsForBottomText))
            {
                var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
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

                var check = 1 / Math.Sqrt(2);

                // top

                _topMText.Location = insertionPoint;

                BottomDesignationPoint = _bottomMText.GeometricExtents.MinPoint.TransformBy(BlockTransform);

                if (HideTextBackground)
                {
                    var maskOffset = TextMaskOffset * scale;
                    _topTextMask = _topMText.GetBackgroundMask(maskOffset);
                    _bottomTextMask = _bottomMText.GetBackgroundMask(maskOffset);
                }
            }
            else
            {
                _bottomMText = null;
            }

            _middleStrokes.Clear();
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
                var setStandard = true;
                if (!string.IsNullOrEmpty(_lastIntegerValue))
                {
                    if (int.TryParse(_lastIntegerValue, out var i))
                    {
                        Designation = (i + 1).ToString();
                        setStandard = false;
                    }
                }
                else if (!string.IsNullOrEmpty(_lastLetterValue))
                {
                    if (Invariables.AxisRusAlphabet.Contains(_lastLetterValue))
                    {
                        var index = Invariables.AxisRusAlphabet.IndexOf(_lastLetterValue);
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
    }
}