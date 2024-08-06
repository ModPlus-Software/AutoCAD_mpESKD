namespace mpESKD.Functions.mpCrestedLeader;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Abstractions;
using Base.Utils;

/// <summary>
/// Вспомогательные методы
/// </summary>
internal static class Helper
{
    /// <summary>
    ///  Функция возвращает перпендикулярную проекцию точки на прямую, заданную двумя точками
    /// </summary>
    /// <param name="pointProj">Проецирумая точка</param>
    /// <param name="linePoint1">Первая точка прямой</param>
    /// <param name="linePoint2">Вторая точка прямой</param>
    /// <returns>Перпендикулярная проекция точки на прямую</returns>
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
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see>т</param>
    /// <returns>Перпендикулярная проекция точки на центральную линию смарт объекта в координатах модели</returns>
    /// <remarks>Например, проекция точки курсора</remarks>
    internal static Point3d GetProjectPointToBaseLine(this Point3d point, CrestedLeader crestedLeader)
    {
        return GetProjectPoint(point, crestedLeader.InsertionPoint, crestedLeader.BaseSecondPoint);
    }

    /// <summary>
    /// Возвращает точку, пересчитанную из относительных координат смарт объекта в координаты модели
    /// </summary>
    /// <param name="pointOcs">Точка в координатах смарт объекта</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns>Точка в координатах модели</returns>
    internal static Point3d Point3dOcsToPoint3d(this Point3d pointOcs, ISmartEntity smartEntity)
    {
        return smartEntity.InsertionPoint + (pointOcs - smartEntity.InsertionPointOCS);
    }

    /// <summary>
    /// Возвращает точку, пересчитанную из координат модели в координаты смарт объекта
    /// </summary>
    /// <param name="point">Точка в координатах модели</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns></returns>
    internal static Point3d Point3dToPoint3dOcs(this Point3d point, ISmartEntity smartEntity)
    {
        return point.TransformBy(smartEntity.BlockTransform.Inverse());
    }

    /// <summary>
    /// Возвращает отсортированный (по центральной линии смарт объекта) список точек по шаблону, в координатах модели
    /// </summary>
    /// <param name="points3d">Список точек для сортировки, в координатах модели</param>
    /// <param name="points3dOcs">Список точек как шаблон для сортировки, в координатах смарт объекта</param>
    /// <returns>Список точек в координатах модели, отсортированный по центральной линии смарт объекта</returns>
    /// <remarks>Второй параметр содержит список точек с относительными координатами по блоку, этот список сортируется по X,<br/>
    /// затем точки списка первого параметра переставляются в соответствии с отсортированным списком относительных точек.<br/><br/>
    /// Результат - список точек первого параметра в координатах модели, последовательно слева направо вдоль центральной линии смарт объекта</remarks>
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
    /// Возвращает список точек начал выносок, отсортированный по центральной линии смарт объекта, в координатах модели
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    internal static List<Point3d> OrderLeaderStartPoints(this CrestedLeader crestedLeader)
    {
       return crestedLeader.LeaderStartPoints.OrderByBaseLine(crestedLeader.LeaderStartPointsOCS);
    }

    /// <summary>
    /// Возвращает список точек концов выносок, отсортированный по центральной линии смарт объекта, в координатах модели
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    internal static List<Point3d> OrderLeaderEndPoints(this CrestedLeader crestedLeader)
    {
        return crestedLeader.LeaderEndPoints.OrderByBaseLine(crestedLeader.LeaderStartPointsOCS);
    }

    /// <summary>
    /// Возвращает 2d вектор из 3d вектора
    /// </summary>
    /// <param name="vector3d"> 2d вектор</param>
    internal static Vector2d ToVector2d(this Vector3d vector3d)
    {
        var ptEnd = Point3d.Origin + vector3d;

        var ptEnd2d = ptEnd.ToPoint2d();
        var ptStart2d = Point2d.Origin;

        var vec2d = ptEnd2d - ptStart2d;
        return vec2d;
    }

    /// <summary>
    /// Возвращает точку, проверенную на приближение к набору точек
    /// </summary>
    /// <param name="point">Проверяемая точка</param>
    /// <param name="targetPoints">Список точек (набор) по которым выполняется проверка</param>
    /// <param name="distance">Критическое расстояние от проверяемой точки</param>
    /// <remarks>При приближении к одной из точек набора на расстояние меньше критического<br/>
    /// возвращается точка пересечения окружности (с центром в точке из набора и радиусом <paramref name="distance"/>)<br/>
    /// и вектора от центра окружности к проверяемой точке
    /// </remarks>
    internal static Point3d GetNormalizedPointByDistToPointSet(this Point3d point, List<Point3d> targetPoints, double distance)
    {
        var searchPoint = point.GetNearestPoint(targetPoints);

        // Точка пересечения окружности с радиусом distance и отрезка к searchPoint
        var lineStartPoint = searchPoint + ((point - searchPoint) * distance * 2);

        var line = new Line(lineStartPoint, searchPoint);

        var circle = new Circle()
        {
            Center = searchPoint,
            Radius = distance,
        };

        var intersectPoint = Intersections.GetIntersectionBetweenCircleLine(line, circle);
        if (intersectPoint != null)
        {
            return intersectPoint.Value;
        }

        return point;
    }

    /// <summary>
    /// Возвращает точку из набора, ближайшую к проверяемой точке
    /// </summary>
    /// <param name="point">Проверяемая точка</param>
    /// <param name="targetPoints">Список точек (набор) по которым выполняется проверка</param>
    internal static Point3d GetNearestPoint(this Point3d point, List<Point3d> targetPoints)
    {
        if (targetPoints != null && targetPoints.Count > 0)
        {
            if (targetPoints.Count == 1)
            {
                return targetPoints[0];
            }

            var minDistance = point.DistanceTo(targetPoints[0]);
            var indexSearch = 0;

            for (int i = 1; i <= targetPoints.Count - 1; i++)
            {
                var iDist = point.DistanceTo(targetPoints[i]);

                if (iDist < minDistance)
                {
                    minDistance = iDist;
                    indexSearch = i;
                }
            }

            return targetPoints[indexSearch];
        }

        return point; 
    }
}