namespace mpESKD.Base.Enums;

/// <summary>
/// Свойство "Направление" для примитивов ЕСКД. Может использоваться различными примитивами
/// </summary>
public enum EntityDirection
{
    /// <summary>
    /// Слева на право
    /// </summary>
    LeftToRight,

    /// <summary>
    /// Справа на лево
    /// </summary>
    RightToLeft,

    /// <summary>
    /// Сверху вниз
    /// </summary>
    UpToBottom,

    /// <summary>
    /// Снизу вверх
    /// </summary>
    BottomToUp
}