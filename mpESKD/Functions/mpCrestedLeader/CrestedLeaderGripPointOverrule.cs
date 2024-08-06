using System;
using System.Collections.Generic;

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
using mpESKD.Functions.mpRevisionMark.Grips;
using mpESKD.Functions.mpRevisionMark;
using System.Reflection;

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
                    var baseVector = crestedLeader.BaseVectorNormal;

                    var insertGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfStartPoint
                    };

                    grips.Add(insertGrip);

                    // Ручка перемещения полки
                    var shelfMoveGrip = new CrestedLeaderShelfMoveGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfLedgePoint
                    };
                    grips.Add(shelfMoveGrip);

                    // Ручка смены позиции полки
                    //                    var shelfPositionGripPoint = crestedLeader.ShelfLedgePoint + (Vector3d.YAxis * 20 * curViewUnitSize);
                    var shelfPositionGripPoint = crestedLeader.ShelfLedgePoint + (baseVector.GetPerpendicularVector() * 20 * curViewUnitSize);
                    if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        // shelfPositionGripPoint -= Vector3d.XAxis * 20 * curViewUnitSize;
                        shelfPositionGripPoint -= baseVector * 20 * curViewUnitSize;
                    }
                    else
                    {
                        //                         shelfPositionGripPoint += Vector3d.XAxis * 20 * curViewUnitSize;
                        shelfPositionGripPoint += baseVector * 20 * curViewUnitSize;
                    }

                    var shelfPositionGrip = new CrestedLeaderShelfPositionGrip(crestedLeader)
                    {
                        GripPoint = shelfPositionGripPoint //.GetRotatedPointByBlock(crestedLeader)
                    };
                    grips.Add(shelfPositionGrip);

                    // Ручки переноса выносок - в начале выносок
                    for (var i = 0; i < crestedLeader.LeaderStartPoints.Count; i++)
                    {
                        if (!crestedLeader.InsertionPoint.Equals(crestedLeader.LeaderStartPoints[i]))
                        {
                            var leaderMoveGrip = new CrestedLeaderStartPointLeaderGrip(crestedLeader, i)
                            {
                                GripPoint = crestedLeader.LeaderStartPoints[i]
                            };

                            grips.Add(leaderMoveGrip);
                        }
                    }
                    // Ручки переноса выносок - в конце выносок
                    for (var i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                    {
                        var leaderMoveGrip = new CrestedLeaderEndPointLeaderGrip(crestedLeader, i)
                        {
                            GripPoint = crestedLeader.LeaderEndPoints[i]
                        };

                        grips.Add(leaderMoveGrip);
                    }

                    // Ручка добавления выносок 
                    var addLeaderGripPoint = crestedLeader.ShelfLedgePoint - (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize);
                    if (crestedLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        //addLeaderGripPoint -= Vector3d.XAxis * 20 * curViewUnitSize;
                        addLeaderGripPoint -= baseVector.GetNormal() * 20 * curViewUnitSize;
                    }
                    else
                    {
                        addLeaderGripPoint += baseVector.GetNormal() * 20 * curViewUnitSize;
                    }

                    var addLeaderGrip = new CrestedLeaderAddLeaderGrip(crestedLeader)
                    {
                        
                        GripPoint = addLeaderGripPoint //.GetRotatedPointByBlock(crestedLeader)
                    };

                    grips.Add(addLeaderGrip);

                    // Ручка удаления выносок 
                    if (crestedLeader.LeaderStartPoints.Count > 1)
                    {
                        for (int i = 0; i < crestedLeader.LeaderEndPoints.Count; i++)
                        {
                            var removeLeaderGripPoint = crestedLeader.LeaderEndPointsOCS[i].Y > crestedLeader.ShelfStartPointOCS.Y
                                    // crestedLeader.LeaderEndPoints[i] - (Vector3d.XAxis * 20 * curViewUnitSize);
                                    ? crestedLeader.LeaderStartPoints[i] -
                                      (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize)
                                    : crestedLeader.LeaderStartPoints[i] +
                                      (baseVector.GetPerpendicularVector().GetNormal() * 20 * curViewUnitSize);

                            grips.Add(new CrestedLeaderLeaderRemoveGrip(crestedLeader, i)
                            {
                                GripPoint = removeLeaderGripPoint
                            });
                        }
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
                       crestedLeader.ToLogAnyString(
                           "CrestedLeaderGripPointOverrule: MoveGripPointsAt: CrestedLeaderGrip");

                       if (insertGrip.GripIndex == 0)
                       {
                           insertGrip.NewPoint = insertGrip.GripPoint + offset;
                           var newPoint = insertGrip.NewPoint;

                            /*
                            if (crestedLeader.LeaderEndPoints.Any(p => p.Equals(newPoint)))
                            {
                                return;
                            }*/

                            /*
                            if (crestedLeader.LeaderEndPoints.Any(p => p.Y.Equals(newPoint.Y)))
                            {
                                newPoint = new Point3d(
                                    newPoint.X,
                                    newPoint.Y + crestedLeader.MinDistanceBetweenPoints,
                                    newPoint.Z);
                            }*/

                            // если newPoint приближается слишком близко к одной из точек EndPoint выносок,
                            // то не дать этого сделать
                            // var minDist = crestedLeader.MinDistanceBetweenPoints;

                            /*
                             var ptCheck = Point3d.Origin + (newPoint - crestedLeader.InsertionPoint);

                             ptCheck.ToLog("ptCheck");


                            if (crestedLeader.BaseLeaderEndPointOCS.Y.Equals(ptCheck.Y))
                            //if (crestedLeader.LeaderEndPointsOCS.Any(p => p.Y.Equals(crestedLeader.InsertionPointOCS.Y)))
                             //if (crestedLeader.BaseLeaderEndPointOCS.Y.Equals(crestedLeader.InsertionPointOCS.Y))
                             {
                                 newPoint = new Point3d(
                                     newPoint.X,
                                     newPoint.Y + crestedLeader.MinDistanceBetweenPoints,
                                     newPoint.Z);
                             }*/

                            /*
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
                            */

                            // Проверка на приближение курсора к концам выносок
                            if (crestedLeader.LeaderEndPoints
                                .Any(p => newPoint.DistanceTo(p) < crestedLeader.MinDistanceBetweenPoints))
                            {
                                newPoint = newPoint.GetNormalizedPointByDistToPointSet(
                                    crestedLeader.LeaderEndPoints, 
                                    crestedLeader.MinDistanceBetweenPoints);
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
                       crestedLeader.ToLogAnyString(
                           "CrestedLeaderGripPointOverrule: MoveGripPointsAt: CrestedLeaderShelfMoveGrip");

                       shelfMoveGrip.NewPoint = shelfMoveGrip.GripPoint + offset;

                       ShelfActions.ShelfPositionMove(ref crestedLeader, shelfMoveGrip.NewPoint);

                       crestedLeader.UpdateEntities();
                       crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                   }
                   else if (gripData is CrestedLeaderEndPointLeaderGrip leaderEndGrip)
                   {
                       var crestedLeader = leaderEndGrip.CrestedLeader;

                       crestedLeader.ToLogAnyString(
                           "CrestedLeaderGripPointOverrule: MoveGripPointsAt: CrestedLeaderEndPointLeaderGrip");

                       leaderEndGrip.NewPoint = leaderEndGrip.GripPoint + offset;
                       var newPoint = leaderEndGrip.NewPoint;

                       // Точка проекции newPoint на центральную линию
                       var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                       var leaderStartPoint = crestedLeader.LeaderStartPoints[leaderEndGrip.GripIndex];
                       var leaderEndPoint = crestedLeader.LeaderEndPoints[leaderEndGrip.GripIndex];

                       if (newPoint.DistanceTo(newPointProject) < crestedLeader.MinDistanceBetweenPoints)
                       {
                           var vectorToNewPointNormal = (newPoint - newPointProject).GetNormal();
                           var vectorToCorrectNewPoint = vectorToNewPointNormal * crestedLeader.MinDistanceBetweenPoints;

                           newPoint = newPointProject + vectorToCorrectNewPoint;
                       }

                       var vectorLeader = leaderEndPoint.ToPoint2d() - leaderStartPoint.ToPoint2d();

                       if (Intersections.GetIntersectionBetweenVectors(
                               crestedLeader.InsertionPoint,
                               crestedLeader.BaseVectorNormal.ToVector2d(),
                               newPoint,
                               vectorLeader) is { } tempLineStartPoint)
                       {
                           bool isValidPoint =
                                 !(newPoint.Point3dToPoint3dOcs(crestedLeader).Y.Equals(leaderStartPoint.Point3dToPoint3dOcs(crestedLeader).Y) || 
                                 crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)) || 
                                 crestedLeader.LeaderEndPoints.Any(p => p.Equals(newPoint) && !p.Equals(leaderEndPoint)));

                           if (isValidPoint)
                           {
                               crestedLeader.LeaderStartPoints[leaderEndGrip.GripIndex] = tempLineStartPoint;
                               crestedLeader.LeaderEndPoints[leaderEndGrip.GripIndex] = newPoint;

                               var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                               crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right 
                                    ? leaderStartPointsSort.Last() 
                                    : leaderStartPointsSort.First();
                               
                               crestedLeader.IsStartPointsAssigned = true;

                               crestedLeader.UpdateEntities();
                               crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                           }

                           tempLineStartPoint.ToLog("tempLineStartPoint");
                       }
                   }
                   else if (gripData is CrestedLeaderStartPointLeaderGrip leaderStartGrip)
                   {
                       var crestedLeader = leaderStartGrip.CrestedLeader;

                        crestedLeader.ToLogAnyString(
                           "CrestedLeaderGripPointOverrule: MoveGripPointsAt: CrestedLeaderStartPointLeaderGrip");

                       var leaderStartPoint = crestedLeader.LeaderStartPoints[leaderStartGrip.GripIndex];
                       var leaderEndPoint = crestedLeader.LeaderEndPoints[leaderStartGrip.GripIndex];

                       leaderStartGrip.NewPoint = leaderStartGrip.GripPoint + offset;
                       var newPoint = leaderStartGrip.NewPoint;

                       // Точка проекции newPoint на центральную линию
                       var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                       bool isValidPoint = 
                           !(crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)) ||
                            crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPointProject)) ||
                                             crestedLeader.LeaderEndPoints.Any(p => p.Equals(newPoint)) ||
                                             newPoint.Equals(crestedLeader.InsertionPoint) ||
                                             newPoint.Equals(crestedLeader.ShelfLedgePoint) ||
                                             newPoint.Equals(crestedLeader.ShelfEndPoint));

                       if (isValidPoint)
                       {
                            /*
                           var vectorToShelfEndPoint = crestedLeader.ShelfEndPoint - crestedLeader.ShelfLedgePoint;
                           var vectorToShelfLedgePoint = crestedLeader.ShelfLedgePoint - crestedLeader.ShelfStartPoint;*/

                            //var tempLineStartPoint = newPointProject; //new Point3d(newPoint.X, leaderStartPoint.Y, leaderStartPoint.Z);

                           crestedLeader.LeaderStartPoints[leaderStartGrip.GripIndex] = newPointProject;

                           var vectorTempLineToEndPoint = leaderEndPoint - leaderStartPoint;
                           crestedLeader.LeaderEndPoints[leaderStartGrip.GripIndex] = newPointProject + vectorTempLineToEndPoint;

                            /*
                           // Список векторов от начал к концам выносок
                           List<Vector3d> vectorsToEndPoint = crestedLeader.LeaderStartPoints
                               .Select((t, i) => crestedLeader.LeaderEndPoints[i] - t).ToList();

                            // var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();
                            var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                           // Список концов выносок, с учетом нового положения перемещаемой выноски
                           List<Point3d> leaderEndPointsSort = new();
                           for (int i = 0; i < leaderStartPointsSort.Count; i++)
                           {
                               foreach (var endPoint in crestedLeader.LeaderEndPoints)
                               {
                                   var vectorToEndPoint = endPoint - leaderStartPointsSort[i];

                                   if (vectorsToEndPoint.Any(v => v.Equals(vectorToEndPoint)))
                                   {
                                       leaderEndPointsSort.Add(endPoint);
                                       break;
                                   }
                               }
                           }
                           /*

                           crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right 
                               ? leaderStartPointsSort.Last() 
                               : leaderStartPointsSort.First();

                           /*
                           crestedLeader.ShelfLedgePoint = crestedLeader.ShelfStartPoint + vectorToShelfLedgePoint;
                           crestedLeader.ShelfEndPoint = crestedLeader.ShelfLedgePoint + vectorToShelfEndPoint;*/

                            var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                            crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right
                                ? leaderStartPointsSort.Last()
                                : leaderStartPointsSort.First();

                            crestedLeader.IsStartPointsAssigned = true;
                           //crestedLeader.IsMoveGripPointsAt = true;

                           crestedLeader.UpdateEntities();
                           crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                       }
                   }
                   else if (gripData is CrestedLeaderAddLeaderGrip addLeaderGrip)
                   {
                        var crestedLeader = addLeaderGrip.CrestedLeader;

                        addLeaderGrip.NewPoint = addLeaderGrip.GripPoint + offset;
                        var newPoint = addLeaderGrip.NewPoint;

                        // Точка проекции newPoint на центральную линию
                        var newPointProject = newPoint.GetProjectPointToBaseLine(crestedLeader);

                        var leaderStartPoint = crestedLeader.LeaderStartPoints.Last();

                        if (newPoint.DistanceTo(newPointProject) < crestedLeader.MinDistanceBetweenPoints)
                        {
                            var vectorToNewPointNormal = (newPoint - newPointProject).GetNormal();
                            var vectorToCorrectNewPoint = vectorToNewPointNormal * crestedLeader.MinDistanceBetweenPoints;

                            newPoint = newPointProject + vectorToCorrectNewPoint;
                        }

                        var vectorLeader = crestedLeader.BaseLeaderEndPoint - crestedLeader.InsertionPoint;

                        if (Intersections.GetIntersectionBetweenVectors(
                                crestedLeader.InsertionPoint,
                                crestedLeader.BaseVectorNormal.ToVector2d(),
                                newPoint,
                                vectorLeader.ToVector2d()) is { } tempLineStartPoint)
                        {
                            bool isValidPoint =
                                  !(newPoint.Point3dToPoint3dOcs(crestedLeader).Y.Equals(leaderStartPoint.Point3dToPoint3dOcs(crestedLeader).Y) ||
                                  crestedLeader.LeaderStartPoints.Any(p => p.Equals(newPoint)));

                            if (isValidPoint)
                            {
                                crestedLeader.LeaderStartPoints[crestedLeader.LeaderStartPoints.Count - 1] = tempLineStartPoint;
                                crestedLeader.LeaderEndPoints[crestedLeader.LeaderStartPoints.Count - 1] = newPoint;

                                var leaderStartPointsSort = crestedLeader.LeaderStartPointsSorted;

                                crestedLeader.ShelfStartPoint = crestedLeader.ShelfPosition == ShelfPosition.Right
                                     ? leaderStartPointsSort.Last()
                                     : leaderStartPointsSort.First();

                                crestedLeader.IsStartPointsAssigned = true;

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                            }
                        }
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