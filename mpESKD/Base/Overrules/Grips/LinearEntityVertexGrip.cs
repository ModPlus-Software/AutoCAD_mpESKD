namespace mpESKD.Base.Overrules.Grips;

using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Abstractions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Enums;
using ModPlusAPI;
using ModPlusAPI.Windows;
using Overrules;
using Utils;

/// <summary>
/// Ручка вершин линейного интеллектуального объекта
/// </summary>
public class LinearEntityVertexGrip : SmartEntityGripData
{
    private readonly List<Point3d> _vertexesPoints;
    private readonly double _minDistance;
    private Point3d? _tmpGripPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearEntityVertexGrip"/> class.
    /// </summary>
    /// <param name="smartEntity">Instance of <see cref="Base.SmartEntity"/> that implement <see cref="ILinearEntity"/></param>
    /// <param name="index">Grip index</param>
    public LinearEntityVertexGrip(SmartEntity smartEntity, int index)
    {
        SmartEntity = smartEntity;
        GripIndex = index;
        GripType = GripType.Point;
        _vertexesPoints = ((ILinearEntity)SmartEntity).GetAllPoints().ToList();
        _minDistance = SmartEntity.MinDistanceBetweenPoints * SmartEntity.GetFullScale();
    }

    /// <summary>
    /// Экземпляр интеллектуального объекта
    /// </summary>
    public SmartEntity SmartEntity { get; }

    /// <summary>
    /// Индекс точки
    /// </summary>
    public int GripIndex { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public Point3d NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp1"); // stretch
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            if (newStatus == Status.GripStart)
            {
                AcadUtils.Editor.TurnForcedPickOn();
                AcadUtils.Editor.PointMonitor += Vertex_EdOnPointMonitor;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса groundLine
            if (newStatus == Status.GripEnd)
            {
                AcadUtils.Editor.TurnForcedPickOff();
                AcadUtils.Editor.PointMonitor -= Vertex_EdOnPointMonitor;

                using (SmartEntity)
                {
                    Point3d? newInsertionPoint = null;
                    var linearEntity = (ILinearEntity)SmartEntity;

                    if (GripIndex == 0)
                    {
                        newInsertionPoint = SmartEntity.InsertionPoint = NewPoint;

                        // Чтобы при совмещении ручки первой вершины с ручкой второй вершины
                        // была создана ручка вершины, а не только "+" и "-"
                        if (_vertexesPoints.Count > 2 && _tmpGripPoint != null)
                        {
                            if (_tmpGripPoint.Value.Equals(_vertexesPoints[1]))
                            {
                                linearEntity.MiddlePoints = linearEntity.MiddlePoints.Skip(1).ToList();
                            }
                        }
                    }
                    else if (GripIndex == _vertexesPoints.Count - 1)
                    {
                        SmartEntity.EndPoint = NewPoint;

                        // Чтобы при совмещении ручки последней вершины с ручкой предпоследней вершины
                        // была создана ручка вершины, а не только "+" и "-"
                        if (_vertexesPoints.Count > 2 && _tmpGripPoint != null)
                        {
                            if (_tmpGripPoint.Value.Equals(_vertexesPoints[GripIndex - 1]))
                            {
                                linearEntity.MiddlePoints = linearEntity.MiddlePoints.Take(GripIndex - 2).ToList();
                            }
                        }
                    }
                    else
                    {
                        linearEntity.MiddlePoints[GripIndex - 1] = NewPoint;

                        // Чтобы при совмещении ручки вершины с ручкой предыдущей/следующей вершины
                        // была создана ручка вершины, а не только "+" и "-"
                        if (_vertexesPoints.Count > 2 && _tmpGripPoint != null)
                        {
                            if (_tmpGripPoint.Value.Equals(_vertexesPoints[GripIndex - 1])
                                || _tmpGripPoint.Value.Equals(_vertexesPoints[GripIndex + 1]))
                            {
                                linearEntity.MiddlePoints = linearEntity.MiddlePoints
                                    .Where((_, index) => index != GripIndex - 1).ToList();
                            }
                        }
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
                AcadUtils.Editor.PointMonitor -= Vertex_EdOnPointMonitor;
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    private void Vertex_EdOnPointMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs)
    {
        try
        {
            var newPoint = pointMonitorEventArgs.Context.ComputedPoint;

            Point3d? gripRightPoint = null;
            Point3d? gripLeftPoint = null;

            if (GripIndex == 0)
            {
                gripRightPoint =  _vertexesPoints[GripIndex + 1];
            }
            else if (GripIndex == _vertexesPoints.Count - 1)
            {
                gripLeftPoint = _vertexesPoints[GripIndex - 1];
            }
            else
            {
                gripLeftPoint = _vertexesPoints[GripIndex - 1];
                gripRightPoint = _vertexesPoints[GripIndex + 1];
            }

            if (gripLeftPoint != null)
            {
                if (!gripLeftPoint.Equals(newPoint) && gripLeftPoint.Value.DistanceTo(newPoint) < _minDistance)
                {
                    newPoint = GeometryUtils.Point3dAtDirection(gripLeftPoint.Value, newPoint, _minDistance);
                }

                var leftLine = new Line(newPoint, gripLeftPoint.Value)
                {
                    ColorIndex = 150
                };
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(leftLine);
            }

            if (gripRightPoint != null)
            {
                if (!gripRightPoint.Equals(newPoint) && gripRightPoint.Value.DistanceTo(newPoint) < _minDistance)
                {
                    newPoint = GeometryUtils.Point3dAtDirection(gripRightPoint.Value, newPoint, _minDistance);
                }

                var rightLine = new Line(newPoint, gripRightPoint.Value)
                {
                    ColorIndex = 150
                };
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(rightLine);
            }

            _tmpGripPoint = newPoint;
        }
        catch
        {
            // ignored
        }
    }
}