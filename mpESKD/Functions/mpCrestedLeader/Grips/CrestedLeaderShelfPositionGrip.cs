#pragma warning disable SA1000
namespace mpESKD.Functions.mpCrestedLeader.Grips;

using System.Linq;
using Autodesk.AutoCAD.Geometry;
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
            // Сохранить начала выносок
            List<Point3d> leaderStartPointsTmp = new ();
            leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

            // Сохранить концы выносок
            List<Point3d> leaderEndPointsTmp = new ();
            leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

            Point3d shelfStartPoint;

            if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
            {
                CrestedLeader.ShelfPosition = ShelfPosition.Left;

                CrestedLeader.InsertionPoint = CrestedLeader.ScaleFactorX == -1
                    ? CrestedLeader.LeaderStartPointsSorted.Last()
                    : CrestedLeader.LeaderStartPointsSorted.First();

                shelfStartPoint = CrestedLeader.InsertionPoint;
            }
            else
            {
                CrestedLeader.ShelfPosition = ShelfPosition.Right;

                CrestedLeader.InsertionPoint = CrestedLeader.ScaleFactorX == -1
                    ? CrestedLeader.LeaderStartPointsSorted.First()
                    : CrestedLeader.LeaderStartPointsSorted.Last();

                 shelfStartPoint = CrestedLeader.InsertionPoint;
            }

            var index = CrestedLeader.LeaderStartPoints.IndexOf(CrestedLeader.InsertionPoint);
            CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(index);
            var boundEndPointTmp = CrestedLeader.BaseLeaderEndPoint;

            CrestedLeader.IsStartPointsAssigned = true;
            CrestedLeader.IsShelfPositionByGrip = true;

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

            CrestedLeader.BaseLeaderEndPoint = boundEndPointTmp;

            CrestedLeader.IsStartPointsAssigned = true;
            CrestedLeader.IsShelfPositionByGrip = true;

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