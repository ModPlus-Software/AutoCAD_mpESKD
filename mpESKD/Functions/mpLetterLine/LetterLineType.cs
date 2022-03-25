namespace mpESKD.Functions.mpLetterLine;

using mpESKD.Base.Attributes;

/// <summary>
/// Тип объекта <see cref="LetterLine"/>
/// </summary>
public enum LetterLineType
{
    /// <summary>
    /// Стандартный
    /// </summary>
    [EnumPropertyDisplayValueKey("llt1")]
    Standard,

    /// <summary>
    /// Составной
    /// </summary>
    [EnumPropertyDisplayValueKey("llt2")]
    Composite
}