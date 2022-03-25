namespace mpESKD.Base.Enums;

using Attributes;

/// <summary>
/// Разделитель целой и дробной части
/// </summary>
public enum NumberSeparator
{
    /// <summary>
    /// Точка
    /// </summary>
    [EnumPropertyDisplayValueKey("dot")]
    Dot,

    /// <summary>
    /// Запятая
    /// </summary>
    [EnumPropertyDisplayValueKey("comma")]
    Comma
}