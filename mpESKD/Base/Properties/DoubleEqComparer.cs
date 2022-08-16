namespace mpESKD.Base.Properties;

using System;
using System.Collections.Generic;

/// <summary>
/// Объект для сравнения значений типа Double
/// </summary>
public class DoubleEqComparer : IEqualityComparer<double>
{
    private readonly double _precisions;

    /// <summary>
    /// Создание объекта с точностью сравнения 0,000001
    /// </summary>
    public DoubleEqComparer()
    {
        _precisions = 0.000001;
    }

    /// <summary>
    /// Создание объекта с заданной точностью сравнения
    /// </summary>
    /// <param name="precisions">Точность сравнения</param>
    public DoubleEqComparer(double precisions)
    {
        _precisions = precisions;
    }

    /// <inheritdoc />
    public bool Equals(double x, double y)
    {
        return Math.Abs(x - y) <= _precisions;
    }

    /// <inheritdoc />
    public int GetHashCode(double obj)
    {
        return obj.GetHashCode();
    }
}