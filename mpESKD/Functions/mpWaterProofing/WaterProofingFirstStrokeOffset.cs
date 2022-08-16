namespace mpESKD.Functions.mpWaterProofing;

using Base.Attributes;

/// <summary>
/// Отступ первого штриха линии гидроизоляции
/// </summary>
public enum WaterProofingFirstStrokeOffset
{
    /// <summary>
    /// Расстояние между штрихами
    /// </summary>
    [EnumPropertyDisplayValueKey("glfst1")]
    ByStrokeOffset,

    /// <summary>
    /// Половина расстояния между штрихами
    /// </summary>
    [EnumPropertyDisplayValueKey("wpfst1")]
    ByHalfStrokeOffset,

    /// <summary>
    /// Без отступа
    /// </summary>
    [EnumPropertyDisplayValueKey("wpfst2")]
    NoOffset
}