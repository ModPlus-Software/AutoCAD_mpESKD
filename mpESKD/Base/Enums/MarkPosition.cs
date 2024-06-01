namespace mpESKD.Base.Enums;

using Attributes;

/// <summary>
/// Положение полки
/// </summary>
public enum MarkPosition
{
    /// <summary>
    /// Вправо
    /// </summary>
    [EnumPropertyDisplayValueKey("tha3")]
    Right = 0,

    /// <summary>
    /// Влево
    /// </summary>
    [EnumPropertyDisplayValueKey("tha1")]
    Left = 1
}