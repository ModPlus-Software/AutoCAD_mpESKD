namespace mpESKD.Functions.mpViewLabel
{
    using mpESKD.Base.Attributes;

    public enum ViewLabelType
    {
        /// <summary>Разрез</summary>
        [EnumPropertyDisplayValueKey("vlt1")]
        Section,

        /// <summary>Вид</summary>
        [EnumPropertyDisplayValueKey("vlt2")]
        View
    }
}
