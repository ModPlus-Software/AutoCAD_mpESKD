namespace mpESKD.Base.Overrules.Grips;

using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base;
using Enums;
using ModPlusAPI;
using Overrules;
using Utils;

/// <summary>
/// Ручка добавления вершины линейного интеллектуального объекта
/// </summary>
public class LinearEntityAddVertexGrip : SmartEntityGripData
{
    private readonly double _minDistance;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearEntityAddVertexGrip"/> class.
    /// </summary>
    /// <param name="smartEntity">Instance of <see cref="Base.SmartEntity"/> that implement <see cref="ILinearEntity"/></param>
    /// <param name="leftPoint">Точка слева</param>
    /// <param name="rightPoint">Точка справа</param>
    public LinearEntityAddVertexGrip(SmartEntity smartEntity, Point3d? leftPoint, Point3d? rightPoint)
    {
        SmartEntity = smartEntity;
        GripLeftPoint = leftPoint;
        GripRightPoint = rightPoint;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        _minDistance = smartEntity.MinDistanceBetweenPoints * smartEntity.GetFullScale();
    }

    /// <summary>
    /// Экземпляр интеллектуального объекта
    /// </summary>
    public SmartEntity SmartEntity { get; }

    /// <summary>
    /// Левая точка
    /// </summary>
    public Point3d? GripLeftPoint { get; }

    /// <summary>
    /// Правая точка
    /// </summary>
    public Point3d? GripRightPoint { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public Point3d NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp4"); // "Добавить вершину";
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

            // Рисуется реальная линия
            using (SmartEntity)
            {
                Point3d? newInsertionPoint = null;

                var linearEntity = (ILinearEntity)SmartEntity;

                if (GripLeftPoint == SmartEntity.InsertionPoint)
                {
                    linearEntity.MiddlePoints.Insert(0, NewPoint);
                }
                else if (GripLeftPoint == null)
                {
                    linearEntity.MiddlePoints.Insert(0, SmartEntity.InsertionPoint);
                    SmartEntity.InsertionPoint = NewPoint;
                    newInsertionPoint = NewPoint;
                }
                else if (GripRightPoint == null)
                {
                    linearEntity.MiddlePoints.Add(SmartEntity.EndPoint);
                    SmartEntity.EndPoint = NewPoint;
                }
                else
                {
                    linearEntity.MiddlePoints.Insert(
                        linearEntity.MiddlePoints.IndexOf(GripLeftPoint.Value) + 1, NewPoint);
                }

                SmartEntity.UpdateEntities();
                SmartEntity.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(SmartEntity.BlockId, OpenMode.ForWrite, true, true);
                    if (newInsertionPoint.HasValue)
                    {
                        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                    }

                    using (var resBuf = SmartEntity.GetDataForXData())
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
            var newPoint = pointMonitorEventArgs.Context.ComputedPoint;

            if (GripLeftPoint.HasValue && GripLeftPoint.Value.DistanceTo(newPoint) < _minDistance)
            {
                newPoint = GeometryUtils.Point3dAtDirection(GripLeftPoint.Value, newPoint, _minDistance);
            }

            if (GripRightPoint.HasValue && GripRightPoint.Value.DistanceTo(newPoint) < _minDistance)
            {
                newPoint = GeometryUtils.Point3dAtDirection(GripRightPoint.Value, newPoint, _minDistance);
            }

            if (GripLeftPoint.HasValue)
            {
                var leftLine = new Line(GripLeftPoint.Value, newPoint)
                {
                    ColorIndex = 150
                };
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(leftLine);
            }

            if (GripRightPoint.HasValue)
            {
                var rightLine = new Line(newPoint, GripRightPoint.Value)
                {
                    ColorIndex = 150
                };
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(rightLine);
            }
        }
        catch
        {
            // ignored
        }
    }
}