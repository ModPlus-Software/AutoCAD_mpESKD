namespace mpESKD.Base.Enums;

/// <summary>
/// Глобальное значение разделителя целой и дробной части
/// </summary>
public enum GlobalNumberSeparator
{
    /// <summary>
    /// Из настроек ModPlus
    /// </summary>
    FromModPlusSettings,

    /// <summary>
    /// Из стиля
    /// </summary>
    FromStyle,

    /// <summary>
    /// Точка
    /// </summary>
    Dot,

    /// <summary>
    /// Запятая
    /// </summary>
    Comma
}