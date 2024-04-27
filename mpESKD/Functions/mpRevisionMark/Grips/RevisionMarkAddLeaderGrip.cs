namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Linq;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class RevisionMarkAddLeaderGrip : SmartEntityGripData
{
    private Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkAddLeaderGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр <see cref="mpRevisionMark.RevisionMark"/></param>
    public RevisionMarkAddLeaderGrip(RevisionMark revisionMark)
    {
        RevisionMark = revisionMark;
        GripType = GripType.Plus;
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
    /// Экземпляр <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public Point3d NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
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
            using (RevisionMark)
            {
                RevisionMark.LeaderPoints.Add(NewPoint);
                
                // todo
                
                RevisionMark.RevisionFrameTypes.Add(0);

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
        }
        catch
        {
            // ignored
        }
    }
}