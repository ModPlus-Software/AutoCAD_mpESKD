namespace mpESKD.Base.Enums;

using Attributes;

/// <summary>
/// Выравнивание текста по горизонтали
/// </summary>
public enum TextHorizontalAlignment
{
    /// <summary>
    /// Влево
    /// </summary>
    [EnumPropertyDisplayValueKey("tha1")]
    Left = 0,

    /// <summary>
    /// По центру
    /// </summary>
    [EnumPropertyDisplayValueKey("tha2")]
    Center = 1,

    /// <summary>
    /// Вправо
    /// </summary>
    [EnumPropertyDisplayValueKey("tha3")]
    Right = 2
}