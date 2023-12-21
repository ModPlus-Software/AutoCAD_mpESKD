namespace mpESKD.Functions.mpThickArrow;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;
using System;
using System.Collections.Generic;

/// <summary>
/// Толстая стрелка
/// </summary>
[SmartEntityDisplayNameKey("h187")]
[SystemStyleDescriptionKey("h188")]
public class ThickArrow : SmartEntity, IWithDoubleClickEditor
{
    #region Entities

    /// <summary>
    /// Линия
    /// </summary>
    private Polyline _line;

    /// <summary>
    /// Стрелка первая
    /// </summary>
    private Polyline _firstArrow;

    /// <summary>
    /// Стрелка вторая
    /// </summary>
    private Polyline _secondArrow;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrow"/> class.
    /// </summary>
    public ThickArrow() 
    { 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrow"/> class.
    /// </summary>
    /// <param name="objectId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
    public ThickArrow(ObjectId objectId)
        : base(objectId)
    {
    }

    #region Неиспользуемые, но необходимые к реализации свойства

    /// <inheritdoc />
    /// В примитиве не используется!
    public override string LineType { get; set; }

    /// <inheritdoc />
    /// В примитиве не используется!
    public override double LineTypeScale { get; set; }

    /// <inheritdoc />
    /// В примитиве не используется!
    public override string TextStyle { get; set; }

    #endregion

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 1.0; // = минимальному значению ArrowLength

    /// <summary> 
    /// Количество стрелок
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p115", ThickArrowCount.First, descLocalKey: "d87")]
    [SaveToXData]
    public ThickArrowCount ArrowCount { get; set; } = ThickArrowCount.First;

    /// <summary>
    /// Толщина линии
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p114", 0.5, 0.1, 3.0, nameSymbol: "t1")]
    [SaveToXData]
    public double LineWidth { get; set; } = 0.5;

    /// <summary>
    /// Длина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 3, "p47", 5.0, 1.0, 10.0, nameSymbol: "e")]
    [SaveToXData]
    public double ArrowLength { get; set; } = 5.0;

    /// <summary>
    /// Толщина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 4, "p48", 1.5, 0.5, 5.0, nameSymbol: "t2")]
    [SaveToXData]
    public double ArrowWidth { get; set; } = 1.5;

    /// <summary>
    /// Средняя точка. Нужна для перемещения примитива
    /// </summary>
    public Point3d MiddlePoint => new (
        (InsertionPoint.X + EndPoint.X) / 2,
        (InsertionPoint.Y + EndPoint.Y) / 2,
        (InsertionPoint.Z + EndPoint.Z) / 2);

    /// <inheritdoc />
    public override IEnumerable<Entity> Entities
    {
        get
        {
            var entities = new List<Entity>
            {
               _firstArrow,
               _secondArrow,
               _line,
            };

            foreach (var e in entities)
            {
                SetImmutablePropertiesToNestedEntity(e);
            }

            return entities;
        }
    }

    /// <inheritdoc />
    public override IEnumerable<Point3d> GetPointsForOsnap()
    {
        yield return InsertionPoint;
        yield return EndPoint;
    }

    /// <summary>
    /// UpdateEntities
    /// </summary>
    public override void UpdateEntities()
    {
        try
        {
            var scale = GetScale();

            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента
                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (7 * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, tmpEndPoint, scale);
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(InsertionPointOCS, EndPointOCS, scale);
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint, Point3d endPoint, double scale)
    {
        _firstArrow = null;
        _secondArrow = null;
        _line = null;

        // Минимальная длина для создания стрелок
        var minDistance = ArrowCount == ThickArrowCount.Both ? ArrowLength * scale * 2 :
            ArrowLength * scale;

        var normalVector = (endPoint - insertionPoint).GetNormal();
        var fullLength = endPoint.DistanceTo(insertionPoint);

        var arrowLength = ArrowLength * scale;
        var arrowWidth = ArrowWidth * scale;

        // Если места для стрелок достаточно
        if (fullLength >= minDistance)
        {
            // Чтобы толщина линии не превысила ширину основания стрелки
            if (LineWidth > ArrowWidth)
                LineWidth = ArrowWidth;

            double lineLength;
            Point3d lineEndPoint;

            // Точка основания второй стрелки
            var secondArrowPoint = insertionPoint + (normalVector * arrowLength);

            switch (ArrowCount)
            {
                case ThickArrowCount.Both:
                    lineLength = fullLength - (arrowLength * 2);
                    lineEndPoint = insertionPoint + (normalVector * (lineLength + arrowLength));
                    break;
                case ThickArrowCount.First:
                    lineLength = fullLength - arrowLength;
                    lineEndPoint = insertionPoint + (normalVector * lineLength);
                    break;
                case ThickArrowCount.Second:
                    lineLength = fullLength - arrowLength;
                    lineEndPoint = secondArrowPoint + (normalVector * lineLength);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _line = new Polyline(2);

            _line.AddVertexAt(0, lineEndPoint.ToPoint2d(), 0.0, LineWidth * scale, LineWidth * scale);

            switch (ArrowCount)
            {
                case ThickArrowCount.First:
                    _line.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, LineWidth * scale, LineWidth * scale);
                    break;
                case ThickArrowCount.Second:
                case ThickArrowCount.Both:
                    _line.AddVertexAt(1, secondArrowPoint.ToPoint2d(), 0.0, LineWidth * scale, LineWidth * scale);
                    break;
            }

            // Стрелки
            if (ArrowCount == ThickArrowCount.First | ArrowCount == ThickArrowCount.Both)
            {
                _firstArrow = new Polyline(2);
                _firstArrow.AddVertexAt(0, endPoint.ToPoint2d(), 0.0, 0.0, arrowWidth);
                _firstArrow.AddVertexAt(1, lineEndPoint.ToPoint2d(), 0.0, arrowWidth, arrowWidth);
            }

            if (ArrowCount == ThickArrowCount.Second | ArrowCount == ThickArrowCount.Both)
            {
                _secondArrow = new Polyline(2);
                _secondArrow.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0.0, arrowWidth);
                _secondArrow.AddVertexAt(1, secondArrowPoint.ToPoint2d(), 0.0, arrowWidth, arrowWidth);
            }
        }
        else
        {
            var lineEndPoint = insertionPoint + (normalVector * fullLength);

            _line = new Polyline(2);
            _line.AddVertexAt(0, lineEndPoint.ToPoint2d(), 0.0, LineWidth * scale, LineWidth * scale);
            _line.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, LineWidth * scale, LineWidth * scale);
        }
    }
}