namespace mpESKD.Functions.mpNodeLabel;

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
/// Обозначение узла
/// </summary>
[SmartEntityDisplayNameKey("h197")]
[SystemStyleDescriptionKey("h200")]
public class NodeLabel : SmartEntity, ITextValueEntity, IWithDoubleClickEditor
{
    private readonly string _lastNodeNumber;
    private string _sheetNumber = string.Empty;

    #region Entities

    /// <summary>
    /// Линия разделения текста с номером узла и текста с номером листа
    /// </summary>
    private Line _line;

    /// <summary>
    /// Внутренняя окружность обозначения узла
    /// </summary>
    private Circle _innerCircle;

    /// <summary>
    /// Внешняя окружность обозначения узла
    /// </summary>
    private Circle _outerCircle;

    /// <summary>
    /// Верхний тест с номером узла
    /// </summary>
    private MText _topText;

    /// <summary>
    /// Нижний текст с номером узла
    /// </summary>
    private MText _bottomText;

    /// <summary>
    ///  Маскировка верхнего текста с номером узла
    /// </summary>
    private Wipeout _topTextMask;

    /// <summary>
    /// Маскировка ниженего текста с номером листа
    /// </summary>
    private Wipeout _bottomTextMask;

    #endregion

    #region Конструкторы

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeLabel"/> class.
    /// </summary>
    public NodeLabel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeLabel"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public NodeLabel(ObjectId objectId)
        : base(objectId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeLabel"/> class.
    /// </summary>
    /// <param name="lastNodeNumber">Номер узла последнего созданного обозначения узла</param>
    public NodeLabel(string lastNodeNumber)
    {
        _lastNodeNumber = lastNodeNumber;
    }

    #endregion

    #region Свойства

    /// <inheritdoc/>
    public override double MinDistanceBetweenPoints => 1;

    /// <inheritdoc/>
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
                _topTextMask,
                _bottomTextMask,
                _topText,
                _bottomText,
                _outerCircle,
                _innerCircle,
                _line,
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
    /// Для контроля видимости зависимых от значения SheetNumber пунктов в палитре
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 10, "", "", propertyScope: PropertyScope.Hidden)]
    [PropertyVisibilityDependency(new[] { nameof(TextVerticalOffset), nameof(SecondTextHeight) })]
    [SaveToXData]
    public bool SecondTextVisibilityDependency { get; private set; } 

    #endregion

    #region Свойства: геометрия

    /// <summary>
    /// Диаметр внутренней окружности
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p120", 12, 12, 32, nameSymbol: "d1")]
    [SaveToXData]
    public int InnerCircleDiameter { get; set; }

    /// <summary>
    /// Внешняя окружность
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p123", false)]
    [PropertyVisibilityDependency(new[] { nameof(OuterCircleDiameter) })]
    [SaveToXData]
    public bool OuterCircleVisible { get; set; }

    /// <summary>
    /// Диаметр внешней окружности
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p121", 14, 14, 34, nameSymbol: "d2")]
    [SaveToXData]
    public int OuterCircleDiameter { get; set; }

    /// <summary>
    /// Вертикальный отступ малого текста от линии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p122", 0.5, 0.1, 3.0, descLocalKey: "d122", nameSymbol: "v")]
    [SaveToXData]
    public double TextVerticalOffset { get; set; } = 0.5;

    #endregion

    #region Свойства: содержимое

    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Content, 1, "p17", "Standard", descLocalKey: "d17")]
    public override string TextStyle { get; set; } = "Standard";

    /// <summary>
    /// Высота текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 2, "p18", 5.0, 0.000000001, 1.0000E+99, descLocalKey: "d18", nameSymbol: "h1")]
    [SaveToXData]
    public double MainTextHeight { get; set; } = 5.0;

    /// <summary>
    /// Высота малого текста
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 3, "p50", 3.5, 0.000000001, 1.0000E+99, nameSymbol: "h2")]
    [SaveToXData]
    public double SecondTextHeight { get; set; } = 3.5;

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
    /// Номер узла
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 6, "p79", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string NodeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Номер листа
    /// </summary>
    [EntityProperty(PropertiesCategory.Content, 7, "p80", "", propertyScope: PropertyScope.Palette)]
    [SaveToXData]
    [ValueToSearchBy]
    public string SheetNumber
    {
        get => _sheetNumber;
        set
        {
            _sheetNumber = value;

            if (string.IsNullOrEmpty(value))
            {
                SecondTextVisibilityDependency = false;
            }
            else
            {
                SecondTextVisibilityDependency = true;
            }
        }
    } 

    #endregion

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
        SetNodeNumberTextOnCreation();

        if (!HasTextValue())
        {
            return;
        }
        
        // Рисуем окружности
        _innerCircle = new Circle
        {
            Center = insertionPoint,
            Radius = InnerCircleDiameter * scale / 2,
        };

        if (OuterCircleVisible)
        {
            _outerCircle = new Circle
            {
                Center = insertionPoint,
                Radius = OuterCircleDiameter * scale / 2,
            };
        }

        // Проверка текста - номера листа
        if (string.IsNullOrEmpty(_sheetNumber))
        {
            // Рисуем номер узла большим текстом
            _topText = new MText
            {
                Contents = NodeNumber,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = insertionPoint
            };
            _topText.SetProperties(TextStyle, MainTextHeight * scale);
        }
        else
        {
            // Рисуем линию
            var correctingVectorOnHorizont = InnerCircleDiameter / 2.0 * scale * Vector3d.XAxis;

            _line = new Line
            {
                StartPoint = insertionPoint - correctingVectorOnHorizont,
                EndPoint = insertionPoint + correctingVectorOnHorizont
            };

            // Рисуем номер узла и номер листа малым текстом
            var correctingVectorOnVertical = (TextVerticalOffset + (SecondTextHeight / 2)) * scale * Vector3d.YAxis;

            _topText = new MText
            {
                Contents = NodeNumber,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = insertionPoint + correctingVectorOnVertical,
            };
            _topText.SetProperties(TextStyle, SecondTextHeight * scale);

            _bottomText = new MText
            {
                Contents = _sheetNumber,
                Attachment = AttachmentPoint.MiddleCenter,
                Location = insertionPoint - correctingVectorOnVertical,
            };
            _bottomText.SetProperties(TextStyle, SecondTextHeight * scale);
        }

        if (!HideTextBackground)
            return;

        _topTextMask = _topText.GetBackgroundMask(TextMaskOffset * scale);
        if (_bottomText != null)
            _bottomTextMask = _bottomText.GetBackgroundMask(TextMaskOffset * scale);
    }

    private void SetNodeNumberTextOnCreation()
    {
        if (!IsValueCreated)
            return;

        var setStandard = true;
        if (!string.IsNullOrEmpty(_lastNodeNumber))
        {
            if (int.TryParse(_lastNodeNumber, out var i))
            {
                NodeNumber = (i + 1).ToString();
                setStandard = false;
            }
        }

        if (setStandard)
        {
            NodeNumber = "1";
        }
    }

    /// <summary>
    /// True - есть хоть какое-то строковое значение
    /// </summary>
    public bool HasTextValue()
    {
        if (string.IsNullOrEmpty(NodeNumber) &&
            string.IsNullOrEmpty(SheetNumber))
        {
            return false;
        }

        return true;
    }
}