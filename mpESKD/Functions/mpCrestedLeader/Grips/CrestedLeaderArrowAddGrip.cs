using System;
using Autodesk.AutoCAD.Geometry;
using mpESKD.Functions.mpChainLeader.Grips;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class CrestedLeaderArrowAddGrip : SmartEntityGripData
{
    private readonly BlockReference _entity;
    private Point3d _startGripTmp;
    private Point3d _endGripTmp;
    private Point3d _leaderGripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderArrowAddGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderArrowAddGrip(CrestedLeader crestedLeader, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
        return Language.GetItem("gp5");
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        AcadUtils.WriteMessageInDebug($"gripstatus {newStatus}");
        if (newStatus == Status.GripStart)
        {
            _startGripTmp = CrestedLeader.InsertionPoint;
            _endGripTmp = CrestedLeader.EndPoint;
            _leaderGripTmp = CrestedLeader.LeaderPoint;
        }

        if (newStatus == Status.Stretch)
        {
            CrestedLeader.InsertionPoint = _startGripTmp;
            CrestedLeader.EndPoint = _endGripTmp;
            CrestedLeader.LeaderPoint = _leaderGripTmp;
        }

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var tmpEndPoint = CrestedLeader.EndPoint;
                var tmpLeaderPoint = CrestedLeader.LeaderPoint;

                var tempLine = new Line(CrestedLeader.EndPoint, CrestedLeader.LeaderPoint);
                var mainNormal = (CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal();
                var pointOnPolyline = CreateLeadersWithArrows(tempLine, Intersect.ExtendBoth, CrestedLeader.TempNewArrowPoint, mainNormal);

                var isOnSegment = IsPointBetween(pointOnPolyline, tmpEndPoint, tmpLeaderPoint);

                if (!isOnSegment)
                {
                    var distToEndPoint = pointOnPolyline.DistanceTo(tmpEndPoint);
                    var distToLeaderPoint = pointOnPolyline.DistanceTo(tmpLeaderPoint);
                    if (distToLeaderPoint < distToEndPoint)
                    {
                        CrestedLeader.LeaderPoint = pointOnPolyline;
                        //CrestedLeader.EndPoint = tmpEndPoint;
                        CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        //CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;

                        AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем LeaderPoint");
                    }
                    else
                    {
                        CrestedLeader.EndPoint = pointOnPolyline;
                        CrestedLeader.LeaderPoint = tmpLeaderPoint;
                        CrestedLeader.ArrowPoints.Add(CrestedLeader.InsertionPoint);
                        CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;
                        
                        
                        AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем EndPoint");
                    }
                }
                else 
                {
                    CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                }
                
                CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);

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
            }
        }

        if (newStatus == Status.GripAbort)
        {
            CrestedLeader.InsertionPoint = _startGripTmp;
            CrestedLeader.EndPoint = _endGripTmp;
            CrestedLeader.LeaderPoint = _leaderGripTmp;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }

    private Point3d CreateLeadersWithArrows(Line secondLeaderLine, Intersect intersectType, Point3d arrowPoint, Vector3d mainNormal)
    {
        var templine = new Line(arrowPoint, arrowPoint + mainNormal);
        var pts = new Point3dCollection();

        secondLeaderLine.IntersectWith(templine, intersectType, pts, IntPtr.Zero, IntPtr.Zero);

        try
        {
            if (pts.Count > 0)
                return pts[0];
        }
        catch (Exception e)
        {
            AcadUtils.WriteMessageInDebug("ошибка построения линии");

        }

        return default;
    }

    private bool IsPointBetween(Point3d point, Point3d startPt, Point3d endPt)
    {
        var segment = new LineSegment3d(startPt, endPt);
        return segment.IsOn(point);
    }
}
