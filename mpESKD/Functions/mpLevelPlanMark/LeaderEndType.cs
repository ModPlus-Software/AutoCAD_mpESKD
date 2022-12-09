namespace mpESKD.Functions.mpLevelPlanMark;

/// <summary>
/// Тип концов выносок
/// </summary>
public enum LeaderEndType
{
    /// <summary>
    /// Нет
    /// </summary>
    None = 0,

    /// <summary>
    /// Полустрелка
    /// </summary>
    HalfArrow = 1,

    /// <summary>
    /// Точка
    /// </summary>
    Point = 2,

    /// <summary>
    /// Засечка
    /// </summary>
    Section = 3, 

    /// <summary>
    /// Двойная засечка
    /// </summary>
    Resection = 4, 

    /// <summary>
    /// Прямой угол
    /// </summary>
    Angle = 5,

    /// <summary>
    /// Закрашенная
    /// </summary>
    Arrow = 6,

    /// <summary>
    /// Разомкнутая
    /// </summary>
    OpenArrow = 7,

    /// <summary>
    /// Замкнутая
    /// </summary>
    ClosedArrow = 8
}