namespace mpESKD.Functions.mpLevelMark.Grips
{
    using Base;

    /// <summary>
    /// Имя ручки объекта <see cref="LevelMark"/>
    /// </summary>
    public enum GripName
    {
        /// <summary>
        /// Точка начала отсчета
        /// </summary>
        BasePoint,

        /// <summary>
        /// Точка уровня отсчета (точка объекта)
        /// </summary>
        ObjectPoint,

        /// <summary>
        /// Точка начала нижней полки
        /// </summary>
        BottomShelfStartPoint,

        /// <summary>
        /// Точка начала стрелки (соответствует <see cref="SmartEntity.EndPoint"/>)
        /// </summary>
        ArrowPoint,

        /// <summary>
        /// Точка начала верхней полки
        /// </summary>
        TopShelfPoint
    }
}
