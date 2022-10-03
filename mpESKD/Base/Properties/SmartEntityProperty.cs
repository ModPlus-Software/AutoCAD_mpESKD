namespace mpESKD.Base.Properties;

using System;
using Attributes;
using Autodesk.AutoCAD.DatabaseServices;
using Enums;
using JetBrains.Annotations;
using ModPlusAPI.Mvvm;
using View;

/// <summary>
/// Свойство интеллектуального объекта
/// </summary>
public class SmartEntityProperty : ObservableObject
{
    private object _value;
    private double _doubleValue;
    private int _intValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEntityProperty"/> class.
    /// </summary>
    /// <param name="attribute">Атрибут <see cref="EntityPropertyAttribute"/></param>
    /// <param name="entityType">Тип интеллектуального объекта</param>
    /// <param name="value">Значение свойства</param>
    /// <param name="ownerObjectId">Идентификатор блока</param>
    /// <param name="regexInputRestrictionAttribute">Экземпляр атрибута <see cref="Attributes.RegexInputRestrictionAttribute"/></param>
    public SmartEntityProperty(
        EntityPropertyAttribute attribute,
        Type entityType,
        object value,
        ObjectId ownerObjectId,
        RegexInputRestrictionAttribute regexInputRestrictionAttribute)
    {
        EntityType = entityType;
        OwnerObjectId = ownerObjectId;
        RegexInputRestrictionAttribute = regexInputRestrictionAttribute;
        Category = attribute.Category;
        OrderIndex = attribute.OrderIndex;
        Name = attribute.Name;
        DisplayNameLocalizationKey = attribute.DisplayNameLocalizationKey;
        NameSymbolForStyleEditor = attribute.NameSymbol;
        DescriptionLocalizationKey = attribute.DescriptionLocalizationKey;

        if (value != null && value.GetType() == typeof(AnnotationScale))
        {
            DefaultValue = new AnnotationScale
            {
                Name = attribute.DefaultValue.ToString(),
                DrawingUnits = double.Parse(attribute.DefaultValue.ToString().Split(':')[0]),
                PaperUnits = double.Parse(attribute.DefaultValue.ToString().Split(':')[1])
            };
        }
        else if (Name == "LayerName" && string.IsNullOrEmpty(attribute.DefaultValue.ToString()))
        {
            DefaultValue = ModPlusAPI.Language.GetItem("defl");
        }
        else
        {
            DefaultValue = attribute.DefaultValue;
        }

        Minimum = attribute.Minimum;
        Maximum = attribute.Maximum;
        Value = value;
        if (value is double d)
        {
            DoubleValue = d;
        }

        if (value is int i)
        {
            IntValue = i;
        }

        PropertyScope = attribute.PropertyScope;
        IsReadOnly = attribute.IsReadOnly;
        StringMaxLength = attribute.StringMaxLength;
    }

    /// <summary>
    /// Тип интеллектуального объекта
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Идентификатор блока-владельца. Свойство используется при работе палитры.
    /// При работе со стилями свойство равно ObjectId.Null
    /// </summary>
    public ObjectId OwnerObjectId { get; }

    /// <summary>
    /// Экземпляр атрибута <see cref="Attributes.RegexInputRestrictionAttribute"/>
    /// <para>
    /// Данное свойство используется для "прокидывания" атрибута <see cref="Attributes.RegexInputRestrictionAttribute"/>
    /// в окно редактора стилей
    /// </para>
    /// </summary> 
    public RegexInputRestrictionAttribute RegexInputRestrictionAttribute { get; }

    /// <summary>
    /// Категория свойства
    /// </summary>
    public PropertiesCategory Category { get; }

    /// <summary>
    /// Индекс порядка расположения свойства в палитре
    /// </summary>
    public int OrderIndex { get; }

    /// <summary>
    /// Имя свойства
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Ключ локализации для отображаемого имени свойства
    /// </summary>
    public string DisplayNameLocalizationKey { get; }

    /// <summary>
    /// Условное обозначение на изображении в редакторе стилей
    /// </summary>
    public string NameSymbolForStyleEditor { get; }

    /// <summary>
    /// Ключ локализации для описания свойства
    /// </summary>
    public string DescriptionLocalizationKey { get; }

    /// <summary>
    /// Значение по умолчанию
    /// </summary>
    public object DefaultValue { get; }

    /// <summary>
    /// Значение свойства
    /// </summary>
    public object Value
    {
        get => _value;
        set
        {
            if (Equals(value, _value))
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подробнее смотри в описании метода <see cref="StyleEditor.CreateTwoWayBindingForPropertyForNumericValue"/>
    /// </summary>
    public double DoubleValue
    {
        get => _doubleValue;
        set
        {
            if (value.Equals(_doubleValue))
            {
                return;
            }

            _doubleValue = value;
            _value = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подробнее смотри в описании метода <see cref="StyleEditor.CreateTwoWayBindingForPropertyForNumericValue"/>
    /// </summary>
    public int IntValue
    {
        get => _intValue;
        set
        {
            if (value == _intValue)
            {
                return;
            }

            _intValue = value;
            _value = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Минимальное значение (для int, double)
    /// </summary>
    [CanBeNull]
    public object Minimum { get; }

    /// <summary>
    /// Максимальное значение (для int, double)
    /// </summary>
    [CanBeNull]
    public object Maximum { get; }

    /// <summary>
    /// Область видимости свойства
    /// </summary>
    public PropertyScope PropertyScope { get; }

    /// <summary>
    /// Свойство только для чтения. Используется только в палитре свойств
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Максимальная длина строки для текстовых свойств
    /// </summary>
    public int StringMaxLength { get; }

    /// <summary>
    /// Установить значение с учетом того, что value может быть <see cref="double"/> или <see cref="int"/>
    /// </summary>
    /// <param name="value">Значение для установки</param>
    public void SetValue(object value)
    {
        if (value is double d)
            DoubleValue = d;
        else if (value is int i)
            IntValue = i;
        else
            Value = value;
    }
}