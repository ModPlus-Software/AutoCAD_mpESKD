using System.Linq;

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
    /// <param name="linePoint2">Пторая точка прямой</param>
    /// <returns>Точка на линии</returns>
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
    /// <param name="point">Проецируемая точка</param>
    /// <param name="crestedLeader">смарт объект</param>
    /// <returns>Точка проекции</returns>
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
        var points3dOcsSort = points3dOcs.OrderBy(p => p.X).ToList();

        List<Point3d> points3dSort = new();
        for (int i = 0; i < points3dOcsSort.Count; i++)
        {
            var indexInPoints3dOcs = points3dOcs.IndexOf(points3dOcsSort[i]);
            points3dSort.Add(points3d.ElementAt(indexInPoints3dOcs));
        }

        return points3dSort;
    }



}