namespace mpESKD.Functions.mpFragmentMarker;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Overrules.Grips;

/// <inheritdoc />
public class FragmentMarkerGripPointOverrule : BaseSmartEntityGripOverrule<FragmentMarker>
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
                var fragmentMarker = EntityReaderService.Instance.GetFromEntity<FragmentMarker>(entity);
                    
                // Паранойя программиста =)
                if (fragmentMarker != null)
                {
                    // Получаем первую ручку (совпадает с точкой вставки блока)
                    var gp = new FragmentMarkerGrip(fragmentMarker, GripName.StartGrip)
                    {
                        GripPoint = fragmentMarker.InsertionPoint
                    };
                    grips.Add(gp);

                    // получаем среднюю ручку
                    gp = new FragmentMarkerGrip(fragmentMarker, GripName.MiddleGrip)
                    {
                        GripPoint = fragmentMarker.MiddlePoint
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new FragmentMarkerGrip(fragmentMarker, GripName.EndGrip)
                    {
                        GripPoint = fragmentMarker.EndPoint
                    };
                    grips.Add(gp);
                    // получаем ручку выноски
                    if (!(!string.IsNullOrEmpty(fragmentMarker.MainText) |
                          !string.IsNullOrEmpty(fragmentMarker.SmallText)))
                        return;
                    gp = new FragmentMarkerGrip(fragmentMarker, GripName.LeaderGrip)
                    {
                        GripPoint = fragmentMarker.LeaderPoint
                    };
                    grips.Add(gp);

                    if ((string.IsNullOrEmpty(fragmentMarker.MainText) | string.IsNullOrEmpty(fragmentMarker.SmallText)) | (string.IsNullOrEmpty(fragmentMarker.MainText) & string.IsNullOrEmpty(fragmentMarker.SmallText)))
                        return;
                    
                    // получаем ручку выравнивания текста
                    
                    var shelfLength = fragmentMarker.TopShelfLineLength;
                    
                    if (fragmentMarker.ShelfPosition == ShelfPosition.Left)
                    {
                        shelfLength = -shelfLength;
                    }

                    if (fragmentMarker.ScaleFactorX < 0)
                    {
                        shelfLength = -shelfLength;
                    }

                    var shelfPointGrip = fragmentMarker.LeaderPoint +
                                         (Vector3d.YAxis *
                                          ((fragmentMarker.MainTextHeight + fragmentMarker.TextVerticalOffset) *
                                           fragmentMarker.GetFullScale()));
                    var alignGripPoint = fragmentMarker.LeaderPoint + (Vector3d.XAxis * shelfLength);
                    alignGripPoint += Vector3d.YAxis * 
                                      (fragmentMarker.MainTextHeight + fragmentMarker.TextVerticalOffset) *
                                      fragmentMarker.GetFullScale();

                    if (fragmentMarker.IsRotated & !fragmentMarker.IsTextAlwaysHorizontal)
                    {
                        shelfPointGrip = shelfPointGrip.RotateBy(fragmentMarker.Rotation, Vector3d.ZAxis, fragmentMarker.LeaderPoint);
                        alignGripPoint = alignGripPoint.RotateBy(fragmentMarker.Rotation, Vector3d.ZAxis, fragmentMarker.LeaderPoint);
                    }

                    grips.Add(new FragmentMarkerShelfPositionGrip(fragmentMarker)
                    {
                        GripPoint = shelfPointGrip,
                        GripType = GripType.TwoArrowsLeftRight
                    });

                    grips.Add(new EntityTextAlignGrip(fragmentMarker,
                        () => fragmentMarker.ValueHorizontalAlignment,
                        (setAlignEntity) => fragmentMarker.ValueHorizontalAlignment = setAlignEntity)
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
                    if (gripData is FragmentMarkerGrip fragmentMarkerGrip)
                    {

                        var gripPoint = fragmentMarkerGrip.GripPoint;
                        var fragmentMarker = fragmentMarkerGrip.FragmentMarker;
                        var scale = fragmentMarker.GetFullScale();

                        // Далее, в зависимости от имени ручки произвожу действия
                        if (fragmentMarkerGrip.GripName == GripName.StartGrip)
                        {
                            // Переношу точку вставки блока, и точку, описывающую первую точку в примитиве
                            // Все точки всегда совпадают (+ ручка)
                            ((BlockReference)entity).Position = gripPoint + offset;
                            var newPt = fragmentMarkerGrip.GripPoint + offset;
                            var length = fragmentMarker.EndPoint.DistanceTo(newPt);

                            if (length < fragmentMarker.MinDistanceBetweenPoints * scale)
                            {
                                /* Если новая точка получается на расстоянии меньше минимального, то
                                 * переносим ее в направлении между двумя точками на минимальное расстояние
                                 */
                                var tmpInsertionPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                    fragmentMarker.EndPoint, newPt, fragmentMarker.EndPoint,
                                    fragmentMarker.MinDistanceBetweenPoints * scale);

                                if (fragmentMarker.EndPoint.Equals(newPt))
                                {
                                    // Если точки совпали, то задаем минимальное значение
                                    tmpInsertionPoint = new Point3d(
                                        fragmentMarker.EndPoint.X + (fragmentMarker.MinDistanceBetweenPoints * scale),
                                        fragmentMarker.EndPoint.Y, fragmentMarker.EndPoint.Z);
                                }

                                ((BlockReference)entity).Position = tmpInsertionPoint;
                                fragmentMarker.InsertionPoint = tmpInsertionPoint;
                            }
                            else
                            {
                                ((BlockReference)entity).Position = fragmentMarkerGrip.GripPoint + offset;
                                fragmentMarker.InsertionPoint = fragmentMarkerGrip.GripPoint + offset;
                            }
                        }

                        if (fragmentMarkerGrip.GripName == GripName.MiddleGrip)
                        {
                            // Т.к. средняя точка нужна для переноса примитива, но не соответствует точки вставки блока
                            // и получается как средняя точка между InsertionPoint и EndPoint, то я переношу
                            // точку вставки
                            var lengthVector = (fragmentMarker.InsertionPoint - fragmentMarker.EndPoint) / 2;
                            ((BlockReference)entity).Position = fragmentMarkerGrip.GripPoint + offset + lengthVector;
                        }

                        if (fragmentMarkerGrip.GripName == GripName.EndGrip)
                        {
                            var newPt = fragmentMarkerGrip.GripPoint + offset;
                            if (newPt.Equals(((BlockReference)entity).Position))
                            {
                                fragmentMarker.EndPoint = new Point3d(
                                    ((BlockReference)entity).Position.X + (fragmentMarker.MinDistanceBetweenPoints * scale),
                                    ((BlockReference)entity).Position.Y, ((BlockReference)entity).Position.Z);
                            }

                            // С конечной точкой все просто
                            else
                            {
                                fragmentMarker.EndPoint = fragmentMarkerGrip.GripPoint + offset;
                            }
                        }
                            
                        if (fragmentMarkerGrip.GripName == GripName.LeaderGrip)
                        {
                            fragmentMarker.LeaderPoint = gripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        fragmentMarker.UpdateEntities();
                        fragmentMarker.BlockRecord.UpdateAnonymousBlocks();
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