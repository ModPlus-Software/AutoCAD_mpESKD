namespace mpESKD.Functions.mpLevelPlanMark;

/// <summary>
/// Тип концов выносок
/// </summary>
public enum LeaderEndType
{
    None = 0, // Нет
    HalfArrow = 1, // Полустрелка
    Point = 2, // Точка
    Resection = 3, // Двойная засечка
    Angle = 4, // Прямой угол 
    Arrow = 5, // Закрашенная
    OpenArrow = 6, // Разомкнутая
    ClosedArrow = 7, // Замкнутая
}