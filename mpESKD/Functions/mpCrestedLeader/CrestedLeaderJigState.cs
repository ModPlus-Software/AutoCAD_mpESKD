namespace mpESKD.Functions.mpCrestedLeader;

public enum CrestedLeaderJigState
{
    /// <summary>
    /// Не в режиме указания точек
    /// </summary>
    None = 0,

    /// <summary>
    /// Происходит указание точки вставки и первой выноски
    /// </summary>
    PromptInsertPoint = 1,

    /// <summary>
    /// Происходит указание точки следующей выноски
    /// </summary>
    PromptNextLeaderPoint = 2,

    /// <summary>
    /// Происходит указание первой точки полки
    /// </summary>
    PromptShelfStartPoint = 3,

    /// <summary>
    /// Происходит указание точки отступа полки
    /// </summary>
    PromptShelfIndentPoint = 4
}