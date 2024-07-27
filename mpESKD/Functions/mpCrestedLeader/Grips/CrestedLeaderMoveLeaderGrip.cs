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

            var tempLineStartPoint = Intersections.GetIntersectionBetweenVectors(
                cursorPoint, vectorLeader, _insertionPoint, Vector2d.XAxis);

            if (tempLineStartPoint == null)
                return;

            line = new Line(tempLineStartPoint.Value, cursorPoint)
            {
                ColorIndex = 150,
            };

            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(line);



            /*
            var widthText = _topText.ActualWidth;
            var vectorToShelfEndPoint = Vector3d.XAxis * (widthText + (CrestedLeader.TextIndent * CrestedLeader.GetScale()));

            var textRegionCenterPoint = GeometryUtils.GetMiddlePoint3d(CrestedLeader.ShelfLedgePointOCS, CrestedLeader.ShelfEndPointOCS);

            if (_topText != null)
            {
                var yVectorToCenterTopText = Vector3d.YAxis * ((CrestedLeader.TextVerticalOffset * CrestedLeader.GetScale()) + 
                                                               (_topText.ActualHeight / 2));

                _topText.Location = textRegionCenterPoint + yVectorToCenterTopText;
            }*/

            var topText = new MText()
            {
                Contents = CrestedLeader.TopText,
                Attachment = AttachmentPoint.MiddleCenter,
            };

            topText.SetProperties(CrestedLeader.TextStyle, CrestedLeader.TopTextHeight * CrestedLeader.GetScale());

            topText.Location = Point3d.Origin;

            Loggerq.WriteRecord($"_topText.Text: {topText.Text}, Text location: {topText.Location.ToString()}");

            var distanceMove  = tempLineStartPoint.Value.X - _startLeaderPoint.X;

            var widthText = topText.ActualWidth + (CrestedLeader.TextIndent * CrestedLeader.GetScale());

            var centerTextPoint = CrestedLeader.ShelfLedgePoint + (Vector3d.XAxis * ((widthText / 2) + distanceMove));

            var centerTopTextPoint = centerTextPoint + 
                                     (Vector3d.YAxis * ((CrestedLeader.TextVerticalOffset * CrestedLeader.GetScale()) + (topText.ActualHeight / 2)));

            var framePoints = topText.GetTextBoundsPoints(0,  centerTopTextPoint);

            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Line(framePoints[0].ToPoint3d(), framePoints[1].ToPoint3d()));
            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Line(framePoints[1].ToPoint3d(), framePoints[2].ToPoint3d()));
            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Line(framePoints[2].ToPoint3d(), framePoints[3].ToPoint3d()));
            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Line(framePoints[3].ToPoint3d(), framePoints[0].ToPoint3d()));

            //var framePointsList = framePoints.ToArray().ToList();
            //framePointsList.Add(framePointsList[0]);
            //var framePoints3d = framePointsList.Select(p => p.ToPoint3d()).ToList();

            //for (int i = 0; i < framePoints3d.Count - 1; i++)
            //{
            //    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Line(framePoints3d[i], framePoints3d[i + 1]));
            //}
        }
        catch
        {
            // ignored
        }
    }

    
}