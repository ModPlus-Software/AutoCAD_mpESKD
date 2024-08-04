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
    /// Возвращает точку в координатах смарт объекта, повернутую в соответствии с вращением смарт объекта.
    /// </summary>
    /// <param name="point3d">Точка, которую нужно повернуть, в координатах смарт объекта</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns>Повернутая точка в координатах смарт объекта</returns>
    /// <remarks>Поворот выполняется вокруг точки вставки. Точка вставки в координатах смарт объекта</remarks>
    internal static Point3d GetRotatedPointOcsByBlock(this Point3d point3dOcs, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPointOCS);

        return point3dOcs.TransformBy(rotationMatrix);
    }

    /// <summary>
    /// Возвращает точку в координатах модели, повернутую в соответствии с вращением смарт объекта
    /// </summary>
    /// <param name="point3d">Точка, которую нужно повернуть, в координатах модели</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns>Повернутая точка в координатах модели</returns>
    /// <remarks>Поворот выполняется вокруг точки вставки. Точка вставки в координатах модели</remarks>
    internal static Point3d GetRotatedPointByBlock(this Point3d point3d, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPoint);

        return point3d.TransformBy(rotationMatrix);
    }



    //internal static Point3d

    /// <summary>
    /// Возвращает точку начала выноски в координатах модели с учетом вращения смарт объекта
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="CrestedLeader"></see></param>
    /// <param name="leaderStartPointOcs">Точка начала выноски в координатах смарт объекта</param>
    /// <returns>Точка начала выноски</returns>
    /// <remarks>Используется для обработки результата метода <see cref="Intersections"/>.GetIntersectionBetweenVectors</remarks>
    internal static Point3d GetLeaderStartPoint(this CrestedLeader crestedLeader, Point2d? leaderStartPointOcs)
    {
        var leaderStartPointOcs3d = leaderStartPointOcs.Value.ToPoint3d();

        if (crestedLeader.IsRotated)
        {
            return crestedLeader.InsertionPoint +
                   (leaderStartPointOcs3d.GetRotatedPointOcsByBlock(crestedLeader) - crestedLeader.InsertionPointOCS);
        }

        // return crestedLeader.InsertionPoint + (leaderStartPointOcs3d - crestedLeader.InsertionPointOCS);
        return leaderStartPointOcs3d.Point3dOcsToPoint3d(crestedLeader);
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

        // return crestedLeader.InsertionPoint + (shelfLedgePointOcs - crestedLeader.InsertionPointOCS);
        return shelfLedgePointOcs.Point3dOcsToPoint3d(crestedLeader);
    }

    /// <summary>
    /// Точка конца полки
    /// </summary>
    /// <param name="crestedLeader">.</param>
    /// <param name="textWidth">.</param>
    /// <returns>.</returns>
    internal static Point3d GetShelfEndPoint(this CrestedLeader crestedLeader, double textWidth)
    {
        var vectorToShelfEndpoint = Vector3d.XAxis * ((crestedLeader.TextIndent * crestedLeader.GetScale()) + textWidth);

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

        // return crestedLeader.InsertionPoint + (shelfEndPointOcs - crestedLeader.InsertionPointOCS);
        return shelfEndPointOcs.Point3dOcsToPoint3d(crestedLeader);
    }

    /*
    internal static Point3d GetRotatePointToXaxis(this Point3d point, CrestedLeader crestedLeader)
    {
        var rotationMatrix = Matrix3d.Rotation(-crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.InsertionPoint);

        return point.TransformBy(rotationMatrix);
    }*/
}