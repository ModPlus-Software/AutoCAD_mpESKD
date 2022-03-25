namespace mpESKD.Functions.mpSecantNodalLeader;

/// <summary>
/// Состояние Jig при создании секущей узловой выноски
/// </summary>
public enum SecantNodalLeaderJigState
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