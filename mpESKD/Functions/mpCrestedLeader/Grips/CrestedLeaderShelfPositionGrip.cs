using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;

/// <summary>
/// Ручка гребенчатой выноски, меняющая положение полки
/// </summary>
public class CrestedLeaderShelfPositionGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderShelfPositionGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="mpCrestedLeader.CrestedLeader"/></param>
    public CrestedLeaderShelfPositionGrip(CrestedLeader crestedLeader)
    {
        CrestedLeader = crestedLeader;
        GripType = GripType.TwoArrowsLeftRight;
    }

    /// <summary>
    /// Экземпляр <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // "Положение полки";
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (CrestedLeader)
        {
            var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

            // Сохранить начала выносок
            List<Point3d> leaderStartPointsTmp = new();
            leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

            // Сохранить концы выносок
            List<Point3d> leaderEndPointsTmp = new();
            leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

            Point3d shelfStartPoint;
            Point3d shelfLedgePoint;
            Point3d shelfEndPoint;

            var distanceStartToLedge = Math.Abs(CrestedLeader.ShelfStartPoint.X - CrestedLeader.ShelfLedgePoint.X);
            var distanceLedgeToEnd = Math.Abs(CrestedLeader.ShelfLedgePoint.X - CrestedLeader.ShelfEndPoint.X);

            if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
            {
                CrestedLeader.ShelfPosition = ShelfPosition.Left;
                CrestedLeader.InsertionPoint = leaderStartPointsSort.First();

                shelfStartPoint = CrestedLeader.InsertionPoint;
                shelfLedgePoint = shelfStartPoint - (Vector3d.XAxis * distanceStartToLedge);
                shelfEndPoint = shelfLedgePoint - (Vector3d.XAxis * distanceLedgeToEnd);
            }
            else
            {
                CrestedLeader.ShelfPosition = ShelfPosition.Right;
                CrestedLeader.InsertionPoint = leaderStartPointsSort.Last();

                shelfStartPoint = CrestedLeader.InsertionPoint;
                shelfLedgePoint = shelfStartPoint + (Vector3d.XAxis * distanceStartToLedge);
                shelfEndPoint = shelfLedgePoint + (Vector3d.XAxis * distanceLedgeToEnd);
            }

            var index = CrestedLeader.LeaderStartPoints.IndexOf(CrestedLeader.InsertionPoint);
            CrestedLeader.BoundEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(index);
            var boundEndPointTmp = CrestedLeader.BoundEndPoint;

            CrestedLeader.IsFirst = true;
            CrestedLeader.IsLeaderPointMovedByOverrule = true;

            CrestedLeader.UpdateEntities();
            CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                // перемещение точки вставки в точку первой точки полки
                ((BlockReference)blkRef).Position = CrestedLeader.InsertionPoint;

                using (var resBuf = CrestedLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }

            CrestedLeader.LeaderStartPoints.Clear();
            CrestedLeader.LeaderStartPoints.AddRange(leaderStartPointsTmp);

            CrestedLeader.LeaderEndPoints.Clear();
            CrestedLeader.LeaderEndPoints.AddRange(leaderEndPointsTmp);

            CrestedLeader.ShelfStartPoint = shelfStartPoint;
            CrestedLeader.ShelfLedgePoint = shelfLedgePoint;
            CrestedLeader.ShelfEndPoint = shelfEndPoint;

            CrestedLeader.BoundEndPoint = boundEndPointTmp;

            CrestedLeader.IsFirst = true;
            CrestedLeader.IsLeaderPointMovedByOverrule = true;

            CrestedLeader.UpdateEntities();
            CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = CrestedLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}