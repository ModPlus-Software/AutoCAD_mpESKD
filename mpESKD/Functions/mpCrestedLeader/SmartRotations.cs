
using Autodesk.AutoCAD.Geometry;
using ControlzEx.Standard;
using mpESKD.Base.Abstractions;
using mpESKD.Base.Enums;
using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpCrestedLeader;

internal static class SmartRotations
{
    /// <summary>
    /// Возвращает точку, повернутую относительно точки вставки блока смарт объекта
    /// </summary>
    /// <param name="point">Точка до синхронизации с вращением блока смарт объекта</param>
    /// <param name="smartEntity">Смарт объект</param>
    /// <returns>Новый экземпляр Point3d, соответствующий вращению блока смарт объекта</returns>
    internal static Point3d RotateByBlock(this Point3d point, ISmartEntity smartEntity)
    {
        var rotationMatrix = smartEntity.GetRotationMatrix();

        //return (new Point3d(point.X, point.Y, point.Z)).TransformBy(rotationMatrix);
        return point.TransformBy(rotationMatrix);
    }

    /// <summary>
    /// Возвращает матрицу для поворота объектов
    /// </summary>
    /// <param name="smartEntity">.</param>
    /// <returns>.</returns>
    internal static Matrix3d GetRotationMatrix(this ISmartEntity smartEntity)
    {
        return Matrix3d.Rotation(smartEntity.Rotation, Vector3d.ZAxis, smartEntity.InsertionPointOCS);
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
            // this.ToLogAnyString($"CreateLeaderLines():  Rotation: {Rotation.ToString()}");
            // this.ToLogAnyString($"CreateLeaderLines():  Rotation: {Rotation.RadiansToDegrees().ToString()}");

            return crestedLeader.InsertionPoint + 
                (intersectPointOcs3d.RotateByBlock(crestedLeader) - crestedLeader.InsertionPointOCS);
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
                ? (crestedLeader.ShelfStartPointOCS + vectorToShelfLedge).RotateByBlock(crestedLeader)
                : (crestedLeader.ShelfStartPointOCS - vectorToShelfLedge).RotateByBlock(crestedLeader);
        }
        else
        {
            shelfLedgePointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? crestedLeader.ShelfStartPointOCS + vectorToShelfLedge
                : crestedLeader.ShelfStartPointOCS - vectorToShelfLedge;
        }

        return  crestedLeader.InsertionPoint + (shelfLedgePointOcs - crestedLeader.InsertionPointOCS);
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
                ? (crestedLeader.ShelfLedgePointOCS + vectorToShelfEndpoint).RotateByBlock(crestedLeader)
                : (crestedLeader.ShelfLedgePointOCS - vectorToShelfEndpoint).RotateByBlock(crestedLeader);
        }
        else
        {
            shelfEndPointOcs = crestedLeader.ShelfPosition == ShelfPosition.Right
                ? crestedLeader.ShelfLedgePointOCS + vectorToShelfEndpoint
                : crestedLeader.ShelfLedgePointOCS - vectorToShelfEndpoint;
        }

        return crestedLeader.InsertionPoint + (shelfEndPointOcs - crestedLeader.InsertionPointOCS);
    }
}