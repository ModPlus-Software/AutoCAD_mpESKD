using mpESKD.Base.Enums;
using mpESKD.Base.Overrules.Grips;

namespace mpESKD.Functions.mpNodalLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using System;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class NodalLeaderGripPointOverrule : BaseSmartEntityGripOverrule<NodalLeader>
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
                var nodalLeader = EntityReaderService.Instance.GetFromEntity<NodalLeader>(entity);

                // Паранойя программиста =)
                if (nodalLeader != null)
                {
                    // Получаем первую ручку (совпадает с точкой вставки блока)
                    var gp = new NodalLeaderGrip(nodalLeader, GripName.InsertionPoint)
                    {
                        GripPoint = nodalLeader.InsertionPoint
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new NodalLeaderGrip(nodalLeader, GripName.FramePoint)
                    {
                        GripPoint = nodalLeader.EndPoint
                    };
                    grips.Add(gp);

                    // получаем ручку типа рамки
                    grips.Add(new NodalFrameTypeGrip(nodalLeader)
                    {
                        GripPoint = new Point3d(
                                (nodalLeader.EndPoint.X - nodalLeader.InsertionPoint.X) * -1 + nodalLeader.InsertionPoint.X, 
                            (nodalLeader.EndPoint.Y-nodalLeader.InsertionPoint.Y)*-1 + nodalLeader.InsertionPoint.Y, 
                            nodalLeader.EndPoint.Z)
                    });

                    // получаем ручку выноски
                    if (!(!string.IsNullOrEmpty(nodalLeader.NodeNumber) |
                          !string.IsNullOrEmpty(nodalLeader.SheetNumber)))
                        return;
                    gp = new NodalLeaderGrip(nodalLeader, GripName.LeaderPoint)
                    {
                        GripPoint = nodalLeader.LeaderPoint
                    };
                    grips.Add(gp);

                    var shelfLength = nodalLeader.TopShelfLineLength;
                    
                    if (nodalLeader.ShelfPosition == ShelfPosition.Left)
                    {
                        shelfLength = -shelfLength;
                    }

                    if (nodalLeader.ScaleFactorX < 0)
                    {
                        shelfLength = -shelfLength;
                    }

                    var shelfPointGrip = nodalLeader.LeaderPoint +
                                         (Vector3d.YAxis *
                                          ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) *
                                           nodalLeader.GetFullScale()));
                    var alignGripPoint = shelfPointGrip + Vector3d.XAxis * shelfLength;
                    if (nodalLeader.IsRotated & !nodalLeader.IsTextAlwaysHorizontal)
                    {
                        shelfPointGrip = shelfPointGrip.RotateBy(nodalLeader.Rotation, Vector3d.ZAxis, nodalLeader.LeaderPoint);
                        alignGripPoint = alignGripPoint.RotateBy(nodalLeader.Rotation, Vector3d.ZAxis, nodalLeader.LeaderPoint);
                    }

                    grips.Add(new NodalLevelShelfPositionGrip(nodalLeader)
                    {
                        GripPoint = shelfPointGrip
                    });

                    grips.Add(new EntityTextAlignGrip(nodalLeader,
                        () => nodalLeader.ValueHorizontalAlignment,
                        (setAlignEntity) => nodalLeader.ValueHorizontalAlignment = setAlignEntity)
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
                    if (gripData is NodalLeaderGrip levelMarkGrip)
                    {
                        var gripPoint = levelMarkGrip.GripPoint;
                        var nodalLeader = levelMarkGrip.NodalLeader;
                        var scale = nodalLeader.GetFullScale();

                        if (levelMarkGrip.GripName == GripName.InsertionPoint)
                        {
                            ((BlockReference)entity).Position = gripPoint + offset;
                        }
                        else if (levelMarkGrip.GripName == GripName.FramePoint)
                        {
                            if (nodalLeader.FrameType == FrameType.Rectangular)
                            {
                                var currentPosition = gripPoint + offset;
                                var frameHeight =
                                    Math.Abs(currentPosition.Y - nodalLeader.InsertionPoint.Y) / scale;
                                var frameWidth = Math.Abs(currentPosition.X - nodalLeader.InsertionPoint.X) / scale;

                                if (!(frameHeight <= nodalLeader.MinDistanceBetweenPoints) &&
                                    !(frameWidth <= nodalLeader.MinDistanceBetweenPoints))
                                {
                                    nodalLeader.EndPoint = gripPoint + offset;
                                }
                            }
                            else
                            {
                                nodalLeader.EndPoint = gripPoint + offset;
                            }
                        }
                        else if (levelMarkGrip.GripName == GripName.LeaderPoint)
                        {
                            nodalLeader.LeaderPoint = gripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        nodalLeader.UpdateEntities();
                        nodalLeader.BlockRecord.UpdateAnonymousBlocks();
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