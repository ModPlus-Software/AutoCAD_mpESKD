using DocumentFormat.OpenXml.Presentation;

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
    private double _shelfLength;

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
            _shelfLength = CrestedLeader.ShelfLength;
        }

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                    var tmpInsPoint = CrestedLeader.InsertionPoint;
                    var mainNormal = (CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal();
                    var tmpEndPoint = CrestedLeader.InsertionPoint + (Math.Abs(CrestedLeader.EndPoint.X - CrestedLeader.InsertionPoint.X) * mainNormal);
                    
                    if (tmpInsPoint == tmpEndPoint)
                    {
                        tmpEndPoint = CrestedLeader.InsertionPoint + CrestedLeader.MinDistanceBetweenPoints * mainNormal;
                    }

                    var mainLine = new Line(CrestedLeader.InsertionPoint, tmpEndPoint);
                    var leaderNormal = (CrestedLeader.FirstArrowSecondPoint - CrestedLeader.FirstArrowFirstPoint).GetNormal();

                    var sortPoint = CrestedLeader.InsertionPoint - (mainNormal * 100000);
                    var points = CrestedLeader.ArrowPoints;
                    if (GripIndex == 0)
                    {
                        //CrestedLeader.ArrowPoints.RemoveAt(0);
                        //CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;

                        points.Sort((p1, p2) => GetPointOnPolyline(p1, mainLine, leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, mainLine, leaderNormal).DistanceTo(sortPoint)));
                        CrestedLeader.ArrowPoints = points;

                        var firstPoint = CrestedLeader.ArrowPoints[0];
                        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, mainLine, leaderNormal);
                        CrestedLeader.EndPoint = GetPointOnPolyline(lastPoint, mainLine, leaderNormal);
                    }
                    else if (GripIndex == CrestedLeader.ArrowPoints.Count - 1)
                    {
                        AcadUtils.WriteMessageInDebug($"меняем endpoint {CrestedLeader.EndPoint}");

                        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                        //CrestedLeader.ArrowPoints.Add();

                        points.Sort((p1, p2) => GetPointOnPolyline(p1, mainLine, leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, mainLine, leaderNormal).DistanceTo(sortPoint)));
                        CrestedLeader.ArrowPoints = points;

                        var firstPoint = CrestedLeader.ArrowPoints[0];
                        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, mainLine, leaderNormal);
                        var newEndPoint = GetPointOnPolyline(lastPoint, mainLine, leaderNormal);
                        
                        if (newEndPoint.X > tmpEndPoint.X)
                        {
                            CrestedLeader.TextIndent -= newEndPoint.DistanceTo(tmpEndPoint);
                            if (CrestedLeader.TextIndent < 0)
                            {
                                CrestedLeader.TextIndent = CrestedLeader.MinDistanceBetweenPoints;
                            }
                            CrestedLeader.EndPoint = newEndPoint;
                        }
                        else
                        {
                            CrestedLeader.EndPoint = tmpEndPoint;
                            if (CrestedLeader.TextIndent < 0)
                            {
                                CrestedLeader.TextIndent = CrestedLeader.MinDistanceBetweenPoints;
                            }
                        }
                    }
                    else
                    {
                        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                        
                        points.Sort((p1, p2) => GetPointOnPolyline(p1, mainLine, leaderNormal).DistanceTo(sortPoint).CompareTo(GetPointOnPolyline(p2, mainLine, leaderNormal).DistanceTo(sortPoint)));
                        CrestedLeader.ArrowPoints = points;

                        var firstPoint = CrestedLeader.ArrowPoints[0];
                        var lastPoint = CrestedLeader.ArrowPoints[CrestedLeader.ArrowPoints.Count - 1];

                        CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, mainLine, leaderNormal);

                        var tempNewEndPoint = GetPointOnPolyline(lastPoint, mainLine, leaderNormal);

                        if (tempNewEndPoint.X < tmpEndPoint.X)
                        {
                            CrestedLeader.TextIndent = tmpEndPoint.DistanceTo(tempNewEndPoint) - CrestedLeader.LargestTextLength;
                            CrestedLeader.EndPoint = tempNewEndPoint;
                            if (CrestedLeader.TextIndent < 0)
                            {
                                CrestedLeader.TextIndent = CrestedLeader.MinDistanceBetweenPoints;
                            }
                            
                        }
                        else
                        {
                            CrestedLeader.EndPoint = tempNewEndPoint;

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
            //CrestedLeader.TextIndent = double.NaN;
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