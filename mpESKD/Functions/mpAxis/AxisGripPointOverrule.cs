namespace mpESKD.Functions.mpAxis
{
    using System;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Overrules;
    using Grips;
    using ModPlusAPI.Windows;
    using Exception = Autodesk.AutoCAD.Runtime.Exception;

    /// <inheritdoc />
    public class AxisGripPointOverrule : BaseSmartEntityGripOverrule<Axis>
    {
        private Point3d _initInsertionPoint;
        private Point3d _initBottomOrientPoint;
        private Point3d _initTopOrientPoint;
        
        /// <inheritdoc />
        public override void GetGripPoints(
            Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            try
            {
                Debug.Print("AxisGripPointOverrule");

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
                    var axis = EntityReaderService.Instance.GetFromEntity<Axis>(entity);

                    // Паранойя программиста =)
                    if (axis != null)
                    {
                        // Получаем первую ручку (совпадает с точкой вставки блока)
                        var gp = new AxisGrip(axis)
                        {
                            GripName = GripName.StartGrip,
                            GripPoint = axis.InsertionPoint // вот эта точка из экземпляра класса axis
                        };
                        grips.Add(gp);
                        _initInsertionPoint = axis.InsertionPoint;

                        // получаем среднюю ручку
                        gp = new AxisGrip(axis)
                        {
                            GripName = GripName.MiddleGrip,
                            GripPoint = axis.MiddlePoint
                        };
                        grips.Add(gp);

                        // получаем конечную ручку
                        gp = new AxisGrip(axis)
                        {
                            GripName = GripName.EndGrip,
                            GripPoint = axis.EndPoint
                        };
                        grips.Add(gp);

                        if (axis.MarkersPosition == AxisMarkersPosition.Both ||
                            axis.MarkersPosition == AxisMarkersPosition.Bottom)
                        {
                            // other points
                            gp = new AxisGrip(axis)
                            {
                                GripName = GripName.BottomMarkerGrip,
                                GripPoint = axis.BottomMarkerPoint
                            };
                            grips.Add(gp);
                        }

                        if (axis.MarkersPosition == AxisMarkersPosition.Both ||
                            axis.MarkersPosition == AxisMarkersPosition.Top)
                        {
                            gp = new AxisGrip(axis)
                            {
                                GripName = GripName.TopMarkerGrip,
                                GripPoint = axis.TopMarkerPoint
                            };
                            grips.Add(gp);
                        }

                        // orient
                        if (axis.MarkersPosition == AxisMarkersPosition.Both || axis.MarkersPosition == AxisMarkersPosition.Bottom)
                        {
                            if (axis.BottomOrientMarkerVisible)
                            {
                                gp = new AxisGrip(axis)
                                {
                                    GripName = GripName.BottomOrientGrip,
                                    GripPoint = axis.BottomOrientPoint
                                };
                                grips.Add(gp);
                                _initBottomOrientPoint = axis.BottomOrientPoint;
                            }
                        }

                        if (axis.MarkersPosition == AxisMarkersPosition.Both || axis.MarkersPosition == AxisMarkersPosition.Top)
                        {
                            if (axis.TopOrientMarkerVisible)
                            {
                                gp = new AxisGrip(axis)
                                {
                                    GripName = GripName.TopOrientGrip,
                                    GripPoint = axis.TopOrientPoint
                                };
                                grips.Add(gp);
                                _initTopOrientPoint = axis.TopOrientPoint;
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
                        var gripPoint = gripData as AxisGrip;

                        // Проверяем, что это та ручка, что мне нужна. 
                        if (gripPoint != null)
                        {
                            // Далее, в зависимости от имени ручки произвожу действия
                            var axis = gripPoint.Axis;
                            var scale = axis.GetFullScale();

                            if (gripPoint.GripName == GripName.StartGrip)
                            {
                                // Переношу точку вставки блока, и точку, описывающую первую точку в примитиве
                                // Все точки всегда совпадают (+ ручка)
                                var newPt = gripPoint.GripPoint + offset;
                                var length = axis.EndPoint.DistanceTo(newPt);
                                if (length < axis.MinDistanceBetweenPoints * scale)
                                {
                                    /* Если новая точка получается на расстоянии меньше минимального, то
                                     * переносим ее в направлении между двумя точками на минимальное расстояние
                                     */
                                    var tmpInsertionPoint = ModPlus.Helpers.GeometryHelpers.Point3dAtDirection(
                                        axis.EndPoint, newPt, axis.EndPoint,
                                        axis.MinDistanceBetweenPoints * scale);

                                    if (axis.EndPoint.Equals(newPt))
                                    {
                                        // Если точки совпали, то задаем минимальное значение
                                        tmpInsertionPoint = new Point3d(
                                            axis.EndPoint.X,
                                            axis.EndPoint.Y - (axis.MinDistanceBetweenPoints * scale),
                                            axis.EndPoint.Z);
                                    }

                                    ((BlockReference)entity).Position = tmpInsertionPoint;
                                    axis.InsertionPoint = tmpInsertionPoint;
                                }
                                else
                                {
                                    ((BlockReference)entity).Position = gripPoint.GripPoint + offset;
                                    axis.InsertionPoint = gripPoint.GripPoint + offset;
                                }
                            }

                            if (gripPoint.GripName == GripName.MiddleGrip)
                            {
                                // Т.к. средняя точка нужна для переноса примитива, но не соответствует точки вставки блока
                                // и получается как средняя точка между InsertionPoint и EndPoint, то я переношу
                                // точку вставки
                                var lengthVector = (axis.InsertionPoint - axis.EndPoint) / 2;
                                ((BlockReference)entity).Position = gripPoint.GripPoint + offset + lengthVector;
                            }

                            if (gripPoint.GripName == GripName.EndGrip)
                            {
                                var newPt = gripPoint.GripPoint + offset;
                                Point3d newEndPoint;
                                if (newPt.Equals(((BlockReference)entity).Position))
                                {
                                    newEndPoint = new Point3d(
                                        ((BlockReference)entity).Position.X,
                                        ((BlockReference)entity).Position.Y - (axis.MinDistanceBetweenPoints * scale),
                                        ((BlockReference)entity).Position.Z);
                                    axis.EndPoint = newEndPoint;
                                }
                                else
                                {
                                    axis.EndPoint = gripPoint.GripPoint + offset;
                                    newEndPoint = gripPoint.GripPoint + offset;
                                }

                                // change bottom orient point
                                axis.BottomOrientPoint = GetSavePositionPoint(
                                    _initInsertionPoint,
                                    gripPoint.GripPoint,
                                    newEndPoint,
                                    _initBottomOrientPoint);
                            }

                            if (gripPoint.GripName == GripName.BottomMarkerGrip)
                            {
                                var mainVector = axis.EndPoint - axis.InsertionPoint;
                                var v = mainVector.CrossProduct(Vector3d.ZAxis).GetNormal();
                                axis.BottomMarkerPoint = gripPoint.GripPoint + (offset.DotProduct(v) * v);

                                // Меняю также точку маркера-ориентира
                                if (_initBottomOrientPoint != Point3d.Origin)
                                {
                                    axis.BottomOrientPoint = _initBottomOrientPoint + (offset.DotProduct(v) * v);
                                }
                            }

                            if (gripPoint.GripName == GripName.TopMarkerGrip)
                            {
                                var mainVector = axis.InsertionPoint - axis.EndPoint;
                                var v = mainVector.CrossProduct(Vector3d.ZAxis).GetNormal();
                                axis.TopMarkerPoint = gripPoint.GripPoint + (offset.DotProduct(v) * v);

                                // Меняю также точку маркера-ориентира
                                if (_initTopOrientPoint != Point3d.Origin)
                                {
                                    axis.TopOrientPoint = _initTopOrientPoint + (offset.DotProduct(v) * v);
                                }
                            }

                            if (gripPoint.GripName == GripName.BottomOrientGrip)
                            {
                                var mainVector = axis.EndPoint - axis.InsertionPoint;
                                var v = mainVector.CrossProduct(Vector3d.ZAxis).GetNormal();
                                var newPoint = gripPoint.GripPoint + (offset.DotProduct(v) * v);

                                if (Math.Abs((newPoint - axis.BottomMarkerPoint).Length) >
                                    axis.MarkersDiameter * scale)
                                {
                                    axis.BottomOrientPoint = newPoint;
                                }
                                else
                                {
                                    if (newPoint.X >= axis.BottomMarkerPoint.X)
                                    {
                                        axis.BottomOrientPoint =
                                            axis.BottomMarkerPoint + (v * -1 * axis.MarkersDiameter * scale);
                                    }
                                    else
                                    {
                                        axis.BottomOrientPoint =
                                            axis.BottomMarkerPoint + (v * axis.MarkersDiameter * scale);
                                    }
                                }
                            }

                            if (gripPoint.GripName == GripName.TopOrientGrip)
                            {
                                var mainVector = axis.InsertionPoint - axis.EndPoint;
                                var v = mainVector.CrossProduct(Vector3d.ZAxis).GetNormal();
                                var newPoint = gripPoint.GripPoint + (offset.DotProduct(v) * v);

                                if (Math.Abs((newPoint - axis.TopMarkerPoint).Length) >
                                    axis.MarkersDiameter * scale)
                                {
                                    axis.TopOrientPoint = newPoint;
                                }
                                else
                                {
                                    if (newPoint.X >= axis.TopMarkerPoint.X)
                                    {
                                        axis.TopOrientPoint =
                                            axis.TopMarkerPoint + (v * axis.MarkersDiameter * scale);
                                    }
                                    else
                                    {
                                        axis.TopOrientPoint =
                                            axis.TopMarkerPoint + (v * -1 * axis.MarkersDiameter * scale);
                                    }
                                }
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            axis.UpdateEntities();
                            axis.BlockRecord.UpdateAnonymousBlocks();
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
        
        #region Helpers

        /// <summary>Получение нового значения точки, которая должна сохранить свое положение
        /// относительно передвигаемой точки</summary>
        /// <param name="basePoint">Базовая точка - точка, относительно которой все двигается</param>
        /// <param name="oldMovedPoint">Начальное значение передвигаемой точки</param>
        /// <param name="newMovedPoint">Новое значение передвигаемой точки</param>
        /// <param name="oldSavePositionPoint">Начальное значение точки, положение которой должно сохранится
        /// относительно передвигаемой точки</param>
        /// <returns></returns>
        private static Point3d GetSavePositionPoint(Point3d basePoint, Point3d oldMovedPoint, Point3d newMovedPoint, Point3d oldSavePositionPoint)
        {
            var vectorOld = oldMovedPoint - basePoint;
            var vectorNew = newMovedPoint - basePoint;
            var l = vectorNew.Length - vectorOld.Length;
            var h = l * vectorOld.GetNormal();
            var angle = vectorOld.GetAngleTo(vectorNew, Vector3d.ZAxis);

            var tmpP = new Point3d(
                oldSavePositionPoint.X - basePoint.X,
                oldSavePositionPoint.Y - basePoint.Y,
                oldSavePositionPoint.Z) + h;
            return new Point3d(
                (tmpP.X * Math.Cos(angle)) - (tmpP.Y * Math.Sin(angle)) + basePoint.X,
                (tmpP.X * Math.Sin(angle)) + (tmpP.Y * Math.Cos(angle)) + basePoint.Y,
                oldSavePositionPoint.Z);
        }

        #endregion
    }
}
