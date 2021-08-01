namespace mpESKD.Functions.mpViewLabel
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
    /// Разрез
    /// </summary>
    [SmartEntityDisplayNameKey("h151")]
    [SystemStyleDescriptionKey("h152")]
    public class ViewLabel : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
    {
        private readonly string _lastIntegerValue = string.Empty;

        private readonly string _lastLetterValue = string.Empty;

        #region Text entities

        private MText _mText;
        private Wipeout _mTextMask;

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
        public override double MinDistanceBetweenPoints => double.NaN;

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
        /// Тип вида (разрез или вид)
        /// </summary>
        [EntityProperty(PropertiesCategory.Content, 9, "p103", ViewLabelType.View, descLocalKey: "d103")]
        [SaveToXData]
        public ViewLabelType ViewType { get; set; }

        /// <inheritdoc />
        public override IEnumerable<Entity> Entities
        {
            get
            {
                var entities = new List<Entity>
                {
                    _mTextMask,
                    _mText,
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
        }

        /// <inheritdoc />
        public override void UpdateEntities()
        {
            try
            {
                var scale = GetScale();
                CreateEntities(InsertionPointOCS, scale);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void CreateEntities(Point3d insertionPoint, double scale)
        {
            // text
            var textContents = GetTextContents();
            if (string.IsNullOrEmpty(textContents)) 
                return;
            var textStyleId = AcadUtils.GetTextStyleIdByName(TextStyle);
            var textHeight = MainTextHeight * scale;
            _mText = new MText
            {
                TextStyleId = textStyleId,
                Contents = textContents,
                TextHeight = textHeight,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = insertionPoint
            };
            
            if (!HideTextBackground) 
                return;
            var maskOffset = TextMaskOffset * scale;
            _mTextMask = _mText.GetBackgroundMask(maskOffset);
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
            if (!IsValueCreated)
                return;
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
                    Designation = index == Invariables.AxisRusAlphabet.Count - 1 ? Invariables.AxisRusAlphabet[0] : Invariables.AxisRusAlphabet[index + 1];

                    setStandard = false;
                }
            }

            if (setStandard)
            {
                Designation = "А";
            }
        }

        /// <summary>
        /// Содержимое для MText в зависимости от значений
        /// </summary>
        /// <returns></returns>
        private string GetTextContents()
        {
            SetFirstTextOnCreation();

            if (!HasTextValue())
            {
                return string.Empty;
            }

            var prefixAndDesignation = DesignationPrefix + Designation;

            if (ViewType == ViewLabelType.Section)
            {
                prefixAndDesignation = $"{prefixAndDesignation}-{prefixAndDesignation}";
            }

            if (!string.IsNullOrEmpty(SheetNumber))
            {
                prefixAndDesignation = $"{prefixAndDesignation}{{\\H{SecondTextHeight / MainTextHeight}x;({SheetNumber})}}";
            }

            return prefixAndDesignation;
        }
    }
}