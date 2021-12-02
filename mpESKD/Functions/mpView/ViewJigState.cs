namespace mpESKD.Functions.mpView
{
    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public enum ViewJigState
    {
        /// <summary>
        /// Производится указание точки вставки (точки начала отсчета)
        /// </summary>
        InsertionPoint,

        /// <summary>
        /// Происходит указание точки рамки
        /// </summary>
        EndPoint
    }
}
