namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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
    private Point3d? _newLeaderStartPoint;

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
            AcadUtils.Editor.TurnForcedPickOn();
            AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
        }

        if (newStatus == Status.GripEnd)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
            using (CrestedLeader)
            {
                if (_newLeaderStartPoint is { } startPoint)
                {
                    CrestedLeader.LeaderStartPoints.Add(startPoint);
                    CrestedLeader.LeaderEndPoints.Add(NewPoint);
                    /*
                    var vectorToShelfEndPoint = CrestedLeader.ShelfEndPoint - CrestedLeader.ShelfLedgePoint;
                    var vectorToShelfLedgePoint = CrestedLeader.ShelfLedgePoint - CrestedLeader.ShelfStartPoint;*/

                    // var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();


                    /*
                    // Список векторов от начал к концам выносок
                    List<Vector3d> vectorsToEndPoint = CrestedLeader.LeaderStartPoints
                        .Select((t, i) => CrestedLeader.LeaderEndPoints[i] - t).ToList();

                    // Список концов выносок, с учетом нового положения перемещаемой выноски
                    List<Point3d> leaderEndPointsSort = new();
                    for (int i = 0; i < leaderStartPointsSort.Count; i++)
                    {
                        foreach (var endPoint in CrestedLeader.LeaderEndPoints)
                        {
                            var vectorToEndPoint = endPoint - leaderStartPointsSort[i];

                            if (vectorsToEndPoint.Any(v => v.Equals(vectorToEndPoint)))
                            {
                                leaderEndPointsSort.Add(endPoint);
                                break;
                            }
                        }
                    }
                    */

                    /*
                    CrestedLeader.ShelfLedgePoint = CrestedLeader.ShelfStartPoint + vectorToShelfLedgePoint;
                    CrestedLeader.ShelfEndPoint = CrestedLeader.ShelfLedgePoint + vectorToShelfEndPoint;*/

                    //CrestedLeader.IsMoveGripPointsAt = true;

                    var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;

                    // Если требуется переместить точку вставки
                    if (!(CrestedLeader.InsertionPoint.Equals(leaderStartPointsSort.Last()) &&
                          CrestedLeader.ShelfPosition == ShelfPosition.Right)
                        ||
                        (!CrestedLeader.InsertionPoint.Equals(leaderStartPointsSort.First()) &&
                         CrestedLeader.ShelfPosition == ShelfPosition.Left))
                    {
                        CrestedLeader.ShelfStartPoint = CrestedLeader.ShelfPosition == ShelfPosition.Right 
                            ? leaderStartPointsSort.Last() 
                            : leaderStartPointsSort.First();

                        //CrestedLeader.ToLogAnyString("CrestedLeaderAddLeaderGrip: OnGripStatusChanged: Insert point move !");

                        // Сохранить начала выносок
                        List<Point3d> leaderStartPointsTmp = new();
                        leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                        // Сохранить концы выносок
                        List<Point3d> leaderEndPointsTmp = new();
                        leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

                        //var boundEndPointTmp = CrestedLeader.BaseLeaderEndPoint;

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
        }

        if (newStatus == Status.GripAbort)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }

    private void AddNewVertex_EdOnPointMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs)
    {
        try
        {
            var cursorPoint = pointMonitorEventArgs.Context.ComputedPoint;

            var vectorToStartPoint = CrestedLeader.InsertionPoint- CrestedLeader.BaseLeaderEndPoint;

             _newLeaderStartPoint = Intersections.GetIntersectionBetweenVectors(
                cursorPoint,
                vectorToStartPoint.ToVector2d(),
                CrestedLeader.InsertionPoint,
                //Vector2d.XAxis
                CrestedLeader.BaseVectorNormal.ToVector2d()
                );

            if (_newLeaderStartPoint is {} startPoint)
            {
                var lineNewLeader = new Line(cursorPoint, startPoint);

                var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;
                var leaderStartPointsOcsSort = CrestedLeader.LeaderStartPointsOCS.OrderBy(p => p.X).ToList();

                if (startPoint.Point3dToPoint3dOcs(CrestedLeader).X < leaderStartPointsOcsSort.First().X)
                {
                    var addedUnionLine = new Line(leaderStartPointsSort.First(), startPoint);
                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(addedUnionLine);
                }
                else if (startPoint.Point3dToPoint3dOcs(CrestedLeader).X > leaderStartPointsOcsSort.Last().X)
                {
                    var addedUnionLine = new Line(leaderStartPointsSort.Last(), startPoint);
                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(addedUnionLine);
                }

                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(lineNewLeader);
            }
        }
        catch
        {
            // ignored
        }
    }
}