namespace mpESKD.Functions.mpLevelMark;

/// <summary>
/// Состояние Jig при создании высотной отметки
/// </summary>
public enum LevelMarkJigState
{
    /// <summary>
    /// Производится указание точки вставки (точки начала отсчета)
    /// </summary>
    InsertionPoint,

    /// <summary>
    /// Происходит указание точки объекта (точки уровня)
    /// </summary>
    ObjectPoint,

    /// <summary>
    /// Указание конечной точки (точка начала стрелки)
    /// </summary>
    EndPoint
}