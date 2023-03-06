namespace mpESKD.Functions.mpCrestedLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Overrules.Grips;
using System;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class CrestedLeaderGripPointOverrule : BaseSmartEntityGripOverrule<CrestedLeader>
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
                var crestedLeader = EntityReaderService.Instance.GetFromEntity<CrestedLeader>(entity);

                if (crestedLeader == null)
                    return;
                if (crestedLeader.ArrowPoints.Count == 1)
                {
                    //// Получаем ручку на первой точке
                    var gp = new CrestedLeaderVertexGrip(crestedLeader, GripName.VertexGrip, (BlockReference)entity)
                    {
                        GripPoint = crestedLeader.InsertionPoint
                    };
                    grips.Add(gp);
                }

                var shelfStretchPoint = crestedLeader.InsertionPoint.X + Math.Abs(crestedLeader.InsertionPoint.X - crestedLeader.EndPoint.X) / 2;
                if (!crestedLeader.IsRight)
                {
                    shelfStretchPoint = crestedLeader.InsertionPoint.X - Math.Abs(crestedLeader.InsertionPoint.X - crestedLeader.EndPoint.X) / 2;
                }

                // Получаем ручку по середине
                if (crestedLeader.ArrowPoints.Count > 1)
                {
                    var gp = new CrestedLeaderStretchGrip(crestedLeader, GripName.Stretch, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(shelfStretchPoint, crestedLeader.InsertionPoint.Y, 0)
                    };
                    grips.Add(gp);
                }

                if (crestedLeader.ArrowPoints.Count >= 1)
                {
                    for (var i = 0; i < crestedLeader.ArrowPoints.Count; i++)
                    {
                        var gripPoint = crestedLeader.ArrowPoints[i];

                        // ручки переноса стрелки
                        grips.Add(new CrestedLeaderArrowMoveGrip(crestedLeader, i, (BlockReference)entity)
                        {
                            GripPoint = gripPoint
                        });

                        if (crestedLeader.ArrowPoints.Count > 1)
                        {
                            // ручки удаления выносок
                            grips.Add(new CrestedLeaderArrowRemoveGrip(crestedLeader, i + 5, (BlockReference)entity)
                            {
                                GripPoint = gripPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                            });
                        }
                    }
                }

                // получаем ручку для создания стрелки
                if (crestedLeader.IsRight)
                {
                    grips.Add(new CrestedLeaderArrowAddGrip(crestedLeader, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(crestedLeader.InsertionPoint.X, crestedLeader.InsertionPoint.Y, 0) - Vector3d.XAxis * 20 * curViewUnitSize
                    });
                }
                else
                {
                    grips.Add(new CrestedLeaderArrowAddGrip(crestedLeader, (BlockReference)entity)
                    {
                        GripPoint = new Point3d(crestedLeader.InsertionPoint.X, crestedLeader.InsertionPoint.Y, 0) + Vector3d.XAxis * 20 * curViewUnitSize
                    });
                }

                var shelfLength = crestedLeader.ShelfLength;

                if (crestedLeader.ScaleFactorX < 0)
                {
                    shelfLength = -shelfLength;
                }

                var endPointByX = new Point3d(crestedLeader.EndPoint.X, crestedLeader.InsertionPoint.Y, 0);

                var arrowTypeGripPoint = endPointByX + (Vector3d.XAxis * shelfLength);
                var shelfMoveGripPoint = endPointByX + (Vector3d.XAxis);

                if (!crestedLeader.IsRight)
                {
                    arrowTypeGripPoint = endPointByX - (Vector3d.XAxis * shelfLength);
                    shelfMoveGripPoint = endPointByX - (Vector3d.XAxis);
                }


                var alignGripPoint = arrowTypeGripPoint + (Vector3d.YAxis *
                                                           (crestedLeader.MainTextHeight + crestedLeader.TextVerticalOffset) * crestedLeader.GetFullScale());
                if (crestedLeader.IsRotated & !crestedLeader.IsTextAlwaysHorizontal)
                {
                    arrowTypeGripPoint = arrowTypeGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, endPointByX);
                    alignGripPoint = alignGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, endPointByX);
                    shelfMoveGripPoint = shelfMoveGripPoint.RotateBy(crestedLeader.Rotation, Vector3d.ZAxis, endPointByX);
                }

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
                    switch (gripData)
                    {
                        case CrestedLeaderVertexGrip vertexGrip:
                            {
                                var crestedLeader = vertexGrip.CrestedLeader;
                                var newPoint = vertexGrip.GripPoint + offset;
                                vertexGrip.NewPoint = offset.Y;

                                crestedLeader.TempNewStretchPoint = newPoint;
                                crestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                                break;
                            }

                        case CrestedLeaderStretchGrip stretchGrip:
                            {
                                var crestedLeader = stretchGrip.CrestedLeader;
                                var newPoint = stretchGrip.GripPoint + offset;
                                stretchGrip.NewPoint = offset.Y;

                                crestedLeader.TempNewStretchPoint = newPoint;
                                crestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                                break;
                            }

                        case CrestedLeaderShelfMoveGrip shelfMoveGrip:
                            {
                                var crestedLeader = shelfMoveGrip.CrestedLeader;
                                crestedLeader.EndPoint = shelfMoveGrip.GripPoint + offset;
                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                                break;
                            }

                        case CrestedLeaderArrowAddGrip addLeaderGrip:
                            {
                                var crestedLeader = addLeaderGrip.CrestedLeader;
                                var newPoint = addLeaderGrip.GripPoint + offset;
                                crestedLeader.TempNewArrowPoint = newPoint;

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                                break;
                            }

                        case CrestedLeaderArrowMoveGrip moveLeaderGrip:
                            {
                                var crestedLeader = moveLeaderGrip.CrestedLeader;
                                var newPoint = moveLeaderGrip.GripPoint + offset;
                                crestedLeader.TempNewArrowPoint = newPoint;

                                crestedLeader.UpdateEntities();
                                crestedLeader.BlockRecord.UpdateAnonymousBlocks();
                                break;
                            }

                        default:
                            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                            break;
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