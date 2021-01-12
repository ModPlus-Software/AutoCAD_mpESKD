namespace mpESKD.Functions.mpNodalLeader
{
    using Base.Attributes;

    /// <summary>
    /// Тип рамки
    /// </summary>
    public enum FrameType
    {
        /// <summary>
        /// Круглая
        /// </summary>
        [EnumPropertyDisplayValueKey("ft1")]
        Round,

        /// <summary>
        /// Прямоугольная
        /// </summary>
        [EnumPropertyDisplayValueKey("ft2")]
        Rectangular
    }
}
