namespace mpESKD.Functions.mpChainLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using System;
using System.Collections.Generic;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class ChainLeaderGripPointOverrule : BaseSmartEntityGripOverrule<ChainLeader>
{
    private List<double> _distArrowPointsFromInsPoint = new();
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
                var chainLeader = EntityReaderService.Instance.GetFromEntity<ChainLeader>(entity);

                if (chainLeader != null)
                {
                    // Получаем ручку на первой точке
                    var gp = new ChainLeaderVertexGrip(chainLeader, 0, (BlockReference)entity)
                    {
                        GripPoint = chainLeader.InsertionPoint
                    };
                    grips.Add(gp);

                    // Получаем ручку на второй точке
                    gp = new ChainLeaderVertexGrip(chainLeader, 1, (BlockReference)entity)
                    {
                        GripPoint = chainLeader.EndPoint
                    };
                    grips.Add(gp);
                    _distArrowPointsFromInsPoint.Clear();
                    var distFromEndPointToInsPoint = chainLeader.EndPoint.DistanceTo(chainLeader.InsertionPoint);
                    foreach (var distance in chainLeader.ArrowPoints)
                    {
                        if (distance < 0)
                        {
                            _distArrowPointsFromInsPoint.Add(distFromEndPointToInsPoint - Math.Abs(distance));
                        }
                        else
                        {
                            _distArrowPointsFromInsPoint.Add(distFromEndPointToInsPoint + distance);
                        }
                    }

                    // Получаем ручку зеркалирования полки
                    var gp1 = new ChainLeaderShelfPositionGrip(chainLeader)
                    {
                        GripPoint = chainLeader.EndPoint +
                                    (Vector3d.YAxis * ((chainLeader.MainTextHeight + chainLeader.TextVerticalOffset) * chainLeader.GetFullScale())),
                        GripType = GripType.TwoArrowsLeftRight
                    };
                    grips.Add(gp1);

                    // Получаем ручку изменения полки
                    if (chainLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        grips.Add(new ChainLeaderShelfMoveGrip(chainLeader, 2)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X + chainLeader.TextIndent,
                                chainLeader.EndPoint.Y,
                                chainLeader.EndPoint.Z)
                        });

                        // ручки выбора типа стрелки
                        grips.Add(new ChainLeaderArrowEndTypeGrip(chainLeader, 3)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X + chainLeader.ShelfLength,
                                chainLeader.EndPoint.Y,
                                chainLeader.EndPoint.Z)
                        });
                    }
                    else
                    {
                        grips.Add(new ChainLeaderShelfMoveGrip(chainLeader, 2)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X - chainLeader.TextIndent,
                                chainLeader.EndPoint.Y,
                                chainLeader.EndPoint.Z)
                        });

                        // ручки выбора типа стрелки
                        grips.Add(new ChainLeaderArrowEndTypeGrip(chainLeader, 3)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X - chainLeader.ShelfLength,
                                chainLeader.EndPoint.Y,
                                chainLeader.EndPoint.Z)
                        });
                    }

                    // получаем ручку для создания стрелки
                    grips.Add(new ChainLeaderArrowAddGrip(chainLeader, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(
                        chainLeader.InsertionPoint.X - 3 * chainLeader.GetFullScale(),
                        chainLeader.InsertionPoint.Y,
                        chainLeader.InsertionPoint.Z) - (Vector3d.YAxis * curViewUnitSize)
                    });

                    if (chainLeader.ArrowPoints.Count >= 1)
                    {
                        // ручки удаления стрелки с insertionPoint
                        grips.Add(new ChainLeaderArrowRemoveGrip(chainLeader, 4, (BlockReference)entity)
                        {
                            GripPoint = new Point3d(
                                chainLeader.InsertionPoint.X + 3 * chainLeader.GetFullScale(),
                                chainLeader.InsertionPoint.Y,
                                0) - (Vector3d.YAxis * curViewUnitSize)
                        });

                        var normal = (chainLeader.EndPoint - chainLeader.InsertionPoint).GetNormal();
                        for (var i = 0; i < chainLeader.ArrowPoints.Count; i++)
                        {
                            // ручки переноса стрелки
                            grips.Add(new ChainLeaderArrowMoveGrip(chainLeader, i, (BlockReference)entity)
                            {
                                GripPoint = chainLeader.EndPoint + chainLeader.ArrowPoints[i] * normal
                            });

                            var gripPoint = chainLeader.EndPoint + chainLeader.ArrowPoints[i] * normal;
                            var deleteGripPoint = new Point3d(gripPoint.X + 2 * chainLeader.GetFullScale(), gripPoint.Y, gripPoint.Z);

                            // ручки удаления выносок
                            grips.Add(new ChainLeaderArrowRemoveGrip(chainLeader, i, (BlockReference)entity)
                            {
                                GripPoint = deleteGripPoint - (Vector3d.YAxis * curViewUnitSize)
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

    // <inheritdoc/>
    public override void MoveGripPointsAt(
        Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    {
        try
        {
            if (IsApplicable(entity))
            {
                foreach (var gripData in grips)
                {
                    if (gripData is ChainLeaderVertexGrip vertexGrip)
                    {
                        var chainLeader = vertexGrip.ChainLeader;

                        if (vertexGrip.GripIndex == 0)
                        {
                            var newPoint = vertexGrip.GripPoint + offset;

                            var pointOnPolyline = GetPerpendicularPoint(chainLeader.InsertionPoint,
                                chainLeader.EndPoint, newPoint);

                            if (pointOnPolyline.DistanceTo(chainLeader.EndPoint) <= chainLeader.MinDistanceBetweenPoints)
                            {
                                pointOnPolyline = chainLeader.EndPoint + ((chainLeader.EndPoint - chainLeader.InsertionPoint) * chainLeader.MinDistanceBetweenPoints);
                            }

                            chainLeader.IsLeft = IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);

                            ((BlockReference)entity).Position = pointOnPolyline;
                            chainLeader.InsertionPoint = pointOnPolyline;
                        }
                        else if (vertexGrip.GripIndex == 1)
                        {
                            chainLeader.EndPoint = vertexGrip.GripPoint + offset;
                            var distInspointToEndPoint = chainLeader.EndPoint.DistanceTo(chainLeader.InsertionPoint);

                            if (distInspointToEndPoint <= chainLeader.MinDistanceBetweenPoints)
                            {
                                chainLeader.EndPoint += (chainLeader.MainNormal * chainLeader.MinDistanceBetweenPoints);
                            }

                            vertexGrip.TempPoint3ds = new List<double>(chainLeader.ArrowPoints);
                            chainLeader.ArrowPoints.Clear();
                            foreach (var distance in _distArrowPointsFromInsPoint)
                            {
                                if (distance < distInspointToEndPoint)
                                {
                                    chainLeader.ArrowPoints.Add(-(distInspointToEndPoint - distance));
                                }
                                else
                                {
                                    chainLeader.ArrowPoints.Add((distance - distInspointToEndPoint));
                                }
                            }
                        }

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var chainLeader = shelfMoveGrip.ChainLeader;
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
                    else if (gripData is ChainLeaderArrowAddGrip addLeaderGrip)
                    {
                        var chainLeader = addLeaderGrip.ChainLeader;
                        var newPoint = addLeaderGrip.GripPoint + offset;

                        var pointOnPolyline = GetPerpendicularPoint(chainLeader.InsertionPoint,
                            chainLeader.EndPoint, newPoint);

                        addLeaderGrip.IsOnsegment = IsPointBetween(pointOnPolyline, chainLeader.InsertionPoint,
                            chainLeader.EndPoint);
                        chainLeader.TempNewArrowPoint = SetChainLeaderTempNewArrowPoint(chainLeader, pointOnPolyline);

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderArrowMoveGrip moveLeaderGrip)
                    {
                        var chainLeader = moveLeaderGrip.ChainLeader;
                        var pointOnPolyline = moveLeaderGrip.GripPoint + offset;

                        chainLeader.TempNewArrowPoint = SetChainLeaderTempNewArrowPoint(chainLeader, pointOnPolyline);

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
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

    private double SetChainLeaderTempNewArrowPoint(ChainLeader chainLeader, Point3d pointOnPolyline)
    {
        chainLeader.IsLeft = IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);

        var isOnSegment = IsPointBetween(pointOnPolyline, chainLeader.FirstPoint,
            chainLeader.SecondPoint);

        if (!isOnSegment)
        {
            if (!chainLeader.IsLeft)
            {
                chainLeader.TempNewArrowPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);
            }
            else
            {
                chainLeader.TempNewArrowPoint = -1 * chainLeader.EndPoint.DistanceTo(pointOnPolyline);
            }
        }

        return chainLeader.TempNewArrowPoint;
    }

    private Point3d GetPerpendicularPoint(Point3d varStart, Point3d varEnd, Point3d varBase)
    {
        var a = varStart;
        var b = varEnd;
        var c = varBase;

        var f0 = c.X - (b.Y - a.Y);
        var f1 = c.Y + (b.X - a.X);
        var k2 = (((c.X - a.X) * (b.Y - a.Y)) - ((b.X - a.X) * (c.Y - a.Y))) / (((b.X - a.X) * (f1 - c.Y)) - ((f0 - c.X) * (b.Y - a.Y)));
        var xPoint = ((f0 - c.X) * k2) + c.X;
        var yPoint = ((f1 - c.Y) * k2) + c.Y;

        return new Point3d(xPoint, yPoint, 0);
    }

    private bool IsLeft(Point3d insertionPoint, Point3d endPoint, Point3d pointOnLine)
    {
        var v1 = (insertionPoint - endPoint).GetNormal();
        var v2 = (pointOnLine - endPoint).GetNormal();

        return v1.DotProduct(v2) > 0;
    }

    private bool IsPointBetween(Point3d point, Point3d startPt, Point3d endPt)
    {
        var segment = new LineSegment3d(startPt, endPt);
        return segment.IsOn(point);
    }
}