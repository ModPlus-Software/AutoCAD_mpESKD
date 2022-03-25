namespace mpESKD.Base.Enums;

/// <summary>
/// Вариант обновления примитива
/// </summary>
public enum UpdateVariant
{
    /// <summary>
    /// Указание точки вставки
    /// </summary>
    SetInsertionPoint,

    /// <summary>
    /// Указание конечной точки при наименьшем допустимом расстоянии
    /// </summary>
    SetEndPointMinLength
}