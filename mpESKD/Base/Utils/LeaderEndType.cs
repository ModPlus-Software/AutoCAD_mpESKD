using mpESKD.Base.Attributes;

namespace mpESKD.Base.Utils;

/// <summary>
/// Тип концов выносок
/// </summary>
public enum LeaderEndType
{
    /// <summary>
    /// Нет
    /// </summary>
    [EnumPropertyDisplayValueKey("let0")]
    None = 0,

    /// <summary>
    /// Полустрелка
    /// </summary>
    [EnumPropertyDisplayValueKey("let1")]
    HalfArrow = 1,

    /// <summary>
    /// Точка
    /// </summary>
    [EnumPropertyDisplayValueKey("let2")]
    Point = 2,

    /// <summary>
    /// Двойная засечка
    /// </summary>
    [EnumPropertyDisplayValueKey("let3")]
    Resection = 3,

    /// <summary>
    /// Прямой угол
    /// </summary>
    [EnumPropertyDisplayValueKey("let4")]
    Angle = 4,

    /// <summary>
    /// Закрашенная
    /// </summary>
    [EnumPropertyDisplayValueKey("let5")]
    Arrow = 5,

    /// <summary>
    /// Разомкнутая
    /// </summary>
    [EnumPropertyDisplayValueKey("let6")]
    OpenArrow = 6,

    /// <summary>
    /// Замкнутая
    /// </summary>
    [EnumPropertyDisplayValueKey("let7")] 
    ClosedArrow = 7,

    /// <summary>
    /// Засечка
    /// </summary>
    [EnumPropertyDisplayValueKey("let8")] 
    Section = 8
}