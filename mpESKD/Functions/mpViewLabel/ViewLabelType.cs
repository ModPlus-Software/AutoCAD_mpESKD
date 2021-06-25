namespace mpESKD.Functions.mpViewLabel
{
    using Base.Attributes;

    /// <summary>
    /// Тип объекта <see cref="ViewLabel"/>
    /// </summary>
    public enum ViewLabelType
    {
        /// <summary>
        /// Разрез
        /// </summary>
        [EnumPropertyDisplayValueKey("vlt1")]
        Section,

        /// <summary>
        /// Вид
        /// </summary>
        [EnumPropertyDisplayValueKey("vlt2")]
        View
    }
}
