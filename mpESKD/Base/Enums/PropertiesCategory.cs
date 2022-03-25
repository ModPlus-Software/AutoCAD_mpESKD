namespace mpESKD.Base.Enums;

using Attributes;

/// <summary>
/// Категория свойства интеллектуального примитива.
/// Указывает в какой группе будет отображаться свойство в палитре свойств
/// </summary>
public enum PropertiesCategory
{
    /// <summary>
    /// Не определено
    /// </summary>
    Undefined = -1,

    /// <summary>
    /// Основные
    /// </summary>
    [EnumPropertyDisplayValueKey("h49")]
    General = 1,

    /// <summary>
    /// Геометрия
    /// </summary>
    [EnumPropertyDisplayValueKey("h51")]
    Geometry = 2,

    /// <summary>
    /// Содержимое
    /// </summary>
    [EnumPropertyDisplayValueKey("h64")]
    Content = 3
}