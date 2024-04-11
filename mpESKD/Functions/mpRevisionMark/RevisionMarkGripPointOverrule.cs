namespace mpESKD.Functions.mpRevisionMark;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Enums;
using System;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class RevisionMarkGripPointOverrule : BaseSmartEntityGripOverrule<RevisionMark>
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
                var revisionMark = EntityReaderService.Instance.GetFromEntity<RevisionMark>(entity);

                // Паранойя программиста =)
                if (revisionMark != null)
                {
                    // Получаем первую ручку (совпадает с точкой вставки блока)
                    var gp = new RevisionMarkGrip(revisionMark, GripName.InsertionPoint)
                    {
                        GripPoint = revisionMark.InsertionPoint
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new RevisionMarkGrip(revisionMark, GripName.FramePoint)
                    {
                        GripPoint = revisionMark.EndPoint
                    };
                    grips.Add(gp);

                    // получаем ручку типа рамки
                    grips.Add(new RevisionFrameTypeGrip(revisionMark)
                    {
                        GripPoint = new Point3d(
                            ((revisionMark.EndPoint.X - revisionMark.InsertionPoint.X) * -1) + revisionMark.InsertionPoint.X,
                            ((revisionMark.EndPoint.Y - revisionMark.InsertionPoint.Y) * -1) + revisionMark.InsertionPoint.Y,
                            revisionMark.EndPoint.Z)
                    });

                    // получаем ручку выноски
                    if (!(!string.IsNullOrEmpty(revisionMark.RevisionNumber) |
                          !string.IsNullOrEmpty(revisionMark.Note)))
                        return;
                    gp = new RevisionMarkGrip(revisionMark, GripName.LeaderPoint)
                    {
                        GripPoint = revisionMark.LeaderPoint
                    };
                    grips.Add(gp);

                    /*
                    var shelfLength = revisionMark.RevisionNumber.Length ;

                    if (revisionMark.MarkPosition == MarkPosition.Left)
                    {
                        shelfLength = -shelfLength;
                    }

                    if (revisionMark.ScaleFactorX < 0)
                    {
                        shelfLength = -shelfLength;
                    }
                    */

                    var shelfPointGrip = revisionMark.LeaderPoint +
                                         (Vector3d.YAxis *
                                          ((revisionMark.RevisionTextHeight + revisionMark.TextVerticalOffset) *
                                           revisionMark.GetFullScale()));

                    grips.Add(new RevisionMarkShelfPositionGrip(revisionMark)
                    {
                        GripPoint = shelfPointGrip
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
                    if (gripData is RevisionMarkGrip levelMarkGrip)
                    {
                        var gripPoint = levelMarkGrip.GripPoint;
                        var revisionMark = levelMarkGrip.RevisionMark;
                        var scale = revisionMark.GetFullScale();

                        if (levelMarkGrip.GripName == GripName.InsertionPoint)
                        {
                            ((BlockReference)entity).Position = gripPoint + offset;
                        }
                        else if (levelMarkGrip.GripName == GripName.FramePoint)
                        {
                            if (revisionMark.FrameType == FrameType.Rectangular)
                            {
                                var currentPosition = gripPoint + offset;
                                var frameHeight =
                                    Math.Abs(currentPosition.Y - revisionMark.InsertionPoint.Y) / scale;
                                var frameWidth = Math.Abs(currentPosition.X - revisionMark.InsertionPoint.X) / scale;

                                if (!(frameHeight <= revisionMark.MinDistanceBetweenPoints) &&
                                    !(frameWidth <= revisionMark.MinDistanceBetweenPoints))
                                {
                                    revisionMark.EndPoint = gripPoint + offset;
                                }
                            }
                            else
                            {
                                revisionMark.EndPoint = gripPoint + offset;
                            }
                        }
                        else if (levelMarkGrip.GripName == GripName.LeaderPoint)
                        {
                            revisionMark.LeaderPoint = gripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        revisionMark.UpdateEntities();
                        revisionMark.BlockRecord.UpdateAnonymousBlocks();
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