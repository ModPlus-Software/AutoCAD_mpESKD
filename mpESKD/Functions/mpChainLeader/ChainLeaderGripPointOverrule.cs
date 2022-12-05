namespace mpESKD.Functions.mpChainLeader;

using mpESKD.Base.Utils;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

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
                    // Получаем ручку на первой точке
                    var gp = new ChainLeaderVertexGrip(chainLeader, 0)
                    {
                        GripPoint = chainLeader.InsertionPoint
                    };
                    grips.Add(gp);

                    // Получаем ручку на второй точке
                    gp = new ChainLeaderVertexGrip(chainLeader, 1)
                    {
                        GripPoint = chainLeader.EndPoint
                    };
                    grips.Add(gp);

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
                        grips.Add(new ChainLeaderShelfMoveGrip(chainLeader,2)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X + chainLeader.ShelfLedge,
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
                        grips.Add(new ChainLeaderShelfMoveGrip(chainLeader,2)
                        {
                            GripPoint = new Point3d(
                                chainLeader.EndPoint.X - chainLeader.ShelfLedge,
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
                    grips.Add(new ChainLeaderAddArrowGrip(chainLeader, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(
                        chainLeader.InsertionPoint.X - 2,
                        chainLeader.InsertionPoint.Y,
                        chainLeader.InsertionPoint.Z)
                    });

                    AcadUtils.WriteMessageInDebug($"chainLeader.ShelfLength-{chainLeader.ShelfLedge}");
                    
                    if (chainLeader.ArrowPoints.Count >= 1)
                    {
                        // ручки удаления стрелки с insertionPoint
                        grips.Add(new ChainLeaderArrowRemoveGrip(chainLeader, 4, (BlockReference)entity)
                        {

                            GripPoint = new Point3d(
                                chainLeader.InsertionPoint.X + 1,
                                chainLeader.InsertionPoint.Y,
                                0)
                        });

                        var normal = (chainLeader.EndPoint - chainLeader.InsertionPoint).GetNormal();
                        for (var i = 0; i < chainLeader.ArrowPoints.Count; i++)
                        {
                            // ручки переноса стрелки
                            grips.Add(new ChainLeaderArrowMoveGrip(chainLeader, i, (BlockReference)entity)
                            {
                                GripPoint = chainLeader.EndPoint + (chainLeader.ArrowPoints[i] * normal)
                            });

                            var gripPoint = chainLeader.EndPoint + (chainLeader.ArrowPoints[i] * normal);
                            var deleteGripPoint = new Point3d(gripPoint.X + 1, gripPoint.Y, gripPoint.Z);

                            // ручки удаления выносок
                            grips.Add(new ChainLeaderArrowRemoveGrip(chainLeader, i, (BlockReference)entity)
                            {
                                GripPoint = deleteGripPoint
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
                            ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                            chainLeader.InsertionPoint = vertexGrip.GripPoint + offset;
                        }
                        else if (vertexGrip.GripIndex == 1)
                        {
                            chainLeader.EndPoint = vertexGrip.GripPoint + offset;
                        }

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderShelfMoveGrip shelfMoveGrip)
                    {
                        var chainLeader = shelfMoveGrip.ChainLeader;
                        chainLeader.ShelfLedge = shelfMoveGrip.GripPoint.X - chainLeader.EndPoint.X + offset.X;
                        shelfMoveGrip.NewPoint = chainLeader.ShelfLedge;
                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderAddArrowGrip addLeaderGrip)
                    {
                        var chainLeader = addLeaderGrip.ChainLeader;
                        var newPoint = addLeaderGrip.GripPoint + offset;

                        var pointOnPolyline = GetPerpendicularPoint(chainLeader.InsertionPoint,
                            chainLeader.EndPoint, newPoint);

                        var isleft = AcadUtils.IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);

                        var isOnSegment = AcadUtils.IsPointBetween(pointOnPolyline, chainLeader.FirstPoint,
                            chainLeader.SecondPoint);
                        AcadUtils.WriteMessageInDebug($"isOnSegment {isOnSegment} \n");
                        if (!isOnSegment)
                        {
                            if (!isleft)
                            {
                                chainLeader.TempNewArrowPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                
                                chainLeader.IsLeft = false;
                            }
                            else
                            {
                                chainLeader.TempNewArrowPoint = -1 * chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                
                                chainLeader.IsLeft = true;
                            }

                            //addLeaderGrip.NewPoint = chainLeader.TempNewArrowPoint;
                        }

                        chainLeader.UpdateEntities();
                        chainLeader.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else if (gripData is ChainLeaderArrowMoveGrip moveLeaderGrip)
                    {
                        var chainLeader = moveLeaderGrip.ChainLeader;
                        var pointOnPolyline = moveLeaderGrip.GripPoint + offset;
                        var isleft = AcadUtils.IsLeft(chainLeader.InsertionPoint, chainLeader.EndPoint, pointOnPolyline);

                        var isOnSegment = AcadUtils.IsPointBetween(pointOnPolyline, chainLeader.FirstPoint,
                            chainLeader.SecondPoint);

                        if (!isOnSegment)
                        {
                            if (!isleft)
                            {
                                chainLeader.TempNewArrowPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                moveLeaderGrip.NewPoint = chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                chainLeader.IsLeft = false;
                            }
                            else
                            {
                                chainLeader.TempNewArrowPoint = -1 * chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                moveLeaderGrip.NewPoint = -1 * chainLeader.EndPoint.DistanceTo(pointOnPolyline);
                                chainLeader.IsLeft = true;
                            }
                        }

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