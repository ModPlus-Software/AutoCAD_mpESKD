namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using mpESKD.Functions.mpChainLeader.Grips;
using System;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class CrestedLeaderArrowAddGrip : SmartEntityGripData
{
    private readonly BlockReference _entity;
    private Point3d _startGripTmp;
    private Point3d _endGripTmp;

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
        if (newStatus == Status.GripStart)
        {
            _startGripTmp = CrestedLeader.InsertionPoint;
            _endGripTmp = CrestedLeader.EndPoint;
        }

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var tmpInsPoint = CrestedLeader.InsertionPoint;
                var tmpEndPoint = new Point3d(CrestedLeader.EndPoint.X, CrestedLeader.InsertionPoint.Y,0);

                if (tmpInsPoint == tmpEndPoint)
                {
                    tmpEndPoint = new Point3d(CrestedLeader.EndPoint.X + CrestedLeader.MinDistanceBetweenPoints, CrestedLeader.InsertionPoint.Y,0);
                }

                var secondLeaderLine = new Line(CrestedLeader.InsertionPoint, tmpEndPoint);
                var mainNormal = (CrestedLeader.FirstArrowSecondPoint - CrestedLeader.FirstArrowFirstPoint).GetNormal();

                var templine = new Line(CrestedLeader.TempNewArrowPoint, CrestedLeader.TempNewArrowPoint + mainNormal);
                var pts = new Point3dCollection();

                secondLeaderLine.IntersectWith(templine, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                var pointOnPolyline = new Point3d();

                if (pts.Count > 0)
                {
                    pointOnPolyline = pts[0];
                }

                var isOnSegment = IsPointBetween(pointOnPolyline, tmpInsPoint, tmpEndPoint);
                var distToInsPoint = pointOnPolyline.DistanceTo(tmpInsPoint);
                var distToEndPoint = pointOnPolyline.DistanceTo(tmpEndPoint);
                AcadUtils.WriteMessageInDebug($"pointOnPolyline {pointOnPolyline}");
                if (!isOnSegment)
                {
                    if (distToInsPoint < distToEndPoint)
                    {
                        CrestedLeader.InsertionPoint = new Point3d(pointOnPolyline.X, CrestedLeader.InsertionPoint.Y, 0);
                        CrestedLeader.ArrowPoints.Insert(0, CrestedLeader.TempNewArrowPoint);
                    }
                    else
                    {
                        CrestedLeader.EndPoint = new Point3d(pointOnPolyline.X, CrestedLeader.InsertionPoint.Y, 0);;
                        CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                    }
                }
                else
                {
                    CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);

                    var points = CrestedLeader.ArrowPoints;
                    var sortPoint = CrestedLeader.InsertionPoint - ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * 100000);

                    points.Sort((p1, p2) => GetPointOnPolyline(p1, secondLeaderLine, mainNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, secondLeaderLine, mainNormal).DistanceTo(sortPoint)));
                    CrestedLeader.ArrowPoints = points;
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
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }

    private bool IsPointBetween(Point3d point, Point3d startPt, Point3d endPt)
    {
        var segment = new LineSegment3d(startPt, endPt);
        return segment.IsOn(point);
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
