namespace mpESKD.Functions.mpRevisionMark.Grips;

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
/// Ручка вершин
/// </summary>
public class RevisionMarkLeaderMoveGrip : SmartEntityGripData
{
    private readonly Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkLeaderMoveGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkLeaderMoveGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;

        var insertionPoint = RevisionMark.InsertionPoint.ToPoint2d();
        _points = new[]
        {
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[0].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[0].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[1].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[1].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[2].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[2].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[3].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[3].Y),
        };
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <summary>
    /// Новое значение точки вершины
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

            using (RevisionMark)
            {
                var prevLeaderPoint = RevisionMark.LeaderPoints[GripIndex];
                var prevStretchPoint = RevisionMark.RevisionFrameStretchPoints[GripIndex];

                var moveVector = NewPoint - prevLeaderPoint;

                RevisionMark.LeaderPoints[GripIndex] = NewPoint;

                RevisionMark.RevisionFrameStretchPoints[GripIndex]=
                    prevStretchPoint + moveVector;

                RevisionMark.UpdateEntities();
                RevisionMark.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(RevisionMark.BlockId, OpenMode.ForWrite, true, true);

                    using (var resBuf = RevisionMark.GetDataForXData())
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
            if (string.IsNullOrEmpty(RevisionMark.Note))
            {
                var nearestPoint = _points
                    .OrderBy(p => p.GetDistanceTo(pointMonitorEventArgs.Context.ComputedPoint.ToPoint2d())).First();

                line = new Line(nearestPoint.ToPoint3d(), pointMonitorEventArgs.Context.ComputedPoint)
                {
                    ColorIndex = 150
                };
            }
            else
            {
                var startPoint = new Point3d(
                    RevisionMark.InsertionPoint.X + RevisionMark.NoteShelfLinePoints[1].X,
                    RevisionMark.InsertionPoint.Y + RevisionMark.NoteShelfLinePoints[1].Y,
                    RevisionMark.InsertionPoint.Z);

                line = new Line(startPoint, pointMonitorEventArgs.Context.ComputedPoint)
                {
                    ColorIndex = 150
                };
            }

            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(line);

            //
            var prevLeaderPoint = RevisionMark.LeaderPoints[GripIndex];
            var prevStretchPoint = RevisionMark.RevisionFrameStretchPoints[GripIndex];

            var moveVector = pointMonitorEventArgs.Context.ComputedPoint - prevLeaderPoint;

            var stretchPoint = prevStretchPoint + moveVector;

            List<Polyline> revisionFramesAsPolylines = new();
            List<Circle> revisionFramesAsCircles = new();

            var frameType = (RevisionFrameType)Enum.GetValues(typeof(RevisionFrameType))
                .GetValue(RevisionMark.RevisionFrameTypes[GripIndex]);

            RevisionMark.CreateRevisionFrame(
                pointMonitorEventArgs.Context.ComputedPoint,
                pointMonitorEventArgs.Context.ComputedPoint,
                stretchPoint,
                frameType,
                revisionFramesAsPolylines,
                revisionFramesAsCircles,
                RevisionMark.GetFullScale()
            );

            if (revisionFramesAsPolylines.Count > 0)
            {
                var polyline = revisionFramesAsPolylines[0];
                polyline.ColorIndex = 150;
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(polyline);
            }
            else if (revisionFramesAsCircles.Count > 0)
            {
                var circle = revisionFramesAsCircles[0];
                circle.ColorIndex = 150;
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(circle);
            }
        }
        catch
        {
            // ignored
        }
    }
}