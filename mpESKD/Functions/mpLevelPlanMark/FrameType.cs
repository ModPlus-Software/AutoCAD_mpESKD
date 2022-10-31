namespace mpESKD.Functions.mpLevelPlanMark;

using Base.Attributes;

/// <summary>
/// Тип рамки
/// </summary>
public enum FrameType
{
    /// <summary>
    /// Прямоугольная
    /// </summary>
    [EnumPropertyDisplayValueKey("ft2")]
    Rectangular,

    /// <summary>
    /// Линия
    /// </summary>
    [EnumPropertyDisplayValueKey("ft3")]
    Line,

    /// <summary>
    /// Без рамки
    /// </summary>
    [EnumPropertyDisplayValueKey("ft4")] 
    None
}