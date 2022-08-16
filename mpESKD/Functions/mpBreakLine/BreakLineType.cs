namespace mpESKD.Functions.mpBreakLine;

using Base.Attributes;

/// <summary>
/// Тип линии: линейный, криволинейный, цилиндрический
/// </summary>
public enum BreakLineType
{
    /// <summary>
    /// Линейный
    /// </summary>
    [EnumPropertyDisplayValueKey("blt1")]
    Linear = 1,

    /// <summary>
    /// Криволинейный
    /// </summary>
    [EnumPropertyDisplayValueKey("blt2")]
    Curvilinear = 2,

    /// <summary>
    /// Цилиндрический
    /// </summary>
    [EnumPropertyDisplayValueKey("blt3")]
    Cylindrical = 3
}