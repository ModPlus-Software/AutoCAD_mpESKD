namespace mpESKD.Base.Abstractions;

using Enums;

/// <summary>
/// Интеллектуальный объект, имеющий числовое содержимое
/// </summary>
public interface INumericValueEntity
{
    /// <summary>
    /// Разделитель целой и дробной части
    /// </summary>
    NumberSeparator NumberSeparator { get; set; }
}