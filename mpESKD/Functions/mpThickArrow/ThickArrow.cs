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
    /// Стрелка 
    /// </summary>
    private Polyline _arrow;

    /// <summary>
    /// Линия стрелки
    /// </summary>
    private Polyline _lineArrow;
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
    public override double MinDistanceBetweenPoints => ArrowThick * 20;

    /*
    /// <summary>
    /// Длина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 1, "p47", 5, 1, 10, nameSymbol: "l")]
    [SaveToXData]
    public int ArrowLength { get; set; } = 5;*/

    /// <summary>
    /// Толщина стрелки
    /// </summary>
    [EntityProperty(PropertiesCategory.Geometry, 2, "p48", 0.5, 0.1, 5, nameSymbol: "t")]
    [SaveToXData]
    public double ArrowThick { get; set; } = 0.5;

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
                //_shelfLine,
                _arrow,
                _lineArrow,
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
        var normalVector = (endPoint - insertionPoint).GetNormal();

        var length = endPoint.DistanceTo(insertionPoint);

        // Длина л стрелки
        var lengthArrow = 10 * ArrowThick * scale;

        // Ширина стрелки у основания
        var wideArrow = 3 * ArrowThick * scale;

        // Длина линии до начала стрелки
        var lengthLine = length - lengthArrow;

        // shelf line
        var lineEndPoint = insertionPoint + (normalVector * lengthLine * scale);

        //TopShelfEndPoint = lineEndPoint.TransformBy(BlockTransform);
        //var tmpEndPoint = insertionPoint + (normalVector * ShelfLength * scale);

        /*
        _shelfLine = new Polyline
        {
            StartPoint = insertionPoint,
            EndPoint = tmpEndPoint
        };*/

        // Линия до начала стрелки
        _arrow = new Polyline(2);
        _arrow.AddVertexAt(0, lineEndPoint.ToPoint2d(), 0.0, ArrowThick * scale, ArrowThick * scale);
        _arrow.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, ArrowThick * scale, ArrowThick * scale);

        // Стрелка
        _lineArrow = new Polyline(2);
        //var endArrowEndPoint= lineEndPoint + (normalVector * 5 * scale);
        //var endArrowStartPoint= lineEndPoint;
        _lineArrow.AddVertexAt(0, endPoint.ToPoint2d(), 0.0, 0.0, wideArrow);
        _lineArrow.AddVertexAt(1, lineEndPoint.ToPoint2d(), 0.0, wideArrow, wideArrow);


        // shelf arrow
        /*
        var topShelfArrowStartPoint = insertionPoint + (normalVector * ShelfArrowLength * scale);
        _Arrow = new Polyline(2);
        _Arrow.AddVertexAt(0, topShelfArrowStartPoint.ToPoint2d(), 0.0, ShelfArrowWidth * scale, 0.0);
        _Arrow.AddVertexAt(1, insertionPoint.ToPoint2d(), 0.0, 0.0, 0.0);*/

    }

}
