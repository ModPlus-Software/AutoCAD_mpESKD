namespace mpESKD.Base;

using System.Collections.Generic;
using Abstractions;
using Attributes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Enums;

/// <summary>
/// Абстрактный класс линейного интеллектуального объекта
/// </summary>
public abstract class SmartLinearEntity : SmartEntity, ILinearEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartLinearEntity"/> class.
    /// </summary>
    protected SmartLinearEntity()
        : base()
    {
    }

    /// <summary>
    /// Инициализация экземпляра класса <see cref="SmartLinearEntity"/> без заполнения данными
    /// В данном случае уже все данные получены и нужно только "построить" базовые примитивы
    /// </summary>
    /// <param name="blockId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    protected SmartLinearEntity(ObjectId blockId)
        : base(blockId)
    {
    }

    /// <inheritdoc/>
    [SaveToXData]
    public List<Point3d> MiddlePoints { get; set; } = new List<Point3d>();
       
    /// <inheritdoc />
    [EntityProperty(PropertiesCategory.Geometry, -1, "len", null, propertyScope: PropertyScope.Palette, isReadOnly: true)]
    public double Length
    {
        get
        {
            var points = new List<Point3d> { InsertionPoint };
            points.AddRange(MiddlePoints);
            points.Add(EndPoint);
            var length = 0.0;
            for (var i = 1; i < points.Count; i++)
            {
                length += points[i - 1].DistanceTo(points[i]);
            }

            return length;
        }
    }

    /// <inheritdoc/>
    public void RebasePoints()
    {
        if (!MiddlePoints.Contains(EndPoint))
        {
            MiddlePoints.Add(EndPoint);
        }
    }

    /// <inheritdoc />
    public List<Point3d> GetAllPoints()
    {
        var list = new List<Point3d> { InsertionPoint };
        list.AddRange(MiddlePoints);
        list.Add(EndPoint);
        return list;
    }
}