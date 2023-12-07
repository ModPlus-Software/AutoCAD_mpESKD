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
    /// Полка
    /// </summary>
    private Polyline _shelf;

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
    public ThickArrow() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="View"/> class.
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
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    #endregion 


    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => ShelfWidth * 20;


    /// <summary> Количество стрелок</summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p115", 1, 1, 2, nameSymbol: "t1")]
    [SaveToXData]
    public int ArrowQuantity { get; set; } = 1;

    /// <summary>Толщина полки</summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p114", 0.5, 0.1, 5, nameSymbol: "t1")]
    [SaveToXData]
    public double ShelfWidth { get; set; } = 0.5;


    /// <summary>Длина стрелки</summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p47", 5.0, 1.0, 10.0, nameSymbol: "e")]
    [SaveToXData]
    public double ArrowLength { get; set; } = 5.0;

    /// <summary>Толщина стрелки</summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p48", 1.5, 1.0, 3.0, nameSymbol: "t2")]
    [SaveToXData]
    public double ArrowWidth { get; set; } = 1.5;


    /// <summary>Средняя точка. Нужна для перемещения  примитива</summary>
    public Point3d MiddlePoint => new Point3d(
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
               _shelf,
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
        yield return InsertionPoint; // TODO: посм. итераторы
        yield return EndPoint;
        // TODO: Добавить 3ю точку?
    }

    /// <summary>
    /// UpdateEntities
    /// </summary>
    public override void UpdateEntities()
    {
        // TODO: разобрать по примеру mpView
        try
        {
            // var scale = GetScale();
            var scale = GetFullScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента

                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (MinDistanceBetweenPoints * scale),
                    InsertionPointOCS.Y, InsertionPointOCS.Z);
                CreateEntities(InsertionPointOCS, tmpEndPoint, scale);
            }
            else
            {
                // Задание любой другой точки
                CreateEntities(InsertionPointOCS, EndPointOCS, scale);
            }

            //// Задание первой точки (точки вставки). Она же точка начала отсчета
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    private void CreateEntities(Point3d insertionPoint, Point3d endPoint, double scale)
    {
        // соблюдается минимальная полная длина для создания стрелки
        bool isSetMinDistanse = true;

        var normalVector = (endPoint - insertionPoint).GetNormal();

        // Полная длина
        var fullLength = endPoint.DistanceTo(insertionPoint);

        // Длина стрелки 
        var arrowLength = ArrowLength * scale;

        // Длина полки
        double shelfLength;

        // Ширина стрелки у основания
        double arrowWidth = 0;

        // Если длина стрелки не превышает заданные значения
        if (arrowLength < (ArrowQuantity == 1 ? fullLength * 0.9 : fullLength / 2))
        {
            shelfLength = fullLength - (ArrowQuantity == 1 ? arrowLength : 2 * arrowLength);
            arrowWidth = ArrowWidth * scale;
        }
        else
        {
            isSetMinDistanse = false;
            shelfLength = fullLength;
        }

        // Точка конца полки
        var lineEndPoint = insertionPoint + (normalVector *
            (ArrowQuantity == 1 ? shelfLength : shelfLength + arrowLength));

        // Точка основания второй стрелки
        var secondArrowPoint = default(Point3d);

        // Линия полки
        _shelf = new Polyline(2);
        
        _shelf.AddVertexAt(0, lineEndPoint.ToPoint2d(), 0.0, ShelfWidth * scale, ShelfWidth * scale);
        if (ArrowQuantity == 1)
            _shelf.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, ShelfWidth * scale, ShelfWidth * scale);
        else
        {
            secondArrowPoint = insertionPoint + (normalVector * arrowLength);
            _shelf.AddVertexAt(1, secondArrowPoint.ToPoint2d(), 0.0, ShelfWidth * scale, ShelfWidth * scale);

        }

        // Линия стрелки
        if (isSetMinDistanse) // Если места для стрелок достаточно
        {
            _firstArrow = new Polyline(2);
            _firstArrow.AddVertexAt(0, endPoint.ToPoint2d(), 0.0, 0.0, arrowWidth);
            _firstArrow.AddVertexAt(1, lineEndPoint.ToPoint2d(), 0.0, arrowWidth, arrowWidth);

            if (ArrowQuantity == 2)
            {
                _secondArrow = new Polyline(2);
                _secondArrow.AddVertexAt(0, insertionPoint.ToPoint2d(), 0.0, 0.0, arrowWidth);
                _secondArrow.AddVertexAt(1, secondArrowPoint.ToPoint2d(), 0.0, arrowWidth, arrowWidth);
            }
        }
        else
        {
            // Удаляется стрелка 1
            _firstArrow = null;
            // Удаляется стрелка 2
            _secondArrow = null;

            // На всю длину создается полка без стрелок
            _shelf = new Polyline(2);
            _shelf.AddVertexAt(0, (insertionPoint + (normalVector * fullLength)).ToPoint2d(),
                0.0, ShelfWidth * scale, ShelfWidth * scale);
            _shelf.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, ShelfWidth * scale, ShelfWidth * scale);
        }

    }

}
