using System.Collections.Generic;
using System.Security.Policy;

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
    private Vector3d _leaderNormal;
    private Line _mainLine;
    private List<Point3d> _points;

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
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                    var tmpEndPointForNormal = new Point3d(CrestedLeader.EndPoint.X, CrestedLeader.InsertionPoint.Y, 0);
                    var mainNormal = (tmpEndPointForNormal - CrestedLeader.InsertionPoint).GetNormal();
                    var endPoint = CrestedLeader.InsertionPoint + (Math.Abs(CrestedLeader.EndPoint.X - CrestedLeader.InsertionPoint.X) * mainNormal);
                    
                    if (CrestedLeader.InsertionPoint == endPoint)
                    {
                        endPoint = CrestedLeader.InsertionPoint + CrestedLeader.MinDistanceBetweenPoints * mainNormal;
                    }

                    _mainLine = new Line(CrestedLeader.InsertionPoint, endPoint);
                    _leaderNormal = (CrestedLeader.FirstArrowSecondPoint - CrestedLeader.FirstArrowFirstPoint).GetNormal();

                    var sortPoint = CrestedLeader.InsertionPoint - (mainNormal * 100000);
                    _points = CrestedLeader.ArrowPoints;
                    CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    if (CrestedLeader.ArrowPoints.Count == 1)
                    {
                        CrestedLeader.InsertionPoint = GetPointOnPolyline(CrestedLeader.TempNewArrowPoint, _mainLine, _leaderNormal);
                    }

                    //if (GripIndex == 0)
                    //{
                        

                       
                    //    else
                    //    {
                    //        //SetInsEndPoint(sortPoint, endPoint);
                    //        _points.Sort((p1, p2) => GetPointOnPolyline(p1, _mainLine, _leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, _mainLine, _leaderNormal).DistanceTo(sortPoint)));
                    //        CrestedLeader.ArrowPoints = _points;

                    //        var firstPoint = CrestedLeader.ArrowPoints[0];
                    //        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                    //        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, _mainLine, _leaderNormal);
                    //        var tempNewEndPoint = GetPointOnPolyline(lastPoint, _mainLine, _leaderNormal);

                    //        if (!CrestedLeader.IsRight)
                    //        {
                    //            CrestedLeader.EndPoint = tempNewEndPoint.X < endPoint.X ? tempNewEndPoint : endPoint;
                    //        }
                    //        else
                    //        {
                    //            CrestedLeader.EndPoint = tempNewEndPoint.X > endPoint.X ? tempNewEndPoint : endPoint;
                    //        }
                    //    }
                    //}
                    //else if (GripIndex == CrestedLeader.ArrowPoints.Count - 1)
                    //{
                    //    //SetInsEndPoint(sortPoint, tmpEndPoint);
                    //    _points.Sort((p1, p2) => GetPointOnPolyline(p1, _mainLine, _leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, _mainLine, _leaderNormal).DistanceTo(sortPoint)));
                    //    CrestedLeader.ArrowPoints = _points;

                    //    var firstPoint = CrestedLeader.ArrowPoints[0];
                    //    var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                    //    CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, _mainLine, _leaderNormal);
                    //    var tempNewEndPoint = GetPointOnPolyline(lastPoint, _mainLine, _leaderNormal);

                    //    if (!CrestedLeader.IsRight)
                    //    {
                    //        CrestedLeader.EndPoint = tempNewEndPoint.X < endPoint.X ? tempNewEndPoint : endPoint;
                    //    }
                    //    else
                    //    {
                    //        CrestedLeader.EndPoint = tempNewEndPoint.X > endPoint.X ? tempNewEndPoint : endPoint;
                    //    }

                    //}
                    else
                    {
                        //SetInsEndPoint(sortPoint, endPoint);
                        _points.Sort((p1, p2) => GetPointOnPolyline(p1, _mainLine, _leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, _mainLine, _leaderNormal).DistanceTo(sortPoint)));
                        CrestedLeader.ArrowPoints = _points;

                        var firstPoint = CrestedLeader.ArrowPoints[0];
                        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, _mainLine, _leaderNormal);
                        var tempNewEndPoint = GetPointOnPolyline(lastPoint, _mainLine, _leaderNormal);

                        if (!CrestedLeader.IsRight)
                        {
                            CrestedLeader.EndPoint = tempNewEndPoint.X < endPoint.X ? tempNewEndPoint : endPoint;
                        }
                        else
                        {
                            CrestedLeader.EndPoint = tempNewEndPoint.X > endPoint.X ? tempNewEndPoint : endPoint;
                        }
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
            CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);
            CrestedLeader.InsertionPoint = _startGripTmp;
            CrestedLeader.EndPoint = _endGripTmp;
        }

        base.OnGripStatusChanged(entityId, newStatus);
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

    private void SetInsEndPoint(Point3d sortPoint, Point3d endPoint)
    {
        _points.Sort((p1, p2) => GetPointOnPolyline(p1, _mainLine, _leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, _mainLine, _leaderNormal).DistanceTo(sortPoint)));
        CrestedLeader.ArrowPoints = _points;

        var firstPoint = CrestedLeader.ArrowPoints[0];
        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, _mainLine, _leaderNormal);
        var tempNewEndPoint = GetPointOnPolyline(lastPoint, _mainLine, _leaderNormal);

        if (!CrestedLeader.IsRight)
        {
            CrestedLeader.EndPoint = tempNewEndPoint.X < endPoint.X ? tempNewEndPoint : endPoint;
        }
        else
        {
            CrestedLeader.EndPoint = tempNewEndPoint.X > endPoint.X ? tempNewEndPoint : endPoint;
        }
    }
}