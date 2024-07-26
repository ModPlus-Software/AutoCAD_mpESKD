namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Utils;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class CrestedLeaderGripPointOverrule : BaseSmartEntityGripOverrule<CrestedLeader>
{
    /// <inheritdoc />
    public override void GetGripPoints(
        Entity entity,
        GripDataCollection grips,
        double curViewUnitSize,
        int gripSize,
        Vector3d curViewDir,
        GetGripPointsFlags bitFlags)
    {
       // Loggerq.WriteRecord("CrestedLeaderGripPointOverrule: GetGripPoints()");

        try
        {
            if (IsApplicable(entity))
            {
                // Удаляю все ручки - это удалит ручку вставки блока

                var blkRef = (BlockReference)entity;
                GripData toRemove = null;

                foreach (var gd in grips)
                {
                    if (gd.GripPoint == blkRef.Position)
                    {
                        toRemove = gd;
                        break;
                    }
                }

                if (toRemove != null)
                {
                    grips.Remove(toRemove);
                }

                var crestedLeader = EntityReaderService.Instance.GetFromEntity<CrestedLeader>(entity);
                if (crestedLeader != null)
                {
                    var insertGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfStartPoint
                    };

                    grips.Add(insertGrip);


                    var shelfMoveGrip = new CrestedLeaderShelfMoveGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfLedgePoint
                    };

                    grips.Add(shelfMoveGrip);

                    // ручки переноса выносок
                    for (var i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                    {
                        var leaderMoveGrip = new CrestedLeaderMoveLeaderGrip(crestedLeader, i)
                        {
                            GripPoint = crestedLeader.LeaderEndPoints[i]
                        };

                        grips.Add(leaderMoveGrip);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            // todo
            //if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
            //    ExceptionBox.Show(exception);

            Loggerq.WriteRecord("CrestedLeaderGripPointOverrule: GetGripPoints() => ERROR");
        }
    }

    /// <inheritdoc />
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
       // Loggerq.WriteRecord("CrestedLeaderGripPointOverrule: MoveGripPointsAt() => START");

        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is CrestedLeaderGrip insertGrip)
                    {
                        var crestedLeader = insertGrip.CrestedLeader;

                        if (insertGrip.GripIndex == 0)
                        {
                            insertGrip.NewPoint = insertGrip.GripPoint + offset;
                            var newPoint = insertGrip.NewPoint;

                            // если newPoint приближается слишком близко к одной из точек EndPoint выносок,
                            // то не дать этого сделать

                            var minDist = crestedLeader.MinDistanceBetweenPoints;

                            if (crestedLeader.LeaderEndPoints.Any(p =>
                                    newPoint.ToPoint2d().GetDistanceTo(p.ToPoint2d()) < minDist))
                            {
                                // найдем LeaderEndPoint, к которому приближается newpoint и
                                // не соблюдается требование к минимальному расстоянию 

                                var searchLeaderEndPoint = crestedLeader.LeaderEndPoints
                                    .Select(leaderEndPoint => new
                                    {
                                        Point = leaderEndPoint,
                                        Distance = leaderEndPoint.ToPoint2d().GetDistanceTo(newPoint.ToPoint2d())
                                    })
                                    .OrderBy(p => p.Distance)
                                    .First();

                                Point3d searchPoint = searchLeaderEndPoint.Point;

                                // Найдем точку пересечения окружности с радиусом minDist и отрезка к searchPoint

                                var lineStartPoint = searchPoint + ((newPoint - searchPoint) * minDist * 2);
                                var line = new Line(lineStartPoint, searchPoint);

                                var circle = new Circle()
                                {
                                    Center = searchPoint,
                                    Radius = minDist,
                                };

                                var intersectPoint = CircleLineIntersection.GetIntersection(line, circle);

                                // перенесем newpPoint на пересечение окружности с радиусом minDist и отрезка к searchPoint
                                newPoint = intersectPoint ?? newPoint;
                            }

                            ((BlockReference)entity).Position = newPoint;
                            crestedLeader.InsertionPoint = newPoint;

                            crestedLeader.IsBasePointMovedByOverrule = true;

                            crestedLeader.UpdateEntities();
                            crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                        }
                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var crestedLeader = shelfMoveGrip.CrestedLeader;
                        shelfMoveGrip.NewPoint = shelfMoveGrip.GripPoint + offset;

                        ShelfActions.ShelfPositionMove(ref crestedLeader, shelfMoveGrip.NewPoint);

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderMoveLeaderGrip leaderMoveGrip)
                    {
                        leaderMoveGrip.NewPoint = leaderMoveGrip.GripPoint + offset;
                    }
                    else
                    {
                        base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                    }
                }
            }
            else
            {
                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

}