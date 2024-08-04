using System.Linq;
using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.Geometry;
using mpESKD.Base.Abstractions;
using System.Collections.Generic;

internal static class Helper
{
    /// <summary>
    ///  Функция возвращает проекцию точки на прямую, заданную двумя точками
    /// </summary>
    /// <param name="pointProj">Проецирумая точка</param>
    /// <param name="linePoint1">Первая точка прямой</param>
    /// <param name="linePoint2">Вторая точка прямой</param>
    /// <returns>Точка на прямой</returns>
    internal static Point3d GetProjectPoint(this Point3d pointProj, Point3d linePoint1, Point3d linePoint2)
    {
        using (var line = new Line3d(linePoint1, linePoint2))
        {
            var vectorToLine = (linePoint2 - linePoint1).GetPerpendicularVector();

            return line.GetProjectedClosestPointTo(pointProj, vectorToLine).Point;
        }
    }

    /// <summary>
    /// Возвращает перпендикулярную проекцию точки на центральную линию смарт объекта 
    /// </summary>
    /// <param name="point">Проецируемая точка в координатах модели</param>
    /// <param name="crestedLeader">смарт объект</param>
    /// <returns>Точка проекции в координатах модели</returns>
    /// <remarks>Например, точки курсора</remarks>
    internal static Point3d GetProjectPointToBaseLine(this Point3d point, CrestedLeader crestedLeader)
    {
        return GetProjectPoint(point, crestedLeader.InsertionPoint, crestedLeader.BaseSecondPoint);
    }

    /// <summary>
    /// Пересчет точки из OCS в координаты модели
    /// </summary>
    /// <param name="point3dOcs">Точка в координатах смарт объекта</param>
    /// <param name="smartEntity">Смарт объект</param>
    /// <returns>Точка в координатах модели</returns>
    internal static Point3d Point3dOcsToPoint3d(this Point3d point3dOcs, ISmartEntity smartEntity)
    {
        return smartEntity.InsertionPoint + (point3dOcs - smartEntity.InsertionPointOCS);
    }

    /// <summary>
    /// Пересчет точки из OCS в координаты модели
    /// </summary>
    /// <param name="point2dOcs">Точка в координатах смарт объекта</param>
    /// <param name="smartEntity">Смарт объект</param>
    /// <returns>Точка в координатах модели.<br/>
    /// Используется для обработки результата метода <see cref="Intersections"/>.GetIntersectionBetweenVectors</returns>
    internal static Point3d Point3dOcsToPoint3d(this Point2d? point2dOcs, ISmartEntity smartEntity)
    {
        if (point2dOcs is { } point2dResult)
        {
            return point2dResult.ToPoint3d().Point3dOcsToPoint3d(smartEntity);
        }

        return Point3d.Origin;
    }

    /// <summary>
    /// Сортирует список точек по возрастанию X первого параметра по отсортированному списку второго параметра
    /// </summary>
    /// <param name="points3d">Список точек для сортировки</param>
    /// <param name="points3dOcs">Список точек как шаблон для сортировки </param>
    /// <returns>Список точек</returns>
    /// <remarks>Второй параметр содержит список точек с относительными координатами по блоку, этот список сортируется по X,
    /// затем точки списка первого параметра переставляются в соответствии с отсортированным списком относительных точек.
    /// Получаем точки в координатах модели последовательно слева направо от первой выноски слева до крайней справа </remarks>
    internal static List<Point3d> OrderByBaseLine(this List<Point3d> points3d, List<Point3d> points3dOcs)
    {
        if (points3d != null && points3d.Count > 0)
        {
            var points3dOcsSort = points3dOcs.OrderBy(p => p.X).ToList();

            List<Point3d> points3dSort = new();
            for (int i = 0; i < points3dOcsSort.Count; i++)
            {
                var indexInPoints3dOcs = points3dOcs.IndexOf(points3dOcsSort[i]);
                points3dSort.Add(points3d.ElementAt(indexInPoints3dOcs));
            }

            return points3dSort;
        }

        return new List<Point3d>();
    }
    
    /// <summary>
    /// Возвращает 2d вектор из 3d вектора
    /// </summary>
    /// <param name="vector3d"></param>
    /// <returns></returns>
    internal static Vector2d ToVector2d(this Vector3d vector3d)
    {
        //return  vector3d.Convert2d(new Plane(Point3d.Origin, vector3d.GetNormal()));

        var ptEnd = Point3d.Origin + vector3d;

        var ptEnd2d = ptEnd.ToPoint2d();
        var ptStart2d = Point2d.Origin;

        var vec2d = ptEnd2d - ptStart2d;
        return vec2d;
    }
}