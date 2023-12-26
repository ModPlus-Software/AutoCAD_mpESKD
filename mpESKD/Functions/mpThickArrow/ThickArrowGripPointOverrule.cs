namespace mpESKD.Functions.mpThickArrow;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Base.Utils;
using Grips;
using ModPlusAPI.Windows;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class ThickArrowGripPointOverrule : BaseSmartEntityGripOverrule<ThickArrow>
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
                var thickArrow = EntityReaderService.Instance.GetFromEntity<ThickArrow>(entity);

                // Паранойя программиста =)
                if (thickArrow != null)
                {
                    // Получаем первую ручку (совпадает с точкой вставки блока)
                    var gp = new ThickArrowGrip(thickArrow, GripName.StartGrip)
                    {
                        GripPoint = thickArrow.InsertionPoint,
                    };
                    grips.Add(gp);

                    // получаем среднюю ручку
                    gp = new ThickArrowGrip(thickArrow, GripName.MiddleGrip)
                    {
                        GripPoint = thickArrow.MiddlePoint,
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new ThickArrowGrip(thickArrow, GripName.EndGrip)
                    {
                        GripPoint = thickArrow.EndPoint,
                    };
                    grips.Add(gp);
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
                // Проходим по коллекции ручек
                foreach (var gripData in grips)
                {
                    // Приводим ручку к моему классу
                    var gripPoint = gripData as ThickArrowGrip;

                    // Проверяем, что это та ручка, что мне нужна. 
                    if (gripPoint != null)
                    {
                        // Далее, в зависимости от имени ручки произвожу действия
                        var thickArrow = gripPoint.ThickArrow;
                        var scale = thickArrow.GetFullScale();

                        var minDistance = thickArrow.MinDistanceBetweenPoints * scale;

                        if (gripPoint.GripName == GripName.StartGrip)
                        {
                            var newPt = gripPoint.GripPoint + offset;

                            if (thickArrow.EndPoint.DistanceTo(newPt) < minDistance)
                            {
                                newPt = GeometryUtils.Point3dAtDirection(thickArrow.EndPoint, newPt, minDistance);
                            }

                            ((BlockReference)entity).Position = newPt;
                            thickArrow.InsertionPoint = newPt;
                        }

                        if (gripPoint.GripName == GripName.MiddleGrip)
                        {
                            // Т.к. средняя точка нужна для переноса примитива, но не соответствует точки вставки блока
                            // и получается как средняя точка между InsertionPoint и EndPoint, то я переношу
                            // точку вставки
                            var lengthVector = (thickArrow.InsertionPoint - thickArrow.EndPoint) / 2;
                            ((BlockReference)entity).Position = gripPoint.GripPoint + offset + lengthVector;
                        }

                        if (gripPoint.GripName == GripName.EndGrip)
                        {
                            var newPt = gripPoint.GripPoint + offset;
                            
                            if (thickArrow.InsertionPoint.DistanceTo(newPt) < minDistance)
                            {
                                newPt = GeometryUtils.Point3dAtDirection(thickArrow.InsertionPoint, newPt, minDistance);
                            }

                            thickArrow.EndPoint = newPt;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        thickArrow.UpdateEntities();
                        thickArrow.BlockRecord.UpdateAnonymousBlocks();
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