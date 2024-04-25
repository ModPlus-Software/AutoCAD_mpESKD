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
/// Ручка вершин
/// </summary>
public class RevisionMarkLeaderMoveGrip : SmartEntityGripData
{
    private readonly Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkLeaderMoveGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;

        // todo RevisionMarkLeaderMoveGrip: Test
        /*
        var borderHalfLength = RevisionMark.BorderWidth / 2 * RevisionMark.GetScale();
        var borderHalfHeight = RevisionMark.BorderHeight / 2 * RevisionMark.GetScale();
        */
        var borderHalfLength = 30;
        var borderHalfHeight = 30;



        _points = new[]
        {
            new Point2d(RevisionMark.InsertionPoint.X - borderHalfLength, RevisionMark.InsertionPoint.Y - borderHalfHeight),
            new Point2d(RevisionMark.InsertionPoint.X + borderHalfLength, RevisionMark.InsertionPoint.Y - borderHalfHeight),
            new Point2d(RevisionMark.InsertionPoint.X + borderHalfLength, RevisionMark.InsertionPoint.Y + borderHalfHeight),
            new Point2d(RevisionMark.InsertionPoint.X - borderHalfLength, RevisionMark.InsertionPoint.Y + borderHalfHeight)
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
                RevisionMark.LeaderPoints[GripIndex] = NewPoint;
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
            var nearestPoint = _points.OrderBy(p => p.GetDistanceTo(pointMonitorEventArgs.Context.ComputedPoint.ToPoint2d())).First();
            var line = new Line(nearestPoint.ToPoint3d(), pointMonitorEventArgs.Context.ComputedPoint)
            {
                ColorIndex = 150
            };
            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(line);
        }
        catch
        {
            // ignored
        }
    }
}