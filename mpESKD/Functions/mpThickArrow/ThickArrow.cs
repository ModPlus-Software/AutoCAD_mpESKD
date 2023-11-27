namespace mpESKD.Functions.mpThickArrow;

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base;
using Base.Abstractions;
using Base.Attributes;
using Base.Enums;
using Base.Utils;
using ModPlusAPI.Windows;

/// <summary>
/// Толстая стрелка
/// </summary>
[SmartEntityDisplayNameKey("h187")]
[SystemStyleDescriptionKey("h188")]
public class ThickArrow : SmartEntity, IWithDoubleClickEditor
{
    #region Entities

    /// <summary>
    /// Верхняя полка
    /// </summary>
    private Polyline _shelfLine;

    /// <summary>
    /// Стрелка 
    /// </summary>
    private Polyline _shelfArrow;

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

    // TODO: Задавать, даже если не использ текст? Можно обойти исполльзование?
    /// <inheritdoc />
    /// В примитиве не используется!
    [EntityProperty(PropertiesCategory.Content, 1, "p41", "Standard", descLocalKey: "d41")]
    [SaveToXData]
    public override string TextStyle { get; set; }

    #endregion 

    /// <inheritdoc />
    public override double MinDistanceBetweenPoints => 0.2;

    /// <summary>
    /// Длина полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p46", 10, 5, 15, nameSymbol: "w")]
    [SaveToXData]
    public int ShelfLength { get; set; } = 10;

    /// <summary>
    /// Толщина полки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 5, "p46", 10, 5, 15, nameSymbol: "w")]
    [SaveToXData]
    public double ShelfWidth { get; set; } = 0.01;

    /// <summary>
    /// Длина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 6, "p47", 5, 1, 8, nameSymbol: "e")]
    [SaveToXData]
    public int ShelfArrowLength { get; set; } = 5;

    /// <summary>
    /// Толщина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 7, "p48", 1.5, 0.1, 5, nameSymbol: "t")]
    [SaveToXData]
    public double ShelfArrowWidth { get; set; } = 1.5;

    /// <summary>
    /// Конечная точка верхней полки
    /// </summary>
    [SaveToXData]
    public Point3d TopShelfEndPoint { get; private set; }

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
                _shelfLine,
                _shelfArrow,
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
            var scale = GetScale();
            if (EndPointOCS.Equals(Point3d.Origin))
            {
                // Задание точки вставки. Второй точки еще нет - отрисовка типового элемента

                var tmpEndPoint = new Point3d(
                    InsertionPointOCS.X + (ShelfLength * scale), InsertionPointOCS.Y, InsertionPointOCS.Z);
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
        var normalVector = (endPoint - insertionPoint).GetNormal();

        // shelf line
        var shelfEndPoint = insertionPoint + (normalVector * ShelfLength * scale);
        TopShelfEndPoint = shelfEndPoint.TransformBy(BlockTransform);
        var tmpEndPoint = insertionPoint + (normalVector * ShelfLength * scale);

        /*
        _shelfLine = new Polyline
        {
            StartPoint = insertionPoint,
            EndPoint = tmpEndPoint
        };*/

        _shelfLine = new Polyline(2);
        _shelfLine.AddVertexAt(0, tmpEndPoint.ToPoint2d(), 0.0, 0.1, 0.0);
        _shelfLine.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, 0.1, 0.0);

        // shelf arrow
        var topShelfArrowStartPoint = insertionPoint + (normalVector * ShelfArrowLength * scale);
        _shelfArrow = new Polyline(2);
        _shelfArrow.AddVertexAt(0, topShelfArrowStartPoint.ToPoint2d(), 0.0, ShelfArrowWidth * scale, 0.0);
        _shelfArrow.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, 0.0, 0.0);

    }

}
