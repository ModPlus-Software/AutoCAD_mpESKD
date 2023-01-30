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
                var tempInsPoint = CrestedLeader.InsertionPoint;
                //TODO 
                if (GripIndex == 0)
                {
                    AcadUtils.WriteMessageInDebug($"CrestedLeader.TempNewArrowPoint {CrestedLeader.TempNewArrowPoint}");
                }
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
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
                            CrestedLeader.ArrowPoints[GripIndex] = (CrestedLeader.TempNewArrowPoint);
                            //CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;

                            AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем LeaderPoint");
                        }
                        else
                        {
                            CrestedLeader.EndPoint = pointOnPolyline;
                            CrestedLeader.LeaderPoint = tmpLeaderPoint;
                            CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;
                            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.InsertionPoint;
                        
                            AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем EndPoint");
                        }
                    }
                    else 
                    {
                        //CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    }

                    //if (!CrestedLeader.IsLeft)
                    //{
                    //    distFromEndPointToInsPoint = -1 * CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    //}

                    //var tempList = new List<Point3d>();
                    //tempList.AddRange(CrestedLeader.ArrowPoints);
                    //var result = tempList.OrderBy(x => x).FirstOrDefault();

                    //if (CrestedLeader.TempNewArrowPoint >= 0)
                    //{
                    //    // если в списке есть значения и они положительные, то берем последнюю
                    //    if (result > 0)
                    //    {
                    //        result = tempList.OrderBy(x => x).LastOrDefault();

                    //        // если последняя больше чем текущая
                    //        if (result > CrestedLeader.TempNewArrowPoint)
                    //        {
                    //            // текущую добавляем в список, inspoint не меняем
                    //            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //        }
                    //        else
                    //        {
                    //            // если текущая больше чем последняя она должна быть insPoint
                    //            tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);
                    //            CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //    }
                    //}
                    //else
                    //{
                    //    // ищем первую
                    //    tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);

                    //    // если первая положительная, значит слева нет точек
                    //    if (IsOnsegment)
                    //    {
                            
                    //        tempInsPoint = CrestedLeader.InsertionPoint;
                    //    }
                    //    else if (CrestedLeader.TempNewArrowPoint > distFromEndPointToInsPoint)
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = CrestedLeader.TempNewArrowPoint;
                    //        tempInsPoint = CrestedLeader.InsertionPoint;
                    //    }
                    //    else if (CrestedLeader.TempNewArrowPoint < result)
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = -1 * distFromEndPointToInsPoint;
                    //    }
                    //    else
                    //    {
                    //        CrestedLeader.ArrowPoints[GripIndex] = distFromEndPointToInsPoint;
                    //    }
                    //}
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