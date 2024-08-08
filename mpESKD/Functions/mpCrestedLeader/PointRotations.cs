using System;

namespace mpESKD.Functions.mpCrestedLeader;

using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Base.Abstractions;
using Base.Enums;

/// <summary>
/// Поворот точек смарт объекта
/// </summary>
internal static class PointRotations
{
    /// <summary>
    /// Возвращает точку в координатах смарт объекта, повернутую в соответствии с вращением смарт объекта.
    /// </summary>
    /// <param name="pointOcs">Точка, которую нужно повернуть, в координатах смарт объекта</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns>Повернутая точка в координатах смарт объекта</returns>
    /// <remarks>Поворот выполняется вокруг точки вставки. Точка вставки в координатах смарт объекта</remarks>
    internal static Point3d GetRotatedPointOcsByBlock(this Point3d pointOcs, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPointOCS);

        return pointOcs.TransformBy(rotationMatrix);
    }

    /// <summary>
    /// Возвращает точку в координатах модели, повернутую в соответствии с вращением смарт объекта
    /// </summary>
    /// <param name="point">Точка, которую нужно повернуть, в координатах модели</param>
    /// <param name="smartEntity">Экземпляр объекта, наследующий <see cref="ISmartEntity"></see></param>
    /// <returns>Повернутая точка в координатах модели</returns>
    /// <remarks>Поворот выполняется вокруг точки вставки. Точка вставки в координатах модели</remarks>
    internal static Point3d GetRotatedPointByBlock(this Point3d point, ISmartEntity smartEntity)
    {
        var rotationMatrix = Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPoint);

        return point.TransformBy(rotationMatrix);
    }

    /// <summary>
    /// Возвращает точку начала полки
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    internal static Point3d GetShelfStartPoint(this CrestedLeader crestedLeader)
    {
        var leaderStartPointsOcsSort = crestedLeader.LeaderStartPointsOCS.OrderBy(p => p.X).ToList();

        if (crestedLeader.ScaleFactorX == -1)
        {
            leaderStartPointsOcsSort.Reverse();
        }

        //leaderStartPointsOcsSort.ToLog("leaderStartPointsOcsSort");

        Point3d shelfStartPointOcs;
        if (crestedLeader.IsRotated)
        {
            shelfStartPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? (leaderStartPointsOcsSort.Last()).GetRotatedPointOcsByBlock(crestedLeader)
                : (leaderStartPointsOcsSort.First()).GetRotatedPointOcsByBlock(crestedLeader);
        }
        else
        {
            shelfStartPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? leaderStartPointsOcsSort.Last()
                : leaderStartPointsOcsSort.First();
        }

        return shelfStartPointOcs.Point3dOcsToPoint3d(crestedLeader);
    }

    /// <summary>
    /// Возвращает точку отступа полки
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    internal static Point3d GetShelfLedgePoint(this CrestedLeader crestedLeader)
    {
        var vectorToShelfLedge = Vector3d.XAxis * crestedLeader.ShelfLedge;
        /*
        bool isMirroring = (crestedLeader.ScaleFactorX <= 0 && !CommandsWatcher.Mirroring) ||
                           (crestedLeader.ScaleFactorX >= 0 && CommandsWatcher.Mirroring);
        
        if (isMirroring)
        {
            vectorToShelfLedge = -vectorToShelfLedge;
        }*/

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
    /// Возвращает точку конца полки
    /// </summary>
    /// <param name="crestedLeader">Экземпляр объекта <see cref="CrestedLeader"></see></param>
    /// <param name="widthWidestText">Длина самого широкого текста</param>
    internal static Point3d GetShelfEndPoint(this CrestedLeader crestedLeader, double widthWidestText)
    {
        // var vectorToShelfEndpoint = Vector3d.XAxis * ((crestedLeader.TextIndent * crestedLeader.GetScale()) + widthWidestText);
        var vectorToShelfEndpoint = Vector3d.XAxis * 
                                    (crestedLeader.ShelfLedge + (crestedLeader.TextIndent * crestedLeader.GetScale()) + widthWidestText);
        /*
        if (crestedLeader.ScaleFactorX == -1)
        {
        
            vectorToShelfEndpoint = -vectorToShelfEndpoint;
        }
        */

        Point3d shelfEndPointOcs;
        /*
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
        */
        if (crestedLeader.IsRotated)
        {
            shelfEndPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? (crestedLeader.ShelfStartPointOCS + vectorToShelfEndpoint).GetRotatedPointOcsByBlock(crestedLeader)
                : (crestedLeader.ShelfStartPointOCS - vectorToShelfEndpoint).GetRotatedPointOcsByBlock(crestedLeader);
        }
        else
        {
            shelfEndPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? crestedLeader.ShelfStartPointOCS + vectorToShelfEndpoint
                : crestedLeader.ShelfStartPointOCS - vectorToShelfEndpoint;
        }


        return shelfEndPointOcs.Point3dOcsToPoint3d(crestedLeader);
    }
}