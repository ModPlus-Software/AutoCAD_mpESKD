namespace mpESKD.Functions.mpRevisionMark;

using Base.Attributes;

/// <summary>
/// Тип рамки ревизии
/// </summary>
public enum RevisionFrameType
{
    /// <summary>
    /// Без рамки
    /// </summary>
    [EnumPropertyDisplayValueKey("ft4")]
    None = 0,

    /// <summary>
    /// Прямоугольная
    /// </summary>
    [EnumPropertyDisplayValueKey("ft2")]
    Rectangular = 1,

    /// <summary>
    /// Круглая
    /// </summary>
    [EnumPropertyDisplayValueKey("ft1")]
    Round = 2,

}