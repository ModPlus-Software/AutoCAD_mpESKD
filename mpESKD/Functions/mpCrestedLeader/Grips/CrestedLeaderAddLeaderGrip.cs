#pragma warning disable SA1000
namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка добавления выноски
/// </summary>
public class CrestedLeaderAddLeaderGrip : SmartEntityGripData
{
    private readonly List<Point3d> _leaderStartPointsTmp = new ();
    private readonly List<Point3d> _leaderEndPointsTmp = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderAddLeaderGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="mpCrestedLeader.CrestedLeader"/></param>
    public CrestedLeaderAddLeaderGrip(CrestedLeader crestedLeader)
    {
        CrestedLeader = crestedLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
    }

    /// <summary>
    /// Экземпляр <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Новое значение точки ручки
    /// </summary>
    public Point3d NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp5");
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            CrestedLeader.LeaderStartPoints.ToLog("Status.GripStart: LeaderStartPoints");

            // Сохранение списков точек выносок
            _leaderStartPointsTmp.Clear();
            _leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

            _leaderEndPointsTmp.Clear();
            _leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

            // Добавление точки новой выноски
            CrestedLeader.LeaderStartPoints.Add(GripPoint);
            CrestedLeader.LeaderEndPoints.Add(GripPoint);
        }

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;

                // Если требуется переместить точку вставки
                if (!(CrestedLeader.InsertionPoint.Equals(leaderStartPointsSort.Last()) &&
                      CrestedLeader.ShelfPosition == ShelfPosition.Right)
                    ||
                    (!CrestedLeader.InsertionPoint.Equals(leaderStartPointsSort.First()) &&
                     CrestedLeader.ShelfPosition == ShelfPosition.Left))
                {
                    // Сохранить начала выносок
                    List<Point3d> leaderStartPointsTmp = new();
                    leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                    // Сохранить концы выносок
                    List<Point3d> leaderEndPointsTmp = new();
                    leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

                    CrestedLeader.InsertionPoint = CrestedLeader.ShelfStartPoint;

                    var index = CrestedLeader.LeaderStartPoints.IndexOf(CrestedLeader.InsertionPoint);
                    CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(index);
                    var boundEndPointTmp = CrestedLeader.BaseLeaderEndPoint;

                    CrestedLeader.IsStartPointsAssigned = true;

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

                    CrestedLeader.BaseLeaderEndPoint = boundEndPointTmp;

                    CrestedLeader.IsStartPointsAssigned = true;

                    CrestedLeader.UpdateEntities();
                    CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                }

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

        if (newStatus == Status.GripAbort)
        {
            if (_leaderStartPointsTmp != null && _leaderEndPointsTmp != null)
            {
                CrestedLeader.LeaderStartPoints.Clear();
                CrestedLeader.LeaderStartPoints.AddRange(_leaderStartPointsTmp);

                CrestedLeader.LeaderEndPoints.Clear();
                CrestedLeader.LeaderEndPoints.AddRange(_leaderEndPointsTmp);
            }
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}