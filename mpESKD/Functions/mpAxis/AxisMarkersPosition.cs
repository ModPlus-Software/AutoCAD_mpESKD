namespace mpESKD.Functions.mpAxis;

using Base.Attributes;

/// <summary>
/// Позиция маркеров оси
/// </summary>
public enum AxisMarkersPosition
{
    /// <summary>С обеих сторон</summary>
    [EnumPropertyDisplayValueKey("amt1")]
    Both,

    /// <summary>Сверху</summary>
    [EnumPropertyDisplayValueKey("amt2")]
    Top,

    /// <summary>Снизу</summary>
    [EnumPropertyDisplayValueKey("amt3")]
    Bottom
}