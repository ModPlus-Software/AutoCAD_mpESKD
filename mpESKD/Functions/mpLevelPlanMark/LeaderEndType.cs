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
    /// Двойная засечка
    /// </summary>
    Resection = 3, 

    /// <summary>
    /// Прямой угол
    /// </summary>
    Angle = 4,

    /// <summary>
    /// Закрашенная
    /// </summary>
    Arrow = 5,

    /// <summary>
    /// Разомкнутая
    /// </summary>
    OpenArrow = 6,

    /// <summary>
    /// Замкнутая
    /// </summary>
    ClosedArrow = 7
}