using System.Diagnostics;

namespace mpESKD.Functions.mpView
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
    using Grips;
    using ModPlusAPI.Windows;
    using mpAxis;

    /// <summary>
    /// Разрез
    /// </summary>
    [SmartEntityDisplayNameKey("h79")]
    [SystemStyleDescriptionKey("h96")]
    public class View : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
    {
        private readonly string _lastIntegerValue = string.Empty;

        private readonly string _lastLetterValue = string.Empty;

        #region Entities

        /// <summary>
        /// Верхняя полка
        /// </summary>
        private Line _shelfLine;

        /// <summary>
        /// Стрелка верхней полки
        /// </summary>
        private Polyline _shelfArrow;

        #region Text entities

        private MText _mText;
        private Wipeout _textMask;

        #endregion

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class.
        /// </summary>
        public View()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class.
        /// </summary>
        /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        public View(ObjectId objectId)
            : base(objectId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class.
        /// </summary>
        /// <param name="lastIntegerValue">Числовое значение последней созданной оси</param>
        /// <param name="lastLetterValue">Буквенное значение последней созданной оси</param>
        public View(string lastIntegerValue, string lastLetterValue)
        {
            _lastIntegerValue = lastIntegerValue;
            _lastLetterValue = lastLetterValue;
        }

        /// <summary>
        /// Точка вставки верхнего текста обозначения
        /// </summary>
        [SaveToXData]
        public Point3d TextDesignationPoint { get; private set; } = Point3d.Origin;


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
        //[EntityProperty(PropertiesCategory.Geometry, 1, "p42", 8, 1, 20, descLocalKey: "d42", nameSymbol: "a")]
        //[SaveToXData]
        //public int MiddleStrokeLength { get; set; } = 8;


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
        /// Конечная точка верхней полки
        /// </summary>
        [SaveToXData]
        public Point3d TopShelfEndPoint { get; private set; }

        /// <summary>
        /// Точка выноски
        /// </summary>
        [SaveToXData]
        public Point3d TextPoint { get; set; }
        /// <summary>
        /// Направление разреза: слева на право или справа на лево. Меняется при работе ручки (<see cref="ViewReverseGrip.OnHotGrip"/>)
        /// Используется для определения положения ручки (<see cref="ViewGripPointOverrule"/>)
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
                    _textMask,
                    _shelfLine,
                    _shelfArrow,
                    _mText
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
                    AcadUtils.Editor.WriteMessage($"При изменении TextPoint {TextPoint} \n");
                    CreateEntities(InsertionPointOCS, EndPointOCS, scale);
                }

                //// Задание первой точки (точки вставки). Она же точка начала отсчета
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }


        private void MakeSimplyEntity(UpdateVariant variant, double scale)
        {
            if (variant == UpdateVariant.SetInsertionPoint)
            {
                /* Изменение базовых примитивов в момент указания второй точки при условии второй точки нет
                 * Примерно аналогично созданию, только точки не создаются, а меняются
                */
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X, InsertionPointOCS.Y + (ShelfLength * scale), InsertionPointOCS.Z);
                AcadUtils.Editor.WriteMessage("первая точка не задана");
                CreateEntities(InsertionPointOCS, tmpEndPoint, scale);

            }
            else
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, ShelfLength * scale);
                UpdateEntities();
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        private void CreateEntities(Point3d insertionPoint, Point3d endPoint, double scale)
        {

            var topStrokeNormalVector = (endPoint - insertionPoint).GetNormal();

            // shelf line линия стрелки

            var topShelfEndPoint = insertionPoint + (topStrokeNormalVector * ShelfLength * scale);
            TopShelfEndPoint = topShelfEndPoint.TransformBy(BlockTransform);

            var topStrokeArrowEndPoint = (endPoint - insertionPoint).GetNormal();
            var tmpEndPoint = insertionPoint + (topStrokeArrowEndPoint * ShelfLength * scale);

            _shelfLine = new Line
            {
                StartPoint = insertionPoint,
                EndPoint = tmpEndPoint
            };

            // shelf arrow
            var topShelfArrowStartPoint = insertionPoint + (topStrokeArrowEndPoint * ShelfArrowLength * scale);
            _shelfArrow = new Polyline(2);
            _shelfArrow.AddVertexAt(0, topShelfArrowStartPoint.ToPoint2d(), 0.0, ShelfArrowWidth * scale, 0.0);
            _shelfArrow.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, 0.0, 0.0);

            // text
            var textContentsForTopText = GetTextContents(true);

            if (!string.IsNullOrEmpty(textContentsForTopText))
            {
                var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
                var textHeight = MainTextHeight * scale;
                _mText = new MText
                {
                    TextStyleId = textStyleId,
                    Contents = textContentsForTopText,
                    TextHeight = textHeight,
                    Attachment = AttachmentPoint.MiddleCenter
                };

                var check = 1 / Math.Sqrt(2);

                // text
                var alongShelfTextOffset = _mText.ActualWidth / 2;
                var acrossShelfTextOffset = _mText.ActualHeight / 2;
                if (double.IsNaN(AlongTopShelfTextOffset) && double.IsNaN(AcrossTopShelfTextOffset))
                {
                    //if ((topStrokeNormalVector.X > check || topStrokeNormalVector.X < -check) &&
                    //    (topStrokeNormalVector.Y < check || topStrokeNormalVector.Y > -check))
                    //{
                    //    alongShelfTextOffset = _mText.ActualHeight / 2;
                    //    acrossShelfTextOffset = _mText.ActualWidth / 2;
                    //}

                    var tempPoint = topShelfEndPoint - (topStrokeNormalVector * alongShelfTextOffset * 2);
                    var normalPerpenducular = topStrokeNormalVector.GetPerpendicularVector();
                    var topTextCenterPoint = tempPoint + (normalPerpenducular * scale * acrossShelfTextOffset * 2);

                    //var textPerpendicularVector = (AlongTopShelfTextOffset * topStrokeNormalVector);
                    //var tempPoint = topShelfEndPoint - textPerpendicularVector * alongShelfTextOffset;

                    TextPoint = topTextCenterPoint;

                    //var topTextCenterPoint = tempPoint + (topStrokeNormalVector * ((2 * scale) + (AcrossTopShelfTextOffset + (_mText.ActualHeight / 2))));
                    _mText.Location = TextPoint;
                    
                    AcadUtils.WriteMessageInDebug($"topTextCenterPoint - {tempPoint} \n");
                }
                //else
                //{
                //    AcadUtils.WriteMessageInDebug("else ");
                //    var tempPoint = topShelfEndPoint + ((insertionPoint - topShelfEndPoint).GetNormal() * (AlongTopShelfTextOffset + (_mText.ActualWidth / 2)));
                //    var topTextCenterPoint = tempPoint + (topStrokeNormalVector * (scale + (AcrossTopShelfTextOffset + (_mText.ActualHeight / 2))));
                //    _mText.Location = tmpEndPoint;
                //}

                TextDesignationPoint = _mText.GeometricExtents.MinPoint.TransformBy(BlockTransform);

                if (HideTextBackground)
                {
                    var maskOffset = TextMaskOffset * scale;
                    _textMask = _mText.GetBackgroundMask(maskOffset);
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