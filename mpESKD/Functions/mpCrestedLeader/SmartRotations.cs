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
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"</param>
    /// <returns>Точка</returns>
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
    /// Точка начала выноски
    /// </summary>
    /// <param name="crestedLeader"></param>
    /// <param name="leaderStartPointOcs">.</param>
    /// <returns>.</returns>
    /// <remarks>Совпадает с точкой вставки блока смарт объекта</remarks>
    internal static Point3d GetLeaderStartPoint(this CrestedLeader crestedLeader, Point2d? leaderStartPointOcs)
    {
        var leaderStartPointOcs3d = leaderStartPointOcs.Value.ToPoint3d();

        if (crestedLeader.IsRotated)
        {
            return crestedLeader.InsertionPoint +
                   (leaderStartPointOcs3d.GetRotatedPointOcsByBlock(crestedLeader) - crestedLeader.InsertionPointOCS);
        }

        return crestedLeader.InsertionPoint + (leaderStartPointOcs3d - crestedLeader.InsertionPointOCS);
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
}