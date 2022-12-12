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
    /// Указание конечной точки (точка выноски)
    /// </summary>
    LeaderPoint
}