namespace mpESKD.Functions.mpCrestedLeader;

using Base.Enums;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using Base.Utils;
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
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    /// <inheritdoc />
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
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

                               var intersectPoint = Intersections.GetIntersectionBetweenCircleLine(line, circle);

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
                       var crestedLeader = leaderMoveGrip.CrestedLeader;

                       leaderMoveGrip.NewPoint = leaderMoveGrip.GripPoint + offset;
                       var newPoint = leaderMoveGrip.NewPoint;

                       var leaderStartPoint = crestedLeader.LeaderStartPoints[leaderMoveGrip.GripIndex];
                       var leaderEndPoint = crestedLeader.LeaderEndPoints[leaderMoveGrip.GripIndex];

                       var vectorLeader = leaderEndPoint.ToPoint2d() - leaderStartPoint.ToPoint2d();

                       if (Intersections.GetIntersectionBetweenVectors(
                               newPoint,
                               vectorLeader,
                               crestedLeader.InsertionPoint,
                               Vector2d.XAxis) is { } tempLineStartPoint)
                       {
                           // todo Проверка на мин расстояние, чтобы не наложились точки начал выносок

                           crestedLeader.LeaderStartPoints[leaderMoveGrip.GripIndex] = tempLineStartPoint;
                           crestedLeader.LeaderEndPoints[leaderMoveGrip.GripIndex] = newPoint;

                           var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                           var vectorToShelfLedgePoint = crestedLeader.ShelfLedgePoint - crestedLeader.ShelfStartPoint;
                           var vectorToShelfEndPoint = crestedLeader.ShelfEndPoint - crestedLeader.ShelfLedgePoint;

                           if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                           {
                               crestedLeader.ShelfStartPoint = leaderStartPointsSort.Last();

                               foreach (var leaderEndPt in crestedLeader.LeaderEndPoints)
                               {
                                   var intersection = Intersections.GetIntersectionBetweenVectors(
                                       leaderEndPt,
                                       vectorLeader,
                                       crestedLeader.InsertionPoint,
                                       Vector2d.XAxis);

                                   if (intersection.Equals(leaderStartPointsSort.Last()))
                                   {
                                       crestedLeader.BoundEndPoint = leaderEndPt;
                                   }
                               }
                           }
                           else
                           {
                               crestedLeader.ShelfStartPoint = leaderStartPointsSort.First();

                               foreach (var leaderEndPt in crestedLeader.LeaderEndPoints)
                               {
                                   var intersection = Intersections.GetIntersectionBetweenVectors(
                                       leaderEndPt,
                                       vectorLeader,
                                       crestedLeader.InsertionPoint,
                                       Vector2d.XAxis);

                                   if (intersection.Equals(leaderStartPointsSort.First()))
                                   {
                                       crestedLeader.BoundEndPoint = leaderEndPt;
                                   }
                               }
                           }

                           crestedLeader.ShelfLedgePoint = crestedLeader.ShelfStartPoint + vectorToShelfLedgePoint;
                           crestedLeader.ShelfEndPoint = crestedLeader.ShelfLedgePoint + vectorToShelfEndPoint;


                           crestedLeader.IsFirst = true;
                           crestedLeader.IsLeaderPointMovedByOverrule = true;
                       }

                       crestedLeader.UpdateEntities();
                       crestedLeader.BlockRecord.UpdateAnonymousBlocks();
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