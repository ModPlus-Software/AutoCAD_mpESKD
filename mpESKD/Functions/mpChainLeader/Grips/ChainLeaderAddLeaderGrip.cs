namespace mpESKD.Functions.mpLevelPlanMark.Grips;

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
public class ChainLeaderAddLeaderGrip : SmartEntityGripData
{
    private Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderAddLeaderGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public ChainLeaderAddLeaderGrip(LevelPlanMark levelPlanMark) 
    {
        LevelPlanMark = levelPlanMark;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;

        var borderHalfLength = LevelPlanMark.BorderWidth / 2 * LevelPlanMark.GetScale();
        var borderHalfHeight = LevelPlanMark.BorderHeight / 2 * LevelPlanMark.GetScale();

        _points = new[]
        {
            new Point2d(LevelPlanMark.InsertionPoint.X - borderHalfLength, LevelPlanMark.InsertionPoint.Y - borderHalfHeight),
            new Point2d(LevelPlanMark.InsertionPoint.X + borderHalfLength, LevelPlanMark.InsertionPoint.Y - borderHalfHeight),
            new Point2d(LevelPlanMark.InsertionPoint.X + borderHalfLength, LevelPlanMark.InsertionPoint.Y + borderHalfHeight),
            new Point2d(LevelPlanMark.InsertionPoint.X - borderHalfLength, LevelPlanMark.InsertionPoint.Y + borderHalfHeight)
        };
    }

    /// <summary>
    /// Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

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
            using (LevelPlanMark)
            {
                LevelPlanMark.LeaderPoints.Add(NewPoint);
                LevelPlanMark.LeaderTypes.Add(0);

                LevelPlanMark.UpdateEntities();
                LevelPlanMark.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);
                    
                    using (var resBuf = LevelPlanMark.GetDataForXData())
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