namespace mpESKD.Functions.mpCrestedLeader;

/// <summary>
/// Состояние Jig при создании узловой выноски
/// </summary>
public enum CrestedLeaderJigState 
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
    LeaderStart
}