﻿namespace mpESKD.Functions.mpNodalLeader
{
    /// <summary>
    /// Состояние Jig при создании узловой выноски
    /// </summary>
    public enum NodalLeaderJigState
    {
        /// <summary>
        /// Производится указание точки вставки (точки начала отсчета)
        /// </summary>
        InsertionPoint,

        /// <summary>
        /// Происходит указание точки рамки
        /// </summary>
        ObjectPoint,

        /// <summary>
        /// Указание конечной точки (точка выноски)
        /// </summary>
        EndPoint
    }
}
