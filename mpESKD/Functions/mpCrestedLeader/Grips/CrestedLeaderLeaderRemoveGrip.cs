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
                var removedPoint = CrestedLeader.LeaderStartPoints[GripIndex];

                CrestedLeader.LeaderStartPoints.RemoveAt(GripIndex);
                CrestedLeader.LeaderEndPoints.RemoveAt(GripIndex);

                if (removedPoint.Equals(CrestedLeader.InsertionPoint))
                {
                    // Найти новые InsertionPoint, BaseLeaderEndPoint, ShelfStartPoint
                    // Сохранить точки 



                    //Point3d shelfStartPoint;
                    //Point3d shelfLedgePoint;
                    //Point3d shelfEndPoint;

                    /*
                    var distanceStartToLedge = Math.Abs(CrestedLeader.ShelfStartPoint.X - CrestedLeader.ShelfLedgePoint.X);
                    var distanceLedgeToEnd = Math.Abs(CrestedLeader.ShelfLedgePoint.X - CrestedLeader.ShelfEndPoint.X);*/

                    var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;

                    CrestedLeader.InsertionPoint = CrestedLeader.ShelfPosition == ShelfPosition.Right 
                        ? leaderStartPointsSort.Last() 
                        : leaderStartPointsSort.First();
                    
                    // Сохранить начало полки
                    var shelfStartPoint = CrestedLeader.ShelfStartPoint = CrestedLeader.InsertionPoint;
                    
                    // Сохранить начала выносок
                    List<Point3d> leaderStartPointsTmp = new();
                    leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                    // Сохранить концы выносок
                    List<Point3d> leaderEndPointsTmp = new();
                    leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

                    var index = CrestedLeader.LeaderStartPoints.IndexOf(CrestedLeader.InsertionPoint);
                    CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(index);

                    // Созранить конец стартовой выноски
                    var boundEndPointTmp = CrestedLeader.BaseLeaderEndPoint;

                    CrestedLeader.IsStartPointsAssigned = true;
                    //CrestedLeader.IsMoveGripPointsAt = true;

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

                    CrestedLeader.ShelfStartPoint = shelfStartPoint;

                    CrestedLeader.LeaderStartPoints.Clear();
                    CrestedLeader.LeaderStartPoints.AddRange(leaderStartPointsTmp);

                    CrestedLeader.LeaderEndPoints.Clear();
                    CrestedLeader.LeaderEndPoints.AddRange(leaderEndPointsTmp);

                    //CrestedLeader.ShelfLedgePoint = shelfLedgePoint;
                    //CrestedLeader.ShelfEndPoint = shelfEndPoint;

                    CrestedLeader.BaseLeaderEndPoint = boundEndPointTmp;

                    //CrestedLeader.IsStartPointsAssigned = true;
                    //CrestedLeader.IsMoveGripPointsAt = true;
                }

                CrestedLeader.IsStartPointsAssigned = true;

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