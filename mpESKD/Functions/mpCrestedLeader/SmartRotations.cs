using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.Geometry;
using Base.Abstractions;
using Base.Enums;
using Base.Utils;
using ControlzEx.Standard;
using CSharpFunctionalExtensions;
using mpESKD.Base;
using System.Windows;

internal static class SmartRotations
{
    /// <summary>
    /// Возвращает точку, повернутую относительно точки вставки блока смарт объекта
    /// </summary>
    /// <param name="point3dOcs">Точка до синхронизации с вращением блока смарт объекта</param>
    /// <param name="smartEntity">Смарт объект</param>
    /// <returns>Новый экземпляр Point3d, соответствующий вращению блока смарт объекта</returns>
    internal static Point3d GetRotatedPointOcsByBlock(this Point3d point3dOcs, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPointOCS);

        return point3dOcs.TransformBy(rotationMatrix);
    }

    internal static Point3d GetRotatedPointByBlock(this Point3d point3d, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPoint);

        return point3d.TransformBy(rotationMatrix);
    }




    /// <summary>
    /// Точка начала полки
    /// </summary>
    /// <param name="crestedLeader">.</param>
    /// <param name="leaderStartPointOcs">.</param>
    /// <returns>.</returns>
    /// <remarks>Совпадает с точкой вставки блока смарт объекта</remarks>
    internal static Point3d GetLeaderStartPoint(this CrestedLeader crestedLeader, Point2d? leaderStartPointOcs)
    {
        var intersectPointOcs3d = leaderStartPointOcs.Value.ToPoint3d();

        if (crestedLeader.IsRotated)
        {
            return crestedLeader.InsertionPoint +
                   (intersectPointOcs3d.GetRotatedPointOcsByBlock(crestedLeader) - crestedLeader.InsertionPointOCS);
        }

        return crestedLeader.InsertionPoint + (intersectPointOcs3d - crestedLeader.InsertionPointOCS);
    }

    /// <summary>
    /// Точка отступа полки
    /// </summary>
    /// <param name="crestedLeader">.</param>
    /// <returns>.</returns>
    internal static Point3d GetShelfLedgePoint(this CrestedLeader crestedLeader)
    {
        var vectorToShelfLedge = Vector3d.XAxis * crestedLeader.ShelfLedge;

        Point3d shelfLedgePointOcs;
        if (crestedLeader.IsRotated)
        {
            shelfLedgePointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? (crestedLeader.ShelfStartPointOCS + vectorToShelfLedge).GetRotatedPointOcsByBlock(crestedLeader)
                : (crestedLeader.ShelfStartPointOCS - vectorToShelfLedge).GetRotatedPointOcsByBlock(crestedLeader);
        }
        else
        {
            shelfLedgePointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? crestedLeader.ShelfStartPointOCS + vectorToShelfLedge
                : crestedLeader.ShelfStartPointOCS - vectorToShelfLedge;
        }

        return crestedLeader.InsertionPoint + (shelfLedgePointOcs - crestedLeader.InsertionPointOCS);
    }

    /// <summary>
    /// Точка конца полки
    /// </summary>
    /// <param name="crestedLeader">.</param>
    /// <param name="textWidth">.</param>
    /// <returns>.</returns>
    internal static Point3d GetShelfEndPoint(this CrestedLeader crestedLeader, double textWidth)
    {
        var vectorToShelfEndpoint = Vector3d.XAxis * ((crestedLeader.TextIndent * crestedLeader.GetScale())
                                                      + textWidth);

        Point3d shelfEndPointOcs;
        if (crestedLeader.IsRotated)
        {
            shelfEndPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? (crestedLeader.ShelfLedgePointOCS + vectorToShelfEndpoint).GetRotatedPointOcsByBlock(crestedLeader)
                : (crestedLeader.ShelfLedgePointOCS - vectorToShelfEndpoint).GetRotatedPointOcsByBlock(crestedLeader);
        }
        else
        {
            shelfEndPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? crestedLeader.ShelfLedgePointOCS + vectorToShelfEndpoint
                : crestedLeader.ShelfLedgePointOCS - vectorToShelfEndpoint;
        }

        return crestedLeader.InsertionPoint + (shelfEndPointOcs - crestedLeader.InsertionPointOCS);
    }


    internal static Point3d GetRotatePointToXaxis(this Point3d point, CrestedLeader crestedLeader)
    {
        var rotationMatrix = Matrix3d.Rotation(-crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.InsertionPoint);

        return point.TransformBy(rotationMatrix);
    }

    // OCS точки вращать? они точно относительно, в координатах блока
    internal static Point3d GetRotatePointOcsToXaxis(this Point3d pointOcs, CrestedLeader crestedLeader)
    {
        var rotationMatrix = Matrix3d.Rotation(-crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.InsertionPointOCS);

        return pointOcs.TransformBy(rotationMatrix);
    }


    // Пересчет точки из OCS в норм
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

        List<Point3d> points3dSort = new ();
        for (int i = 0; i < points3dOcsSort.Count; i++)
        {
            var indexInPoints3dOcs = points3dOcs.IndexOf(points3dOcsSort[i]);
            points3dSort.Add(points3d.ElementAt(indexInPoints3dOcs));
        }

        return points3dSort;
    }

    /// <summary>
    ///  Функция возвращает проекцию точки на прямую, заданную
    ///  двумя точками. Проекция по оси Y
    /// </summary>
    /// <param name="p">Проецирумая точка</param>
    /// <param name="p1">первая точка прямой</param>
    /// <param name="p2">вторая точка прямой</param>
    /// <returns></returns>
    /// <remarks>Модифицировано из <see href="https://adn-cis.org/forum/index.php?topic=9772.15"/></remarks>
    internal static Point3d GetProjectPoint(this Point3d p, Point3d p1, Point3d p2)
    {
        using (var line = new Line3d(p1, p2)) 
        {
            var vector = (p2 - p1).GetPerpendicularVector();

            return line.GetProjectedClosestPointTo(p, vector).Point;
        }
    }

    /// <summary>
    /// Перпендикулярная проекция точки на центральную линию смарт объекта 
    /// </summary>
    /// <param name="point">Проецируемая точка</param>
    /// <param name="crestedLeader">смарт объект</param>
    /// <returns>Точка проекции</returns>
    /// <remarks>Например, точки курсора</remarks>
    internal static Point3d GetProjectPointToBaseLine(this Point3d point, CrestedLeader crestedLeader)
    {
        return GetProjectPoint(point, crestedLeader.InsertionPoint, crestedLeader.BaseSecondPoint);
    }
}