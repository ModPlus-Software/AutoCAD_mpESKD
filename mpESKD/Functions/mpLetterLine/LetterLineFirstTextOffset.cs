namespace mpESKD.Functions.mpLetterLine
{
    using Base.Attributes;

    /// <summary>
    /// Отступ первого текста в объекте LetterLine
    /// </summary>
    public enum LetterLineFirstTextOffset
    {
        /// <summary>
        /// По расстоянию между текстами
        /// </summary>
        [EnumPropertyDisplayValueKey("glfst1")]
        ByTextOffset,

        /// <summary>
        /// Половина расстояния между группами текстами
        /// </summary>
        [EnumPropertyDisplayValueKey("glfst2")]
        ByHalfSpace,

        /// <summary>
        /// Расстояние между группами Текстами
        /// </summary>
        [EnumPropertyDisplayValueKey("glfst3")]
        BySpace
    }
}
