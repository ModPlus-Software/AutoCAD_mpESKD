namespace mpESKD.Base.Enums;

/// <summary>
/// Область видимости свойства
/// </summary>
public enum PropertyScope
{
    /// <summary>
    /// Скрытое свойство. Нужно для работы привязок, но не должно отображаться
    /// </summary>
    Hidden,

    /// <summary>
    /// Только на палитре
    /// </summary>
    Palette,

    /// <summary>
    /// На палитре и в редакторе стилей
    /// </summary>
    PaletteAndStyleEditor
}