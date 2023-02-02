using System;
using System.Collections.Generic;
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
            //_leaderGripTmp = CrestedLeader.LeaderPoint;
        }

        //if (newStatus == Status.Stretch)
        //{
        //    CrestedLeader.InsertionPoint = _startGripTmp;
        //    CrestedLeader.EndPoint = _endGripTmp;
        //    //CrestedLeader.LeaderPoint = _leaderGripTmp;
        //}

        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var tmpInsPoint = CrestedLeader.InsertionPoint;
                var tmpEndPoint = CrestedLeader.EndPoint;
                //var tmpLeaderPoint = CrestedLeader.LeaderPoint;

                var tempLine = new Line(CrestedLeader.InsertionPoint, CrestedLeader.EndPoint);
                var mainNormal = (CrestedLeader.InsertionPoint - CrestedLeader.ArrowPoints[0]).GetNormal();
                var pointOnPolyline = CreateLeadersWithArrows(tempLine, Intersect.ExtendBoth, CrestedLeader.TempNewArrowPoint, mainNormal);

                var isOnSegment = IsPointBetween(pointOnPolyline, tmpInsPoint, tmpEndPoint);

                if (!isOnSegment)
                {
                    var distToInsPoint = pointOnPolyline.DistanceTo(tmpInsPoint);
                    var distToEndPoint = pointOnPolyline.DistanceTo(tmpEndPoint);
                    //var distToLeaderPoint = pointOnPolyline.DistanceTo(tmpLeaderPoint);
                    if (distToInsPoint < distToEndPoint)
                    {
                        CrestedLeader.InsertionPoint = pointOnPolyline;
                        var tmpPoint = CrestedLeader.ArrowPoints[0];
                        //CrestedLeader.ArrowPoints[0] = CrestedLeader.TempNewArrowPoint;
                        CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        //CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;

                        AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем InserPoint");
                    }
                    else
                    {
                        CrestedLeader.EndPoint = pointOnPolyline;
                        //CrestedLeader.LeaderPoint = tmpLeaderPoint;
                        //var tmpPoint = CrestedLeader.ArrowPoints.LastIndexOf();
                        //CrestedLeader.ArrowPoints.Add();
                        CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        
                        //CrestedLeader.InsertionPoint = CrestedLeader.TempNewArrowPoint;
                        
                        AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} меняем EndPoint");
                    }
                }
                else 
                {
                    CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                }
                
                CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);
                var tempList = SortByDistance(CrestedLeader.ArrowPoints, CrestedLeader.InsertionPoint);
                CrestedLeader.ArrowPoints = tempList;
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

    List<Point3d> SortByDistance(List<Point3d> lst, Point3d startPoint)
    {
        List<Point3d> output = new List<Point3d>();
        output.Add(lst[NearestPoint(startPoint, lst)]);
        lst.Remove(output[0]);
        int x = 0;
        for (int i = 0; i < lst.Count + x; i++)
        {
            output.Add(lst[NearestPoint(output[output.Count - 1], lst)]);
            lst.Remove(output[output.Count - 1]);
            x++;
        }

        return output;
    }

    int NearestPoint(Point3d srcPt, List<Point3d> lookIn)
    {
        KeyValuePair<double, int> smallestDistance = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Math.Sqrt(Math.Pow(srcPt.X - lookIn[i].X, 2) + Math.Pow(srcPt.Y - lookIn[i].Y, 2));
            if (i == 0)
            {
                smallestDistance = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < smallestDistance.Key)
                {
                    smallestDistance = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return smallestDistance.Value;
    }
}
