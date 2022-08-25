namespace mpESKD.Functions.mpNodalLeader;

using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class NodalLeaderGripPointOverrule : BaseSmartEntityGripOverrule<NodalLeader>
{
    private Point3d _initFramePoint;

    /// <inheritdoc />
    //public override void GetGripPoints(
    //    Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
    //{
    //    try
    //    {
    //        if (IsApplicable(entity))
    //        {
    //            // Удаляю все ручки - это удалит ручку вставки блока
    //            grips.Clear();

    //            var nodalLeader = EntityReaderService.Instance.GetFromEntity<NodalLeader>(entity);
    //            if (nodalLeader != null)
    //            {
    //                grips.Add(new NodalLeaderGrip(
    //                    nodalLeader, GripType.BasePoint, GripName.InsertionPoint, nodalLeader.InsertionPoint));
    //                grips.Add(new NodalLeaderGrip(
    //                    nodalLeader, GripType.Point, GripName.FramePoint, nodalLeader.LeaderPoint));
    //                grips.Add(new NodalLeaderGrip(
    //                    nodalLeader, GripType.Point, GripName.LeaderPoint, nodalLeader.EndPoint));

    //                grips.Add(new NodalLevelShelfPositionGrip(nodalLeader)
    //                {
    //                    GripPoint = nodalLeader.EndPoint +
    //                                (Vector3d.YAxis * ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) * nodalLeader.GetFullScale())),
    //                    GripType = GripType.TwoArrowsLeftRight
    //                });

    //                _initFramePoint = nodalLeader.LeaderPoint;
    //            }
    //        }
    //    }
    //    catch (Exception exception)
    //    {
    //        if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
    //            ExceptionBox.Show(exception);
    //    }
    //}


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

                    // получаем ручку выноски
                    if (!(!string.IsNullOrEmpty(nodalLeader.NodeNumber) |
                          !string.IsNullOrEmpty(nodalLeader.SheetNumber)))
                        return;
                    gp = new NodalLeaderGrip(nodalLeader, GripName.LeaderPoint)
                    {
                        GripPoint = nodalLeader.LeaderPoint
                    };
                    grips.Add(gp);
                    grips.Add(new NodalLevelShelfPositionGrip(nodalLeader)
                    {
                        GripPoint = nodalLeader.LeaderPoint +
                                    (Vector3d.YAxis * ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) * nodalLeader.GetFullScale())),
                        GripType = GripType.TwoArrowsLeftRight
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


    //public override void MoveGripPointsAt(
    //    Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
    //{
    //    try
    //    {
    //        if (IsApplicable(entity))
    //        {
    //            // Проходим по коллекции ручек
    //            foreach (var gripData in grips)
    //            {
    //                if (gripData is NodalLeaderGrip nodalLeaderGrip)
    //                {

    //                    var gripPoint = nodalLeaderGrip.GripPoint;
    //                    var nodalLeader = nodalLeaderGrip.NodalLeader;
    //                    var scale = nodalLeader.GetFullScale();

    //                    // Далее, в зависимости от имени ручки произвожу действия
    //                    if (nodalLeaderGrip.GripName == GripName.InsertionPoint)
    //                    {
    //                        // Переношу точку вставки блока, и точку, описывающую первую точку в примитиве
    //                        // Все точки всегда совпадают (+ ручка)
    //                        //((BlockReference)entity).Position = gripPoint + offset;
    //                        //var newPt = nodalLeaderGrip.GripPoint + offset;
    //                        //var length = fragmentMarker.EndPoint.DistanceTo(newPt);
    //                        ((BlockReference)entity).Position = gripPoint + offset;
    //                        nodalLeader.InsertionPoint = gripPoint + offset;
    //                        nodalLeader.LeaderPoint = _initFramePoint + offset;

    //                        if (length < nodalLeader.MinDistanceBetweenPoints * scale)
    //                        {
    //                            /* Если новая точка получается на расстоянии меньше минимального, то
    //                             * переносим ее в направлении между двумя точками на минимальное расстояние
    //                             */
    //                            var tmpInsertionPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
    //                                nodalLeader.EndPoint, newPt, nodalLeader.EndPoint,
    //                                nodalLeader.MinDistanceBetweenPoints * scale);

    //                            if (nodalLeader.EndPoint.Equals(newPt))
    //                            {
    //                                // Если точки совпали, то задаем минимальное значение
    //                                tmpInsertionPoint = new Point3d(
    //                                    nodalLeader.EndPoint.X + (nodalLeader.MinDistanceBetweenPoints * scale),
    //                                    nodalLeader.EndPoint.Y, nodalLeader.EndPoint.Z);
    //                            }

    //                            ((BlockReference)entity).Position = tmpInsertionPoint;
    //                            nodalLeader.InsertionPoint = tmpInsertionPoint;
    //                        }
    //                        else
    //                        {
    //                            ((BlockReference)entity).Position = nodalLeaderGrip.GripPoint + offset;
    //                            nodalLeader.InsertionPoint = nodalLeaderGrip.GripPoint + offset;
    //                        }
    //                    }
    //                    if (nodalLeaderGrip.GripName == GripName.FramePoint)
    //                    {
    //                        var newPt = nodalLeaderGrip.GripPoint + offset;
    //                        if (newPt.Equals(((BlockReference)entity).Position))
    //                        {
    //                            nodalLeader.EndPoint = new Point3d(
    //                                ((BlockReference)entity).Position.X + (nodalLeader.MinDistanceBetweenPoints * scale),
    //                                ((BlockReference)entity).Position.Y, ((BlockReference)entity).Position.Z);
    //                        }

    //                        // С конечной точкой все просто
    //                        else
    //                        {
    //                            nodalLeader.EndPoint = nodalLeaderGrip.GripPoint + offset;
    //                        }
    //                    }

    //                    if (nodalLeaderGrip.GripName == GripName.LeaderPoint)
    //                    {
    //                        nodalLeader.LeaderPoint = gripPoint + offset;
    //                    }

    //                    // Вот тут происходит перерисовка примитивов внутри блока
    //                    nodalLeader.UpdateEntities();
    //                    nodalLeader.BlockRecord.UpdateAnonymousBlocks();
    //                }
    //                else
    //                {
    //                    base.MoveGripPointsAt(entity, grips, offset, bitFlags);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
    //        }
    //    }
    //    catch (Exception exception)
    //    {
    //        if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
    //            ExceptionBox.Show(exception);
    //    }
    //}

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
                            //nodalLeader.InsertionPoint = gripPoint + offset;
                            //nodalLeader.LeaderPoint =  + offset;
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