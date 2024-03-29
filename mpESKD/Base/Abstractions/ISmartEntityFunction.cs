﻿namespace mpESKD.Base.Abstractions;

/// <summary>
/// Интерфейс функции примитива
/// </summary>
public interface ISmartEntityFunction
{
    /// <summary>
    /// Метод, вызываемый при загрузке AutoCAD
    /// </summary>
    void Initialize();

    /// <summary>
    /// Создать аналог интеллектуального примитива
    /// </summary>
    /// <param name="sourceEntity">Объект-источник</param>
    /// <param name="copyLayer">Копировать ли слой</param>
    void CreateAnalog(SmartEntity sourceEntity, bool copyLayer);
}