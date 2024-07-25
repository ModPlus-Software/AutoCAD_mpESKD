﻿using System;
using System.Linq;

namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Enums;
using mpESKD.Base.Utils;
using System.Collections.Generic;
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
        Loggerq.WriteRecord("CrestedLeaderGripPointOverrule: GetGripPoints()");

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
                    var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                    #region insertGripPoint

                    // insertion (start) grip
                    //Point3d insertGripPoint;

                    // Перемещение ручки по положению полки
                    //if (crestedLeader.IsChangeShelfPosition)
                    //{
                    //    insertGripPoint = crestedLeader.ShelfStartPoint;
                    //}
                    //else
                    //{
                    //    insertGripPoint = crestedLeader.InsertionPoint;
                    //}

                    // insertGripPoint = crestedLeader.ShelfStartPoint;

                    var insertGrip = new CrestedLeaderGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfStartPoint
                    };

                    grips.Add(insertGrip);

                    #endregion


                    #region shelfMoveGripPoint

                    //Point3d shelfMoveGripPoint;

                    //shelfMoveGripPoint = crestedLeader.ShelfLedgePoint;

                    var shelfMoveGrip = new CrestedLeaderShelfMoveGrip(crestedLeader, 0)
                    {
                        GripPoint = crestedLeader.ShelfLedgePoint
                    };

                    grips.Add(shelfMoveGrip);
                    #endregion



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
        Loggerq.WriteRecord("CrestedLeaderGripPointOverrule: MoveGripPointsAt() => START");

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
                        var newPoint = shelfMoveGrip.NewPoint;

                        // новое значение ShelfPosition(? , ShelfStartPoint, ShelfLedgePoint, ShelfEndPoint
                        var leaderStartPointsSort = crestedLeader.LeaderStartPoints.OrderBy(p => p.X);
                        var leftStartPoint = leaderStartPointsSort.First();
                        var rightStartPoint = leaderStartPointsSort.Last();

                        var midUnionLinePoint = GeometryUtils.GetMiddlePoint3d(leftStartPoint, rightStartPoint);

                        if ((crestedLeader.ShelfPosition == ShelfPosition.Right && newPoint.X >= midUnionLinePoint.X) ||
                            (crestedLeader.ShelfPosition == ShelfPosition.Left && newPoint.X < midUnionLinePoint.X))
                        {
                            if (newPoint.X >= leftStartPoint.X && newPoint.X <= rightStartPoint.X)
                            {
                                /*
                                if (crestedLeader.IsChangeShelfPosition)
                                {
                                    if ((crestedLeader.ShelfPosition == ShelfPosition.Left && crestedLeader.InsertionPoint.Equals(rightStartPoint)) ||
                                         (crestedLeader.ShelfPosition == ShelfPosition.Right && crestedLeader.InsertionPoint.Equals(leftStartPoint)))
                                    {
                                        crestedLeader.ShelfLedge = rightStartPoint.ToPoint2d().GetDistanceTo(leftStartPoint.ToPoint2d());
                                    }
                                    else
                                    {
                                        crestedLeader.ShelfLedge = 0;
                                    }

                                    crestedLeader.IsChangeShelfPosition = false;
                                }
                                else
                                {
                                    crestedLeader.ShelfLedge = 0;
                                }*/


                                if (crestedLeader.InsertionPoint.Equals(rightStartPoint) &&
                                    crestedLeader.ShelfPosition == ShelfPosition.Left)
                                {
                                    crestedLeader.ShelfLedge = rightStartPoint.ToPoint2d().GetDistanceTo(leftStartPoint.ToPoint2d());
                                }
                                else if (crestedLeader.InsertionPoint.Equals(leftStartPoint) &&
                                         crestedLeader.ShelfPosition == ShelfPosition.Right)
                                {
                                    crestedLeader.ShelfLedge = rightStartPoint.ToPoint2d().GetDistanceTo(leftStartPoint.ToPoint2d());
                                }
                                else
                                {
                                    crestedLeader.ShelfLedge = 0;
                                }
                            }
                            else
                            {
                                crestedLeader.ShelfLedge = Math.Abs(newPoint.X - crestedLeader.ShelfStartPoint.X);
                            }

                        }
                        // Если полка вправо, а курсор налево
                        else if (crestedLeader.ShelfPosition == ShelfPosition.Right && newPoint.X < midUnionLinePoint.X)
                        {
                            if (newPoint.X >= leftStartPoint.X)
                            {
                              //  crestedLeader.ShelfLedge = rightStartPoint.ToPoint2d().GetDistanceTo(leftStartPoint.ToPoint2d());
                            }
                            else
                            {
                              //  crestedLeader.ShelfLedge = Math.Abs(newPoint.X - leftStartPoint.X);
                            }

                            crestedLeader.ShelfPosition = ShelfPosition.Left;

                            Loggerq.WriteRecord($"CrestedLeaderGripPointOverrule: MoveGripPointsAt() => " +
                                                $"ShelfPosition set is LEFT");
                            crestedLeader.PrevShelfPosition = ShelfPosition.Right;
                            crestedLeader.IsChangeShelfPosition = true;
                        }

                        // Если полка влево, а курсор направо
                        else if (crestedLeader.ShelfPosition == ShelfPosition.Left && newPoint.X >= midUnionLinePoint.X)
                        {
                            if (newPoint.X <= rightStartPoint.X)
                            {
                               // crestedLeader.ShelfLedge = rightStartPoint.ToPoint2d().GetDistanceTo(leftStartPoint.ToPoint2d());
                            }
                            else
                            {
                               // crestedLeader.ShelfLedge = Math.Abs(newPoint.X - rightStartPoint.X);
                            }

                            crestedLeader.PrevShelfPosition = ShelfPosition.Left;
                            crestedLeader.ShelfPosition = ShelfPosition.Right;

                            crestedLeader.IsChangeShelfPosition = true;
                        }
                        Loggerq.WriteRecord($"CrestedLeaderGripPointOverrule: MoveGripPointsAt() => " +
                                            $"ShelfPosition: {crestedLeader.ShelfPosition.ToString()}");

                        Loggerq.WriteRecord($"CrestedLeaderGripPointOverrule: MoveGripPointsAt() => " +
                                        $"IsChangeShelfPosition: {crestedLeader.IsChangeShelfPosition}");

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