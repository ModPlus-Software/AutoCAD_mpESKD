﻿namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using ModPlusAPI.Windows;
using mpESKD.Base.Overrules.Grips;
using Grips;
using System;
using System.Collections.Generic;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class CrestedLeaderGripPointOverrule : BaseSmartEntityGripOverrule<CrestedLeader>
{
    private readonly List<double> _distArrowPointsFromInsPoint = new ();

    /// <inheritdoc />
    public override void GetGripPoints(
        Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
    {
        try
        {
            // Проверка дополнительных условий
            if (IsApplicable(entity))
            {
                // Чтобы "отключить" точку вставки блока, нужно получить сначала блок
                // Т.к. мы точно знаем для какого примитива переопределение, то получаем блок:
                var blkRef = (BlockReference)entity;

                // Удаляем стандартную ручку позиции блока (точки вставки)
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

                // Получаем экземпляр класса, который описывает как должен выглядеть примитив
                // т.е. правила построения графики внутри блока
                // Информация собирается по XData и свойствам самого блока
                var crestedLeader = EntityReaderService.Instance.GetFromEntity<CrestedLeader>(entity);

                if (crestedLeader != null)
                {
                    // Получаем ручку на первой точке
                    var gp = new CrestedLeaderVertexGrip(crestedLeader, 0, (BlockReference)entity)
                    {
                        GripPoint = crestedLeader.InsertionPoint
                    };
                    grips.Add(gp);

                    // Получаем ручку на второй точке
                    //gp = new CrestedLeaderVertexGrip(crestedLeader, 1, (BlockReference)entity)
                    //{
                    //    GripPoint = crestedLeader.EndPoint
                    //};
                    //grips.Add(gp);
                    //_distArrowPointsFromInsPoint.Clear();

                    //var distFromEndPointToInsPoint = crestedLeader.EndPoint.DistanceTo(crestedLeader.InsertionPoint);
                    //foreach (var distance in crestedLeader.ArrowPoints)
                    //{
                    //    //if (distance < 0)
                    //    //{
                    //    //    _distArrowPointsFromInsPoint.Add(distFromEndPointToInsPoint - Math.Abs(distance));
                    //    //}
                    //    //else
                    //    //{
                    //    //    _distArrowPointsFromInsPoint.Add(distFromEndPointToInsPoint + distance);
                    //    //}
                    //}

                    // получаем ручку для создания стрелки
                    grips.Add(new CrestedLeaderArrowAddGrip(crestedLeader, (BlockReference)entity)
                    {
                        GripPoint = crestedLeader.EndPoint - (Vector3d.XAxis * 20 * curViewUnitSize)
                    });

                    if (crestedLeader.ArrowPoints.Count >= 1)
                    {
                        // ручки удаления стрелки с insertionPoint
                        grips.Add(new CrestedLeaderArrowRemoveGrip(crestedLeader, 4, (BlockReference)entity)
                        {
                            GripPoint = crestedLeader.InsertionPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        });

                        var normal = (crestedLeader.EndPoint - crestedLeader.InsertionPoint).GetNormal();
                        for (var i = 0; i < crestedLeader.ArrowPoints.Count; i++)
                        {
                            var gripPoint = crestedLeader.ArrowPoints[i] ;

                            // ручки переноса стрелки
                            grips.Add(new CrestedLeaderArrowMoveGrip(crestedLeader, i, (BlockReference)entity)
                            {
                                GripPoint = gripPoint
                            });

                            // ручки удаления выносок
                            grips.Add(new CrestedLeaderArrowRemoveGrip(crestedLeader, i + 5, (BlockReference)entity)
                            {
                                GripPoint = gripPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                            });
                        }
                    }

                    var textIndent = crestedLeader.TextIndent;
                    var shelfLength = crestedLeader.ShelfLength;

                    if (crestedLeader.ShelfPosition == ShelfPosition.Left)
                    {
                        textIndent = -textIndent;
                        shelfLength = -shelfLength;
                    }

                    if (crestedLeader.ScaleFactorX < 0)
                    {
                        textIndent = -textIndent;
                        shelfLength = -shelfLength;
                    }

                    var arrowTypeGripPoint = crestedLeader.EndPoint + (Vector3d.XAxis * shelfLength);
                    var alignGripPoint = arrowTypeGripPoint + (Vector3d.YAxis *
                                                                (crestedLeader.MainTextHeight + crestedLeader.TextVerticalOffset) * crestedLeader.GetFullScale());
                    var shelfMoveGripPoint = crestedLeader.EndPoint + (Vector3d.XAxis * textIndent);
                    var shelfPositionGripPoint = crestedLeader.EndPoint +
                                                 (Vector3d.YAxis *
                                                 (crestedLeader.MainTextHeight + crestedLeader.TextVerticalOffset));

                    if (crestedLeader.IsRotated & !crestedLeader.IsTextAlwaysHorizontal)
                    {
                        arrowTypeGripPoint = arrowTypeGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        alignGripPoint = alignGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        shelfMoveGripPoint = shelfMoveGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                        shelfPositionGripPoint = shelfPositionGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, crestedLeader.EndPoint);
                    }

                    // Получаем ручку зеркалирования полки
                    var gp1 = new CrestedLeaderShelfPositionGrip(crestedLeader)
                    {
                        GripPoint = shelfPositionGripPoint,
                        GripType = GripType.TwoArrowsLeftRight
                    };
                    grips.Add(gp1);

                    // Получаем ручку изменения полки
                    grips.Add(new CrestedLeaderShelfMoveGrip(crestedLeader, 2)
                    {
                        GripPoint = shelfMoveGripPoint
                    });

                    // ручки выбора типа стрелки
                    grips.Add(new CrestedLeaderArrowEndTypeGrip(crestedLeader, 3)
                    {
                        GripPoint = arrowTypeGripPoint
                    });

                    if ((string.IsNullOrEmpty(crestedLeader.LeaderTextValue) | string.IsNullOrEmpty(crestedLeader.LeaderTextComment)) 
                        | (string.IsNullOrEmpty(crestedLeader.LeaderTextValue) & string.IsNullOrEmpty(crestedLeader.LeaderTextComment)))
                        return;

                    grips.Add(new EntityTextAlignGrip(
                        crestedLeader,
                        () => crestedLeader.ValueHorizontalAlignment,
                        setAlignEntity => crestedLeader.ValueHorizontalAlignment = setAlignEntity)
                    {
                        GripPoint = alignGripPoint
                    });
                }
            }
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }

    /// <inheritdoc/>
    public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is CrestedLeaderVertexGrip vertexGrip)
                    {
                        var chainLeader = vertexGrip.CrestedLeader;

                        if (vertexGrip.GripIndex == 0)
                        {
                            var newPoint = vertexGrip.GripPoint + offset;
                            var normal = (chainLeader.EndPoint - chainLeader.InsertionPoint).GetNormal();
                            
                            //var pointOnPolyline = GetPointOnMainLeader(chainLeader.InsertionPoint, chainLeader.EndPoint, newPoint);

                            //if (pointOnPolyline.DistanceTo(chainLeader.EndPoint) <= chainLeader.MinDistanceBetweenPoints)
                            //{
                            //    pointOnPolyline = chainLeader.EndPoint + ((chainLeader.EndPoint - chainLeader.InsertionPoint) * chainLeader.MinDistanceBetweenPoints);
                            //}

                            //chainLeader.IsLeft = IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);

                            ((BlockReference)entity).Position = newPoint;
                            chainLeader.InsertionPoint = newPoint;
                        }
                        else if (vertexGrip.GripIndex == 1)
                        {
                            chainLeader.EndPoint = vertexGrip.GripPoint + offset;
                            var distInspointToEndPoint = chainLeader.EndPoint.DistanceTo(chainLeader.InsertionPoint);

                            if (distInspointToEndPoint <= chainLeader.MinDistanceBetweenPoints)
                            {
                                chainLeader.EndPoint += (chainLeader.EndPoint - chainLeader.InsertionPoint).GetNormal() * chainLeader.MinDistanceBetweenPoints;
                            }

                            //vertexGrip.TempPoint3ds = new List<Point3d>(chainLeader.ArrowPoints);
                            //chainLeader.ArrowPoints.Clear();
                            //foreach (var distance in _distArrowPointsFromInsPoint)
                            //{
                            //    if (distance < distInspointToEndPoint)
                            //    {
                            //        chainLeader.ArrowPoints.Add(-(distInspointToEndPoint - distance));
                            //    }
                            //    else
                            //    {
                            //        chainLeader.ArrowPoints.Add(distance - distInspointToEndPoint);
                            //    }
                            //}
                        }

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var chainLeader = shelfMoveGrip.CrestedLeader;
                        if (chainLeader.ShelfPosition == ShelfPosition.Right)
                        {
                            chainLeader.TextIndent = shelfMoveGrip.GripPoint.X - chainLeader.EndPoint.X + offset.X;
                        }
                        else
                        {
                            chainLeader.TextIndent = chainLeader.EndPoint.X - shelfMoveGrip.GripPoint.X - offset.X;
                        }

                        shelfMoveGrip.NewPoint = chainLeader.TextIndent;
                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderArrowAddGrip addLeaderGrip)
                    {
                        var crestedLeader = addLeaderGrip.CrestedLeader;
                        var newPoint = addLeaderGrip.GripPoint + offset;

                        //var pointOnPolyline = GetPerpendicularPoint(
                        //    chainLeader.InsertionPoint,
                        //    chainLeader.EndPoint,
                        //    newPoint);
                        //var mainNormal = (crestedLeader.EndPoint - crestedLeader.InsertionPoint).GetNormal();
                        //var secondNormal = (crestedLeader.LeaderPoint - crestedLeader.EndPoint).GetNormal();

                        //var mainAngle = secondNormal.GetAngleTo(mainNormal, Vector3d.ZAxis);
                        //var mainLine = new Line(crestedLeader.EndPoint, crestedLeader.LeaderPoint);
                        //var pointOnPolyline = GetPointOnMainLeader(newPoint, mainAngle, secondNormal, mainLine);
                        //addLeaderGrip.IsOnsegment = IsPointBetween(
                        //    pointOnPolyline, 
                        //    crestedLeader.InsertionPoint,
                        //    crestedLeader.EndPoint);
                        crestedLeader.TempNewArrowPoint = newPoint;

                        crestedLeader.UpdateEntities();
                        crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is CrestedLeaderArrowMoveGrip moveLeaderGrip)
                    {
                        var crestedLeader = moveLeaderGrip.CrestedLeader;
                        var newPoint = moveLeaderGrip.GripPoint + offset;
                        //var pointOnPolyline = GetPerpendicularPoint(
                        //    chainLeader.InsertionPoint,
                        //    chainLeader.EndPoint, 
                        //    newPoint);
                        //moveLeaderGrip.IsOnsegment = IsPointBetween(
                        //    pointOnPolyline, 
                        //    chainLeader.InsertionPoint,
                        //    chainLeader.EndPoint);
                        crestedLeader.TempNewArrowPoint = newPoint;

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

    //private double SetChainLeaderTempNewArrowPoint(CrestedLeader chainLeader, Point3d pointOnPolyline)
    //{
    //    chainLeader.IsLeft = IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);
    //    var isOnSegment = IsPointBetween(pointOnPolyline, chainLeader.FirstPoint,
    //        chainLeader.SecondPoint);

    //    if (!isOnSegment)
    //    {
    //        if (!chainLeader.IsLeft)
    //        {
    //            chainLeader.TempNewArrowPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);
    //        }
    //        else
    //        {
    //            chainLeader.TempNewArrowPoint = -1 * chainLeader.EndPoint.DistanceTo(pointOnPolyline);
    //        }
    //    }

    //    return chainLeader.TempNewArrowPoint;
    //}

    //private Point3d GetPerpendicularPoint(Point3d varStart, Point3d varEnd, Point3d varBase)
    //{
    //    var a = varStart;
    //    var b = varEnd;
    //    var c = varBase;

    //    var f0 = c.X - (b.Y - a.Y);
    //    var f1 = c.Y + (b.X - a.X);
    //    var k2 = (((c.X - a.X) * (b.Y - a.Y)) - ((b.X - a.X) * (c.Y - a.Y))) / (((b.X - a.X) * (f1 - c.Y)) - ((f0 - c.X) * (b.Y - a.Y)));
    //    var xPoint = ((f0 - c.X) * k2) + c.X;
    //    var yPoint = ((f1 - c.Y) * k2) + c.Y;

    //    return new Point3d(xPoint, yPoint, 0);
    //}

    //private Point3d GetPointOnMainLeader(Point3d newPoint, double angle, Vector3d normal, Line mainLine)
    //{
    //    var katetPoint = mainLine.GetClosestPointTo(newPoint, true);
    //    var katetA = newPoint.DistanceTo(katetPoint);
    //    var b = katetA * Math.Tan(angle);

    //    return katetPoint + normal * b;
    //}

    //private bool IsLeft(Point3d insertionPoint, Point3d endPoint, Point3d pointOnLine)
    //{
    //    var v1 = (insertionPoint - endPoint).GetNormal();
    //    var v2 = (pointOnLine - endPoint).GetNormal();

    //    return v1.DotProduct(v2) > 0;
    //}

    //private bool IsPointBetween(Point3d point, Point3d startPt, Point3d endPt)
    //{
    //    var segment = new LineSegment3d(startPt, endPt);
    //    return segment.IsOn(point);
    //}
}