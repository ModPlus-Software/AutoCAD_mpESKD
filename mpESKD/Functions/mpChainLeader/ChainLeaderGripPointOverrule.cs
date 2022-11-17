﻿using System;
using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpChainLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;

public class ChainLeaderGripPointOverrule : BaseSmartEntityGripOverrule<ChainLeader>
{
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
                    // Получаем первую ручку на первой точке
                    var gp = new ChainLeaderVertexGrip(chainLeader, 0)
                    {
                        GripPoint = chainLeader.InsertionPoint
                    };
                    grips.Add(gp);

                    // Получаем первую ручку на первой точке
                    gp = new ChainLeaderVertexGrip(chainLeader, 1)
                    {
                        GripPoint = chainLeader.EndPoint
                    };
                    grips.Add(gp);

                    // получаем ручку для создания выноски
                    grips.Add(new ChainLeaderAddLeaderGrip(chainLeader)
                    {
                        GripPoint = new Point3d(
                            chainLeader.EndPoint.X - 2,
                            chainLeader.EndPoint.Y,
                            chainLeader.EndPoint.Z)
                    });
                    var normal = (chainLeader.EndPoint - chainLeader.InsertionPoint).GetNormal();
                    for (var i = 0; i < chainLeader.ArrowPoints.Count; i++)
                    {
                        // ручки переноса выносок
                        grips.Add(new ChainLeaderMoveGrip(chainLeader, i)
                        {
                            GripPoint = chainLeader.EndPoint + (chainLeader.ArrowPoints[i] * normal)
                        });
                        var gripPoint = chainLeader.EndPoint + (chainLeader.ArrowPoints[i] * normal);
                        var deleteGripPoint = new Point3d(
                            gripPoint.X + 1,
                            gripPoint.Y,
                            gripPoint.Z);
                        var leaderEndTypeGripPoint = new Point3d(
                            gripPoint.X - 1,
                            gripPoint.Y,
                            gripPoint.Z);

                        // ручки удаления выносок
                        grips.Add(new ChainLeaderRemoveGrip(chainLeader, i)
                        {
                            GripPoint = deleteGripPoint
                        });

                        // ручки выбора типа выносок
                        grips.Add(new ChainLeaderEndTypeGrip(chainLeader, i)
                        {
                            GripPoint = leaderEndTypeGripPoint
                        });
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
                            ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                            chainLeader.InsertionPoint = vertexGrip.GripPoint + offset;
                        }

                        if (vertexGrip.GripIndex == 1)
                        {
                            chainLeader.EndPoint = vertexGrip.GripPoint + offset;
                        }

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderAddLeaderGrip addLeaderGrip)
                    {
                        var chainLeader = addLeaderGrip.ChainLeader;
                        var newPoint = addLeaderGrip.GripPoint + offset;

                        var pointOnPolyline = GetPerpendicularPoint(chainLeader.InsertionPoint,
                            chainLeader.EndPoint, newPoint);

                        AcadUtils.WriteMessageInDebug($"insertionPointOCS {chainLeader.InsertionPointOCS} - leaderPointOCS {chainLeader.EndPointOCS}");
                        AcadUtils.WriteMessageInDebug($"insertionPoint {chainLeader.InsertionPoint} - leaderPoint {chainLeader.EndPoint}");
                        AcadUtils.WriteMessageInDebug($" pointOnPolyline.X {pointOnPolyline.X} - pointOnPolyline.Y {pointOnPolyline.Y}");
                        //var pointOnMainLine = chainLeader.MainLine.GetClosestPointTo(newPoint, false);
                        //var newplpoint = new Point3d(-14970, 2230, 0);

                        addLeaderGrip.NewPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);


                    }

                    else if (gripData is ChainLeaderMoveGrip moveLeaderGrip)
                    {
                        var chainLeader = moveLeaderGrip.ChainLeader;
                        moveLeaderGrip.NewPoint = chainLeader.EndPoint.DistanceTo(moveLeaderGrip.GripPoint + offset);
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

    public Point3d GetPerpendicularPoint(Point3d varStart, Point3d varEnd, Point3d varBase)
    {
        Point3d a = varStart;
        Point3d b = varEnd;
        Point3d c = varBase;

        var F0 = c.X - (b.Y - a.Y);
        var F1 = c.Y + (b.X - a.X);
        var k2 = ((c.X - a.X) * (b.Y - a.Y) - (b.X - a.X) * (c.Y - a.Y)) /
                 (double)((b.X - a.X) * (F1 - c.Y) - (F0 - c.X) * (b.Y - a.Y));
        var xPoint = (F0 - c.X) * k2 + c.X;
        var yPoint = (F1 - c.Y) * k2 + c.Y;

        return new Point3d(xPoint, yPoint, 0);
    }

}