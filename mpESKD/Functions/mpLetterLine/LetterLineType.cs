namespace mpESKD.Functions.mpLetterLine
{
    using mpESKD.Base.Attributes;

    /// <summary>
    /// Тип объекта <see cref="LetterLine"/>
    /// </summary>
    public enum LetterLineType
    {
        /// <summary>
        /// Стандартный
        /// </summary>
        [EnumPropertyDisplayValueKey("vlt11")]
        Standart,

        /// <summary>
        /// Составной
        /// </summary>
        [EnumPropertyDisplayValueKey("vlt22")]
        Composite
    }
}
