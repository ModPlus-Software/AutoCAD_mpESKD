namespace mpESKD.Functions.mpRevisionMark;

/// <summary>
/// Состояние Jig при создании узловой выноски
/// </summary>
public enum RevisionMarkJigState
{
    /// <summary>
    /// Производится указание точки вставки (точки начала отсчета)
    /// </summary>
    InsertionPoint,

    /// <summary>
    /// Происходит указание точки рамки
    /// </summary>
    EndPoint,

    /// <summary>
    /// Указание конечной точки (точка выноски)
    /// </summary>
    LeaderPoint
}