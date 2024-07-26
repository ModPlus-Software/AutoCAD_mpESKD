namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Ручка перетаскивания выносок
/// </summary>
public class CrestedLeaderMoveLeaderGrip : SmartEntityGripData
{
    private readonly Point3d _startLeaderPoint;
    private readonly Point3d _endLeaderPoint;
    private readonly Point3d _insertionPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderMoveLeaderGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderMoveLeaderGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;

        _startLeaderPoint = CrestedLeader.LeaderStartPoints[gripIndex];
        _endLeaderPoint = CrestedLeader.LeaderEndPoints[gripIndex];
        _insertionPoint = CrestedLeader.InsertionPoint;
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
            AcadUtils.Editor.TurnForcedPickOn();
            AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
        }

        if (newStatus == Status.GripEnd)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;

            using (CrestedLeader)
            {
                var moveDistance = CrestedLeader.LeaderEndPoints[GripIndex].X - NewPoint.X;
                var leaderStartPointPrev = CrestedLeader.LeaderStartPoints[GripIndex];

                CrestedLeader.LeaderStartPoints[GripIndex] = new Point3d(
                    leaderStartPointPrev.X + moveDistance,
                    leaderStartPointPrev.Y,
                    leaderStartPointPrev.Z);

                CrestedLeader.LeaderEndPoints[GripIndex] = NewPoint;
                

                #region Переместить точку вставки
                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                // Сохранить начала выносок
                List<Point3d> leaderStartPointsTmp = new();
                leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                // Сохранить концы выносок
                List<Point3d> leaderEndPointsTmp = new();
                leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);
                
                int searchIndex;
                if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
                {
                    var leaderStartPointRight = leaderStartPointsSort.Last();
                    CrestedLeader.InsertionPoint = leaderStartPointRight;

                    searchIndex = CrestedLeader.LeaderStartPoints.FindIndex(p => p.Equals(leaderStartPointRight));
                }
                else
                {
                    var leaderStartPointLeft = leaderStartPointsSort.First();
                    CrestedLeader.InsertionPoint = leaderStartPointLeft;

                    // найдем соответствующую точку на конце выноски, начало которой в leaderStartPointRight
              
                    searchIndex = CrestedLeader.LeaderStartPoints.FindIndex(p => p.Equals(leaderStartPointLeft));
                }

                // Сохранить конец выноски, начало которой совпадает с точкой вставки
                var boundEndPointTmp = CrestedLeader.LeaderEndPoints[searchIndex];


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

                #endregion

                #region Вернуть в блок точки начал и концов выносок и BoundEndPoint
                CrestedLeader.LeaderStartPoints.Clear();
                CrestedLeader.LeaderStartPoints.AddRange(leaderStartPointsTmp);

                CrestedLeader.LeaderEndPoints.Clear();
                CrestedLeader.LeaderEndPoints.AddRange(leaderEndPointsTmp);

                CrestedLeader.BoundEndPoint = boundEndPointTmp;
                #endregion

                

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
            Line line;

            var cursorPoint = pointMonitorEventArgs.Context.ComputedPoint;
            /*
            var pointStart = new Point3d(
                _startLeaderPoint.X + (cursorPoint.X - _endLeaderPoint.X),
                _startLeaderPoint.Y,
                _startLeaderPoint.Z
            );
            */

            var vectorLeader = _endLeaderPoint.ToPoint2d() - _startLeaderPoint.ToPoint2d();

            var pointStart = Intersections.GetIntersectionBetweenVectors(
                cursorPoint, vectorLeader, _insertionPoint, Vector2d.XAxis);

            if (pointStart == null)
                return;

            line = new Line(pointStart.Value, cursorPoint)
            {
                ColorIndex = 150,
            };

            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(line);
        }
        catch
        {
            // ignored
        }
    }
}