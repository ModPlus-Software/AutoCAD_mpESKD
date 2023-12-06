namespace mpESKD.Functions.mpThickArrow;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Functions.mpThickArrow.Grips;
using mpESKD.Functions.mpThickArrow;
using System.Diagnostics;


/// <inheritdoc />
public class ThickArrowGripPointOverrule : BaseSmartEntityGripOverrule<mpThickArrow.ThickArrow>
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
                        GripPoint = thickArrow.InsertionPoint
                    };
                    grips.Add(gp);

                    // получаем среднюю ручку
                    gp = new ThickArrowGrip(thickArrow, GripName.MiddleGrip)
                    {
                        GripPoint = thickArrow.MiddlePoint
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new ThickArrowGrip(thickArrow, GripName.EndGrip)
                    {
                        GripPoint = thickArrow.EndPoint
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
                    if (gripData is ThickArrowGrip gripPoint)
                    {
                        var thickArrow = gripPoint.ThickArrow;
                        var scale = thickArrow.GetFullScale();

                        // Далее, в зависимости от имени ручки произвожу действия
                        if (gripPoint.GripName == GripName.StartGrip)
                        {
                            // Переношу точку вставки блока, и точку, описывающую первую точку в примитиве
                            // Все точки всегда совпадают (+ ручка)
                            var newPt = gripPoint.GripPoint + offset;
                            var length = thickArrow.EndPoint.DistanceTo(newPt);

                            if (length < thickArrow.MinDistanceBetweenPoints * scale)
                            {
                                /* Если новая точка получается на расстоянии меньше минимального, то
                                 * переносим ее в направлении между двумя точками на минимальное расстояние
                                 */
                                var tmpInsertionPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                    thickArrow.EndPoint, newPt, thickArrow.EndPoint,
                                    thickArrow.MinDistanceBetweenPoints * scale);

                                if (thickArrow.EndPoint.Equals(newPt))
                                {
                                    // Если точки совпали, то задаем минимальное значение
                                    tmpInsertionPoint = new Point3d(
                                        thickArrow.EndPoint.X + (thickArrow.MinDistanceBetweenPoints * scale),
                                        thickArrow.EndPoint.Y, thickArrow.EndPoint.Z);
                                }

                                ((BlockReference)entity).Position = tmpInsertionPoint;
                                thickArrow.InsertionPoint = tmpInsertionPoint;
                            }
                            else
                            {
                                ((BlockReference)entity).Position = gripPoint.GripPoint + offset;
                                thickArrow.InsertionPoint = gripPoint.GripPoint + offset;
                            }
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
                            if (newPt.Equals(((BlockReference)entity).Position))
                            {
                                thickArrow.EndPoint = new Point3d(
                                    ((BlockReference)entity).Position.X + (thickArrow.MinDistanceBetweenPoints * scale),
                                    ((BlockReference)entity).Position.Y, ((BlockReference)entity).Position.Z);
                            }

                            // С конечной точкой все просто
                            else
                            {
                                thickArrow.EndPoint = gripPoint.GripPoint + offset;
                            }
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