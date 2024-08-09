namespace mpESKD.Functions.mpCrestedLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка перетаскивания выносок
/// </summary>
public class CrestedLeaderStartPointLeaderGrip : SmartEntityGripData
{
    // Временное значение точек
    private Point3d _leaderStartPointTmp;
    private Point3d _leaderEndPointTmp;
    private Point3d _shelfStartPointTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderStartPointLeaderGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderStartPointLeaderGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Новое значение точки ручки
    /// </summary>
    public Point3d NewPoint { get; set; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            _leaderStartPointTmp = CrestedLeader.LeaderStartPoints[GripIndex];
            _leaderEndPointTmp = CrestedLeader.LeaderEndPoints[GripIndex];
            _shelfStartPointTmp = CrestedLeader.ShelfStartPoint;
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
            if (_leaderStartPointTmp != null && _leaderEndPointTmp != null)
            {
                CrestedLeader.LeaderStartPoints[GripIndex] = _leaderStartPointTmp;
                CrestedLeader.LeaderEndPoints[GripIndex] = _leaderEndPointTmp;
                CrestedLeader.ShelfStartPoint = _shelfStartPointTmp;

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

            }
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}