using System;
using mpESKD.Functions.mpChainLeader.Grips;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderVertexGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmpIns;
    private Point3d _gripTmpEnd;

    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderVertexGrip(CrestedLeader crestedLeader, int gripIndex, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Stretch;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    public double NewPoint { get; set; }

    public Point3d NewInsPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            if (newStatus == Status.GripStart)
            {

                _gripTmpIns = CrestedLeader.InsertionPoint;
                _gripTmpEnd = CrestedLeader.EndPoint;

            }

            if (newStatus == Status.GripEnd)
            {
                var tempInsPoint = CrestedLeader.InsertionPoint;
                using (CrestedLeader)
                {
                    if (GripIndex == 0)
                    {
                        AcadUtils.WriteMessageInDebug($"CrestedLeader.TempNewArrowPoint {GripPoint}");

                        var tmpInsPoint = CrestedLeader.InsertionPoint;
                        var tmpEndPoint = new Point3d(CrestedLeader.EndPoint.X, CrestedLeader.InsertionPoint.Y, 0);
                        var secondLeaderLine = new Line(new Point3d(CrestedLeader.InsertionPoint.X, CrestedLeader.InsertionPoint.Y + NewPoint, 0),
                            new Point3d(tmpEndPoint.X, tmpEndPoint.Y + NewPoint, 0));
                        var mainNormal = (CrestedLeader.FirstArrowSecondPoint - CrestedLeader.FirstArrowFirstPoint).GetNormal();

                        var firstPoint = CrestedLeader.ArrowPoints[0];
                        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, secondLeaderLine, mainNormal);
                        CrestedLeader.EndPoint = GetPointOnPolyline(lastPoint, secondLeaderLine, mainNormal);

                        AcadUtils.WriteMessageInDebug($"vertexGrip.GripPoint {GripPoint} + offset {NewPoint};");
                        
                    }

                    if (GripIndex == 1)
                    {
                        AcadUtils.WriteMessageInDebug($"CrestedLeader.TempNewArrowPoint {GripPoint}");

                        CrestedLeader.InsertionPoint = NewInsPoint;
                    }
                    
                }
                CrestedLeader.TempNewStretchPoint = new Point3d(double.NaN, double.NaN, double.NaN);
                CrestedLeader.Delta = double.NaN;
                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                    _entity.Position = CrestedLeader.InsertionPoint;
                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;

                    }

                    tr.Commit();
                }

                CrestedLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmpIns != null)
                {
                    if (GripIndex == 0)
                    {
                        CrestedLeader.InsertionPoint = _gripTmpIns;
                    }

                    if (GripIndex == 1)
                    {
                        CrestedLeader.EndPoint = _gripTmpEnd;
                    }
                }
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    private Point3d GetPointOnPolyline(Point3d point, Line line, Vector3d mainNormal)
    {
        var templine = new Line(point, point + mainNormal);
        var pts = new Point3dCollection();

        line.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
        var pointOnPolyline = new Point3d();

        if (pts.Count > 0)
        {
            pointOnPolyline = pts[0];
        }

        return pointOnPolyline;

    }
}