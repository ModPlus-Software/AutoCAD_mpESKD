using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderLeaderRemoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderLeaderRemoveGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderLeaderRemoveGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp6"); // Удалить выноску
    }

    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (CrestedLeader)
        {
            if (CrestedLeader.LeaderStartPoints.Count > 1)
            {
                var checkPoint = CrestedLeader.LeaderStartPoints[GripIndex];

                CrestedLeader.LeaderStartPoints.RemoveAt(GripIndex);
                CrestedLeader.LeaderEndPoints.RemoveAt(GripIndex);

                if (checkPoint.Equals(CrestedLeader.InsertionPoint))
                {
                    var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;

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
                        CrestedLeader.InsertionPoint = leaderStartPointsSort.Last();

                        shelfStartPoint = CrestedLeader.InsertionPoint;
                        shelfLedgePoint = shelfStartPoint + (Vector3d.XAxis * distanceStartToLedge);
                        shelfEndPoint = shelfLedgePoint + (Vector3d.XAxis * distanceLedgeToEnd);
                    }
                    else
                    {
                        CrestedLeader.InsertionPoint = leaderStartPointsSort.First();

                        shelfStartPoint = CrestedLeader.InsertionPoint;
                        shelfLedgePoint = shelfStartPoint - (Vector3d.XAxis * distanceStartToLedge);
                        shelfEndPoint = shelfLedgePoint - (Vector3d.XAxis * distanceLedgeToEnd);
                    }

                    var index = CrestedLeader.LeaderStartPoints.IndexOf(CrestedLeader.InsertionPoint);
                    CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(index);
                    var boundEndPointTmp = CrestedLeader.BaseLeaderEndPoint;

                    CrestedLeader.IsStartPointsAssigned = true;
                    CrestedLeader.IsMoveGripPointsAt = true;

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

                    CrestedLeader.BaseLeaderEndPoint = boundEndPointTmp;

                    CrestedLeader.IsStartPointsAssigned = true;
                    CrestedLeader.IsMoveGripPointsAt = true;
                }

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
        }

        return ReturnValue.GetNewGripPoints;
    }
}