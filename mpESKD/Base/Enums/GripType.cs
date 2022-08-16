namespace mpESKD.Base.Enums;

/// <summary>
/// Виды ручек для примитива
/// </summary>
public enum GripType
{
    /// <summary>
    /// Обычная точка
    /// </summary>
    Point = 1,

    /// <summary>
    /// Отображение плюса
    /// </summary>
    Plus,

    /// <summary>
    /// Отображение минуса
    /// </summary>
    Minus,

    /// <summary>
    /// Положение текста
    /// </summary>
    Text,

    /// <summary>
    /// Список (выпадающий список)
    /// </summary>
    List,

    /// <summary>
    /// Две стрелки с направлением "Вверх-вниз"
    /// </summary>
    TwoArrowsUpDown,
        
    /// <summary>
    /// Две стрелки с направлением "Влево-вправо"
    /// </summary>
    TwoArrowsLeftRight,

    /// <summary>
    /// Точка отсчета
    /// </summary>
    BasePoint
}