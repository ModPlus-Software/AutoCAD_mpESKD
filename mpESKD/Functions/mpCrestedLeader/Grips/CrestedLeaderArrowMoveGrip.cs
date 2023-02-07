namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderArrowMoveGrip : SmartEntityGripData
{
    // Экземпляр анонимного блока
    private readonly BlockReference _entity;
    private Point3d _startGripTmp;
    private Point3d _endGripTmp;
    private Point3d _leaderGripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderArrowMoveGrip(CrestedLeader crestedLeader, int gripIndex, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <summary>
    /// Свойство для определения точки в существующем сегменте
    /// </summary>
    public bool IsOnsegment { get; set; }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            _startGripTmp = CrestedLeader.InsertionPoint;
            _endGripTmp = CrestedLeader.EndPoint;
            _leaderGripTmp = CrestedLeader.LeaderPoint;
        }

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                    var tmpInsPoint = CrestedLeader.InsertionPoint;
                    var tmpEndPoint = CrestedLeader.EndPoint;

                    var secondLeaderLine = new Line(CrestedLeader.InsertionPoint, CrestedLeader.EndPoint);
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

                    if (!isOnSegment)
                    {
                        if (distToInsPoint < distToEndPoint)
                        {
                            CrestedLeader.InsertionPoint = pointOnPolyline;
                            
                            CrestedLeader.ArrowPoints.RemoveAt(GripIndex);
                            CrestedLeader.ArrowPoints.Insert(0, CrestedLeader.TempNewArrowPoint);
                        }
                        else
                        {
                            CrestedLeader.EndPoint = pointOnPolyline;

                            CrestedLeader.ArrowPoints.RemoveAt(GripIndex);
                            CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        }
                    }
                    else
                    {
                        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                        var points = CrestedLeader.ArrowPoints;

                        var sortPoint = CrestedLeader.InsertionPoint - ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * 100000);

                        points.Sort((p1, p2) => GetPointOnPolyline(p1, secondLeaderLine, mainNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, secondLeaderLine, mainNormal).DistanceTo(sortPoint)));
                        CrestedLeader.ArrowPoints = points;
                    }
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
            CrestedLeader.TempNewArrowPoint = default;
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