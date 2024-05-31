namespace mpESKD.Functions.mpCrestedLeader;

public enum CrestedLeaderJigState
{
    /// <summary>
    /// Не в режиме запроса точек
    /// </summary>
    None = 0,

    /// <summary>
    /// Запрос точки вставки и первой выноски
    /// </summary>
    PromptInsertPoint = 1,

    /// <summary>
    /// Запрос точки следующей выноски
    /// </summary>
    PromptNextPoint = 2,

    /// <summary>
    /// Запрос точки полки
    /// </summary>
    CustomPoint = 3
}