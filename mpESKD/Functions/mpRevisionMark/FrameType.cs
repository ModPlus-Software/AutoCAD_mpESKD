namespace mpESKD.Functions.mpRevisionMark;

using mpESKD.Base.Attributes;

/// <summary>
/// Тип рамки
/// </summary>
public enum FrameType
{
    /// <summary>
    /// Круглая
    /// </summary>
    [EnumPropertyDisplayValueKey("ft1")] Round,

    /// <summary>
    /// Прямоугольная
    /// </summary>
    [EnumPropertyDisplayValueKey("ft2")] Rectangular
}