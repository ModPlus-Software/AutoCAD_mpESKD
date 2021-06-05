namespace mpESKD.Functions.mpFragmentMarker.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;
    using Grips;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    public class FragmentMarkerGripPointOverrule : GripOverrule
    {
        private static FragmentMarkerGripPointOverrule _fragmentMakerGripPointOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FragmentMarkerGripPointOverrule Instance()
        {
            if (_fragmentMakerGripPointOverrule != null)
            {
                return _fragmentMakerGripPointOverrule;
            }

            _fragmentMakerGripPointOverrule = new FragmentMarkerGripPointOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _fragmentMakerGripPointOverrule.SetXDataFilter(FragmentMarker.GetDescriptor().Name);
            return _fragmentMakerGripPointOverrule;
        }

        /// <inheritdoc />
        public override void GetGripPoints(
            Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            Debug.Print("FragmentMarkerGripPointOverrule");
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
                        //gp = new FragmentMarkerGrip(fragmentMarker, GripName.MiddleGrip)
                        //{
                        //    GripPoint = fragmentMarker.MiddlePoint
                        //};
                        //grips.Add(gp);

                        // получаем ручку выноски
                        gp = new FragmentMarkerGrip(fragmentMarker, GripName.LeaderGrip)
                        {
                            GripPoint = fragmentMarker.LeaderPoint
                        };
                        
                        // получаем конечную ручку
                        gp = new FragmentMarkerGrip(fragmentMarker, GripName.EndGrip)
                        {
                            GripPoint = fragmentMarker.EndPoint
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
                                Debug.Print(fragmentMarkerGrip.GripName.ToString());
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

                            //if (fragmentMarkerGrip.GripName == GripName.MiddleGrip)
                            //{
                            //    // Т.к. средняя точка нужна для переноса примитива, но не соответствует точки вставки блока
                            //    // и получается как средняя точка между InsertionPoint и EndPoint, то я переношу
                            //    // точку вставки
                            //    var lengthVector = (fragmentMarker.InsertionPoint - fragmentMarker.EndPoint) / 2;
                            //    ((BlockReference)entity).Position = fragmentMarkerGrip.GripPoint + offset + lengthVector;
                            //}

                            if (fragmentMarkerGrip.GripName == GripName.EndGrip)
                            {
                                Debug.Print(fragmentMarkerGrip.GripName.ToString());
                                
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
                                Debug.Print(fragmentMarkerGrip.GripName.ToString());

                                fragmentMarker.EndPoint = gripPoint + offset;
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

        /// <inheritdoc />
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, FragmentMarker.GetDescriptor().Name);
        }
    }
}