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

        // todo Тест
        /*
        var borderHalfLength = RevisionMark.BorderWidth / 2 * RevisionMark.GetScale();
        var borderHalfHeight = RevisionMark.BorderHeight / 2 * RevisionMark.GetScale();
        var borderHalfLength = 30;
        var borderHalfHeight = 30;




        _points = new[]
        {
            new Point2d(RevisionMark.InsertionPoint.X - borderHalfLength, RevisionMark.InsertionPoint.Y - borderHalfHeight), // слева внизу
            new Point2d(RevisionMark.InsertionPoint.X + borderHalfLength, RevisionMark.InsertionPoint.Y - borderHalfHeight), // справа внизу
            new Point2d(RevisionMark.InsertionPoint.X + borderHalfLength, RevisionMark.InsertionPoint.Y + borderHalfHeight), // справа вверху
            new Point2d(RevisionMark.InsertionPoint.X - borderHalfLength, RevisionMark.InsertionPoint.Y + borderHalfHeight)  // слева вверху
        };
        */

        var insertionPoint = RevisionMark.InsertionPoint.ToPoint2d();

        AcadUtils.WriteMessageInDebug($"Перед прочтением RevisionMark.FrameRevisionTextPoints в _points в конструкторе  RevisionMarkAddLeaderGrip ");
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

        AcadUtils.WriteMessageInDebug($"RevisionMark.FrameRevisionTextPoints прочитан в _points в конструкторе  RevisionMarkAddLeaderGrip :" +
                                      $"points[0]: {_points[0].X}, {_points[0].Y}"+
                                      $"points[1]: {_points[1].X}, {_points[1].Y}"+
                                      $"points[2]: {_points[2].X}, {_points[2].Y}"+
                                      $"points[3]: {_points[3].X}, {_points[3].Y}");

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
                /*
                RevisionMark.RevisionFrameTypes.Add(0);
                */

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

            AcadUtils.WriteMessageInDebug($"");

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