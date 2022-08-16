namespace mpESKD.Functions.mpGroundLine;

using Base.Attributes;

/// <summary>
/// Отступ первого штриха в объекте GroundLine
/// </summary>
public enum GroundLineFirstStrokeOffset
{
    /// <summary>
    /// По расстоянию между штрихами
    /// </summary>
    [EnumPropertyDisplayValueKey("glfst1")]
    ByStrokeOffset,

    /// <summary>
    /// Половина расстояния между группами штрихов
    /// </summary>
    [EnumPropertyDisplayValueKey("glfst2")]
    ByHalfSpace,

    /// <summary>
    /// Расстояние между группами штрихов
    /// </summary>
    [EnumPropertyDisplayValueKey("glfst3")]
    BySpace
}