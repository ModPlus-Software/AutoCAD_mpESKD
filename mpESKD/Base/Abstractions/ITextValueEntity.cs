namespace mpESKD.Base.Abstractions
{
    /// <summary>
    /// Интеллектуальный объект, имеющий текстовое содержимое
    /// </summary>
    public interface ITextValueEntity
    {
        /// <summary>
        /// Скрывать задний план у текста
        /// </summary>
        bool HideTextBackground { get; set; }

        /// <summary>
        /// Отступ маскировки текста
        /// </summary>
        double TextMaskOffset { get; set; }
    }
}
