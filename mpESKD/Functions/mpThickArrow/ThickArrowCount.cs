namespace mpESKD.Functions.mpThickArrow;

using Base.Attributes;

/// <summary>
/// Наличие стрелок
/// </summary>
public enum ThickArrowCount
{
    /// <summary>С обеих сторон</summary>
    [EnumPropertyDisplayValueKey("amt1")]
    Both,

    /// <summary>Первая</summary>
    [EnumPropertyDisplayValueKey("amt4")]
    First,

    /// <summary>Вторая</summary>
    [EnumPropertyDisplayValueKey("amt5")]
    Second,
}