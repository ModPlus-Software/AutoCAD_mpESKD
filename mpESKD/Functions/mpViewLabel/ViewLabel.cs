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
    using Grips;
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
        private readonly List<Polyline> _middleStrokes = new List<Polyline>();

        #region Text entities

        private MText _topMText;
        private Wipeout _topTextMask;

        private MText _bottomMText;
        private Wipeout _bottomTextMask;

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
                    _topTextMask,
                    _bottomTextMask,
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
            yield return InsertionPoint;
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
                else if (length < MinDistanceBetweenPoints * scale && MiddlePoints.Count == 0)
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
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X, InsertionPointOCS.Y - (MinDistanceBetweenPoints * scale), InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
            }
            else if (variant == UpdateVariant.SetEndPointMinLength)
            {
                /* Изменение базовых примитивов в момент указания второй точки
                * при условии что расстояние от второй точки до первой больше минимального допустимого
                */
                var tmpEndPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                    InsertionPoint, EndPoint, InsertionPointOCS, MinDistanceBetweenPoints * scale);
                CreateEntities(InsertionPointOCS, MiddlePointsOCS, tmpEndPoint, scale);
                EndPoint = tmpEndPoint.TransformBy(BlockTransform);
            }
        }

        private void CreateEntities(Point3d insertionPoint, List<Point3d> middlePoints, Point3d endPoint, double scale)
        {


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