namespace mpESKD.Base.Attributes;

using System;
using System.Runtime.CompilerServices;
using Enums;
using JetBrains.Annotations;

/// <summary>
/// Атрибут используется для настройки свойств интеллектуальных объектов
/// </summary>
public class EntityPropertyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityPropertyAttribute"/> class.
    /// </summary>
    /// <param name="category">Категория свойства</param>
    /// <param name="orderIndex">Индекс для сортировки</param>
    /// <param name="displayNameLocalizationKey">Ключ локализации для отображаемого имени свойства</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <param name="minimum">Минимальное значение. Для свойств типа int, double</param>
    /// <param name="maximum">Максимальное значение. Для свойств типа int, double</param>
    /// <param name="propertyScope">Область видимости свойства</param>
    /// <param name="name">Имя свойства</param>
    /// <param name="descLocalKey">Ключ локализации для описания свойства. Если не задан
    /// (string.Empty или null), то для описания будет браться значение <see cref="DisplayNameLocalizationKey"/></param>
    /// <param name="nameSymbol">Условное обозначение свойства на изображении в редакторе стилей.
    /// Условные обозначения всегда указываются латинскими буквами, а значит не требуют локализации</param>
    /// <param name="isReadOnly">Свойство только для чтения. Используется только в палитре свойств</param>
    public EntityPropertyAttribute(
        PropertiesCategory category,
        int orderIndex,
        string displayNameLocalizationKey,
        object defaultValue,
        object minimum = null,
        object maximum = null,
        PropertyScope propertyScope = PropertyScope.PaletteAndStyleEditor,
        [CallerMemberName] string name = null,
        string descLocalKey = null,
        string nameSymbol = null,
        bool isReadOnly = false)
    {
        Category = category;
        OrderIndex = orderIndex;
        Name = name;
        DisplayNameLocalizationKey = displayNameLocalizationKey;
        DescriptionLocalizationKey = string.IsNullOrEmpty(descLocalKey)
            ? displayNameLocalizationKey
            : descLocalKey;
        DefaultValue = defaultValue;
        Minimum = minimum;
        Maximum = maximum;
        PropertyScope = propertyScope;
        NameSymbol = nameSymbol;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Категория свойства
    /// </summary>
    public PropertiesCategory Category { get; }

    /// <summary>
    /// Имя свойства
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Индекс для сортировки
    /// </summary>
    public int OrderIndex { get; }

    /// <summary>
    /// Ключ локализации для отображаемого имени свойства
    /// </summary>
    public string DisplayNameLocalizationKey { get; }

    /// <summary>
    /// Ключ локализации для описания свойства. Если не задан (string.Empty или null), то для описания будет
    /// браться значение <see cref="DisplayNameLocalizationKey"/>
    /// </summary>
    public string DescriptionLocalizationKey { get; }

    /// <summary>
    /// Значение по умолчанию
    /// </summary>
    [NotNull]
    public object DefaultValue { get; }

    /// <summary>
    /// Минимальное значение. Для свойств типа int, double
    /// </summary>
    [CanBeNull]
    public object Minimum { get; }

    /// <summary>
    /// Максимальное значение. Для свойств типа int, double
    /// </summary>
    [CanBeNull]
    public object Maximum { get; }

    /// <summary>
    /// Область видимости свойства
    /// </summary>
    public PropertyScope PropertyScope { get; }

    /// <summary>
    /// Условное обозначение на изображении в редакторе стилей
    /// </summary>
    public string NameSymbol { get; }

    /// <summary>
    /// Свойство только для чтения. Используется только в палитре свойств
    /// </summary>
    public bool IsReadOnly { get; }
}