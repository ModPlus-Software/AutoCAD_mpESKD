namespace mpESKD.Base.Abstractions;

using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

/// <summary>
/// Линейный интеллектуальный объект
/// </summary>
public interface ILinearEntity
{
    /// <summary>
    /// Первая точка примитива в мировой системе координат.
    /// Должна соответствовать точке вставке блока
    /// </summary>
    Point3d InsertionPoint { get; set; }

    /// <summary>
    /// Конечная точка примитива в мировой системе координат. Свойство содержится в базовом классе для
    /// работы <see cref="DefaultEntityJig"/>. Имеется в каждом примитиве, но
    /// если не требуется, то просто не использовать её
    /// </summary>
    Point3d EndPoint { get; set; }

    /// <summary>
    /// Промежуточные точки
    /// </summary>
    List<Point3d> MiddlePoints { get; set; }

    /// <summary>
    /// Длина линии
    /// </summary>
    double Length { get; }

    /// <summary>
    /// Указывает, что для интеллектуального объекта включено "легкое" создание (рисование)
    /// </summary>
    bool IsLightCreation { get; set; }

    /// <summary>
    /// Смарт-объект был развернут
    /// </summary>
    bool IsReversed { get; set; }

    /// <summary>
    /// Перестроение точек - помещение EndPoint в список
    /// </summary>
    void RebasePoints();

    /// <summary>
    /// Возвращает все точки линейного объекта. В список добавляется InsertionPoint, затем MiddlePoints и в конце EndPoint
    /// </summary>
    IEnumerable<Point3d> GetAllPoints();

    /// <summary>
    /// Возвращает все точки во внутренней системе координат, используемые для отрисовки.
    /// Учитывает свойство <see cref="IsReversed"/>
    /// </summary>
    /// <param name="endPoint">Конечная точка. Если не null, значит использовать ее вместо <see cref="EndPoint"/></param>
    List<Point3d> GetOcsAll3dPointsForDraw(Point3d? endPoint);
}