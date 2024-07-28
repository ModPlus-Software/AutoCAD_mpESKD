using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Windows.Data;

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

    /*
    private readonly string _topText;
    private readonly string _bottomText;
    private readonly double _topTextHeight;
    private readonly double _bottomTextHeight;
    private readonly string _textStyle;*/

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
        /*
       _topText = CrestedLeader.TopText;
       _topTextHeight = CrestedLeader.TopTextHeight;
       _textStyle = CrestedLeader.TextStyle;*/
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
            //AcadUtils.Editor.TurnForcedPickOn();
            //AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
        }

        if (newStatus == Status.GripEnd)
        {
            //AcadUtils.Editor.TurnForcedPickOff();
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;

            using (CrestedLeader)
            {
                /*
                var moveDistance = CrestedLeader.LeaderEndPoints[GripIndex].X - NewPoint.X;
                var leaderStartPointPrev = CrestedLeader.LeaderStartPoints[GripIndex];

                CrestedLeader.LeaderStartPoints[GripIndex] = new Point3d(
                    leaderStartPointPrev.X + moveDistance,
                    leaderStartPointPrev.Y,
                    leaderStartPointPrev.Z);

                CrestedLeader.LeaderEndPoints[GripIndex] = NewPoint;*/
                

                #region Переместить точку вставки
               /* CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();*/

               // var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                // Сохранить начала выносок
                List<Point3d> leaderStartPointsTmp = new();
                leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                // Сохранить концы выносок
                List<Point3d> leaderEndPointsTmp = new();
                leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

                /*
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
                */

                var boundEndPointTmp = CrestedLeader.BoundEndPoint;

                CrestedLeader.InsertionPoint = CrestedLeader.ShelfStartPoint;

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                    // перемещение точки вставки в точку первой точки полки
                    ((BlockReference)blkRef).Position =  CrestedLeader.InsertionPoint;

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
            //AcadUtils.Editor.TurnForcedPickOff();
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}