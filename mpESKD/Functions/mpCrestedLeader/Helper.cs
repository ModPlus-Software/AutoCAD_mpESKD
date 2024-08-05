using System.Linq;
using System.Windows.Navigation;
using Autodesk.AutoCAD.DatabaseServices;
using ControlzEx.Standard;
using DocumentFormat.OpenXml.Bibliography;
using mpESKD.Base;
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
    /// Получаем точки первого параметра в координатах модели последовательно слева направо от первой выноски слева до крайней справа </remarks>
    internal static List<Point3d> OrderByBaseLine(this List<Point3d> points3d, List<Point3d> points3dOcs)
    {
        if (points3d != null && points3d.Count > 0)
        {
            var points3dOcsSort = points3dOcs.OrderBy(p => p.X).ToList();

            List<Point3d> points3dSort = new();
            for (int i = 0; i < points3dOcsSort.Count; i++)
            {
                var indexInPoints3dOcs = points3dOcs. IndexOf(points3dOcsSort[i]);
                points3dSort.Add(points3d.ElementAt(indexInPoints3dOcs));
            }

            return points3dSort;
        }

        return new List<Point3d>();
    }


    internal static List<Point3d> OrderLeaderStartPoints(this CrestedLeader crestedLeader)
    {
       return crestedLeader.LeaderStartPoints.OrderByBaseLine(crestedLeader.LeaderStartPointsOCS);
    }

    internal static List<Point3d> OrderLeaderEndPoints(this CrestedLeader crestedLeader)
    {
        return crestedLeader.LeaderEndPoints.OrderByBaseLine(crestedLeader.LeaderStartPointsOCS);
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

    // Проверка на приближение курсора к концам выносок
    internal static Point3d GetNormalizedPointByDistToPointSet(this Point3d checkPoint, List<Point3d> pointSet, double dist)
    {
        //if (pointSet.Any(p => checkPoint.ToPoint2d().GetDistanceTo(p.ToPoint2d()) < dist))
        //{

        /*
            var searchLeaderEndPoint = pointSet
                .Select(leaderEndPoint => new
                {
                    Point = leaderEndPoint,
                    Distance = leaderEndPoint.ToPoint2d().GetDistanceTo(checkPoint.ToPoint2d())
                })
                .OrderBy(p => p.Distance)
                .First();

            var searchPoint = searchLeaderEndPoint.Point;
        */
         var searchPoint = checkPoint.GetNearestPoint(pointSet);

       // Найдем точку пересечения окружности с радиусом dist и отрезка к searchPoint
       var lineStartPoint = searchPoint + ((checkPoint - searchPoint) * dist * 2);

            var line = new Line(lineStartPoint, searchPoint);
            var circle = new Circle()
            {
                Center = searchPoint,
                Radius = dist,
            };

            var intersectPoint = Intersections.GetIntersectionBetweenCircleLine(line, circle);
            if (intersectPoint != null)
            {
                return intersectPoint.Value;
            }
        //}

        return checkPoint;
    }

    internal static Point3d GetNearestPoint(this Point3d point, List<Point3d> targetPoints)
    {
        if (targetPoints != null && targetPoints.Count > 0)
        {
            if (targetPoints.Count == 1)
            {
                return targetPoints[0];
            }

            var minDist = point.DistanceTo(targetPoints[0]);
            var indexSearch = 0;

            for (int i = 1; i <= targetPoints.Count - 1; i++)
            {
                var iDist = point.DistanceTo(targetPoints[i]);

                if (iDist < minDist)
                {
                    minDist = iDist;
                    indexSearch = i;
                }
            }

            return targetPoints[indexSearch];
        }

        return point; 
    }


    internal static Point3d Point3dToPoint3dOcs(this Point3d point3d, ISmartEntity smartEntity)
    {
        return point3d.TransformBy(smartEntity.BlockTransform.Inverse());
    }
}
