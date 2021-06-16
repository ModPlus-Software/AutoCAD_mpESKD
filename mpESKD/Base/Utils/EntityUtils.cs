namespace mpESKD.Base.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Overrules;
    using Overrules.Grips;
    using View;

    /// <summary>
    /// Утилиты для объектов
    /// </summary>
    public static class EntityUtils
    {
        /// <summary>
        /// Установка свойств для однострочного текста
        /// </summary>
        /// <param name="dbText">Однострочный текст</param>
        /// <param name="textStyle">имя текстового стиля</param>
        /// <param name="height">Высота текста (с учетом масштаба блока)</param>
        public static void SetProperties(this DBText dbText, string textStyle, double height)
        {
            dbText.Height = height;
            dbText.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
            dbText.Linetype = "ByBlock";
            dbText.LineWeight = LineWeight.ByBlock;
            dbText.TextStyleId = AcadUtils.GetTextStyleIdByName(textStyle);
        }

        /// <summary>
        /// Установить позицию для однострочного текста
        /// </summary>
        /// <param name="dbText">Однострочный текст</param>
        /// <param name="horizontalMode">Выравнивание по горизонтали</param>
        /// <param name="verticalMode">Выравнивание по вертикали</param>
        /// <param name="attachmentPoint">Привязка к точке вставки</param>
        public static void SetPosition(
            this DBText dbText,
            TextHorizontalMode? horizontalMode = null,
            TextVerticalMode? verticalMode = null,
            AttachmentPoint? attachmentPoint = null)
        {
            if (horizontalMode.HasValue)
                dbText.HorizontalMode = horizontalMode.Value;
            if (verticalMode.HasValue)
                dbText.VerticalMode = verticalMode.Value;
            if (attachmentPoint.HasValue)
                dbText.Justify = attachmentPoint.Value;
        }
        
        /// <summary>
        /// Редактирование свойств для интеллектуального объекта в специальном окне. Применяется для интеллектуальных
        /// объектов, содержащих текстовые значения
        /// </summary>
        /// <param name="blockReference">Блок, представляющий интеллектуальный объект</param>
        public static void DoubleClickEdit(BlockReference blockReference)
        {
            BeditCommandWatcher.UseBedit = false;

            var smartEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
            if (smartEntity is IWithDoubleClickEditor entityWithEditor)
            {
                smartEntity.UpdateEntities();
                var saveBack = false;

                var entityType = entityWithEditor.GetType();
                var control = TypeFactory.Instance.GetClickEditControl(entityType);
                control.Initialize(entityWithEditor);
                var sectionValueEditor = new DoubleClickEditor(
                    TypeFactory.Instance.GetDescriptor(entityType).LName,
                    control);

                if (sectionValueEditor.ShowDialog() == true)
                {
                    saveBack = true;
                }

                if (saveBack)
                {
                    smartEntity.UpdateEntities();
                    smartEntity.BlockRecord.UpdateAnonymousBlocks();
                    using (var resBuf = smartEntity.GetDataForXData())
                    {
                        blockReference.XData = resBuf;
                    }
                }

                smartEntity.Dispose();
            }
        }

        /// <summary>
        /// Возвращает стандартные ручки для линейного интеллектуального объекта:
        /// ручки вершин, добавить вершину, удалить вершину, реверс объекта
        /// </summary>
        /// <param name="linearEntity">Линейный интеллектуальный объекты</param>
        /// <param name="curViewUnitSize">Размер единиц текущего вида</param>
        public static IEnumerable<SmartEntityGripData> GetLinearEntityGeneralGrips(
            ILinearEntity linearEntity, double curViewUnitSize)
        {
            var smartEntity = (SmartEntity)linearEntity;

            // Если средних точек нет, значит линия состоит всего из двух точек
            // в этом случае не нужно добавлять точки удаления крайних вершин

            // insertion (start) grip
            var vertexGrip = new LinearEntityVertexGrip(smartEntity, 0)
            {
                GripPoint = linearEntity.InsertionPoint
            };
            yield return vertexGrip;

            if (linearEntity.MiddlePoints.Any())
            {
                var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, 0)
                {
                    GripPoint = linearEntity.InsertionPoint - (Vector3d.YAxis * 20 * curViewUnitSize)
                };
                yield return removeVertexGrip;
            }

            // middle points
            for (var index = 0; index < linearEntity.MiddlePoints.Count; index++)
            {
                vertexGrip = new LinearEntityVertexGrip(smartEntity, index + 1)
                {
                    GripPoint = linearEntity.MiddlePoints[index]
                };
                yield return vertexGrip;

                var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, index + 1)
                {
                    GripPoint = linearEntity.MiddlePoints[index] - (Vector3d.YAxis * 20 * curViewUnitSize)
                };
                yield return removeVertexGrip;
            }

            // end point
            vertexGrip = new LinearEntityVertexGrip(smartEntity, linearEntity.MiddlePoints.Count + 1)
            {
                GripPoint = linearEntity.EndPoint
            };
            yield return vertexGrip;

            if (linearEntity.MiddlePoints.Any())
            {
                var removeVertexGrip = new LinearEntityRemoveVertexGrip(smartEntity, linearEntity.MiddlePoints.Count + 1)
                {
                    GripPoint = linearEntity.EndPoint - (Vector3d.YAxis * 20 * curViewUnitSize)
                };
                yield return removeVertexGrip;
            }

            #region AddVertex grips

            // add vertex grips
            for (var i = 0; i < linearEntity.MiddlePoints.Count; i++)
            {
                if (i == 0)
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(
                        smartEntity,
                        linearEntity.InsertionPoint, linearEntity.MiddlePoints[i])
                    {
                        GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.InsertionPoint, linearEntity.MiddlePoints[i])
                    };
                    yield return addVertexGrip;
                }
                else
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(
                        smartEntity,
                        linearEntity.MiddlePoints[i - 1], linearEntity.MiddlePoints[i])
                    {
                        GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.MiddlePoints[i - 1], linearEntity.MiddlePoints[i])
                    };
                    yield return addVertexGrip;
                }

                // last segment
                if (i == linearEntity.MiddlePoints.Count - 1)
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(
                        smartEntity,
                        linearEntity.MiddlePoints[i], linearEntity.EndPoint)
                    {
                        GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.MiddlePoints[i], linearEntity.EndPoint)
                    };
                    yield return addVertexGrip;
                }
            }

            {
                if (linearEntity.MiddlePoints.Any())
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.EndPoint, null)
                    {
                        GripPoint = linearEntity.EndPoint +
                                    ((linearEntity.EndPoint - linearEntity.MiddlePoints.Last()).GetNormal() * 20 * curViewUnitSize)
                    };
                    yield return addVertexGrip;

                    addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, null, linearEntity.InsertionPoint)
                    {
                        GripPoint = linearEntity.InsertionPoint +
                                    ((linearEntity.InsertionPoint - linearEntity.MiddlePoints.First()).GetNormal() * 20 * curViewUnitSize)
                    };
                    yield return addVertexGrip;
                }
                else
                {
                    var addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.EndPoint, null)
                    {
                        GripPoint = linearEntity.EndPoint +
                                    ((linearEntity.InsertionPoint - linearEntity.EndPoint).GetNormal() * 20 * curViewUnitSize)
                    };
                    yield return addVertexGrip;

                    addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, null, linearEntity.EndPoint)
                    {
                        GripPoint = linearEntity.InsertionPoint +
                                    ((linearEntity.EndPoint - linearEntity.InsertionPoint).GetNormal() * 20 * curViewUnitSize)
                    };
                    yield return addVertexGrip;

                    addVertexGrip = new LinearEntityAddVertexGrip(smartEntity, linearEntity.InsertionPoint, linearEntity.EndPoint)
                    {
                        GripPoint = GeometryUtils.GetMiddlePoint3d(linearEntity.InsertionPoint, linearEntity.EndPoint)
                    };
                    yield return addVertexGrip;
                }
            }

            #endregion

            var reverseGrip = new LinearEntityReverseGrip(smartEntity)
            {
                GripPoint = linearEntity.InsertionPoint + (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return reverseGrip;

            reverseGrip = new LinearEntityReverseGrip(smartEntity)
            {
                GripPoint = linearEntity.EndPoint + (Vector3d.YAxis * 20 * curViewUnitSize)
            };
            yield return reverseGrip;
        }

        /// <summary>
        /// Обработка ручек в методе MoveGripPointsAt класса <see cref="GripOverrule"/> для линейных интеллектуальных объектов
        /// </summary>
        /// <param name="entity">Примитив AutoCAD</param>
        /// <param name="grips">Коллекция ручек</param>
        /// <param name="offset">Смещение ручки</param>
        /// <param name="baseAction">Базовое действие метода MoveGripPointsAt для ручки</param>
        public static void LinearEntityGripPointMoveProcess(
            Entity entity, GripDataCollection grips, Vector3d offset, Action baseAction)
        {
            foreach (var gripData in grips)
            {
                if (gripData is LinearEntityVertexGrip vertexGrip)
                {
                    var intellectualEntity = vertexGrip.SmartEntity;

                    if (vertexGrip.GripIndex == 0)
                    {
                        ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                        intellectualEntity.InsertionPoint = vertexGrip.GripPoint + offset;
                    }
                    else if (vertexGrip.GripIndex == ((ILinearEntity)intellectualEntity).MiddlePoints.Count + 1)
                    {
                        intellectualEntity.EndPoint = vertexGrip.GripPoint + offset;
                    }
                    else
                    {
                        ((ILinearEntity)intellectualEntity).MiddlePoints[vertexGrip.GripIndex - 1] =
                            vertexGrip.GripPoint + offset;
                    }

                    // Вот тут происходит перерисовка примитивов внутри блока
                    intellectualEntity.UpdateEntities();
                    intellectualEntity.BlockRecord.UpdateAnonymousBlocks();
                }
                else if (gripData is LinearEntityAddVertexGrip addVertexGrip)
                {
                    addVertexGrip.NewPoint = addVertexGrip.GripPoint + offset;
                }
                else
                {
                    baseAction.Invoke();
                }
            }
        }

        /// <summary>
        /// Возвращает геометрическую длину однострочного текста
        /// </summary>
        /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
        public static double GetLength(this DBText dbText)
        {
            return Math.Abs(dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X);
        }

        /// <summary>
        /// Возвращает геометрическую высоту однострочного текста
        /// </summary>
        /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
        public static double GetHeight(this DBText dbText)
        {
            return Math.Abs(dbText.GeometricExtents.MaxPoint.Y - dbText.GeometricExtents.MinPoint.Y);
        }

        /// <summary>
        /// Возвращает маскировку, созданную по контуру текста с указанным отступом
        /// </summary>
        /// <param name="dbText">Экземпляр <see cref="DBText"/></param>
        /// <param name="offset">Отступ</param>
        public static Wipeout GetBackgroundMask(this DBText dbText, double offset)
        {
            if (dbText == null)
                return null;
            
            return GetBackgroundMask(dbText.GeometricExtents, offset);
        }
        
        /// <summary>
        /// Возвращает маскировку, созданную по контуру текста с указанным отступом
        /// </summary>
        /// <param name="mText">Экземпляр <see cref="DBText"/></param>
        /// <param name="offset">Отступ</param>
        public static Wipeout GetBackgroundMask(this MText mText, double offset)
        {
            if (mText == null)
                return null;

            return GetBackgroundMask(mText.GeometricExtents, offset);
        }

        /// <summary>
        /// Возвращает маскировку, созданную по контуру с указанным отступом
        /// </summary>
        /// <param name="extents3d">Крайние границы</param>
        /// <param name="offset">Отступ</param>
        private static Wipeout GetBackgroundMask(Extents3d extents3d, double offset)
        {
            try
            {
                var minPoint = extents3d.MinPoint;
                var maxPoint = extents3d.MaxPoint;
                var bottomLeftPoint = new Point2d(minPoint.X - offset, minPoint.Y - offset);
                var topLeftPoint = new Point2d(minPoint.X - offset, maxPoint.Y + offset);
                var topRightPoint = new Point2d(maxPoint.X + offset, maxPoint.Y + offset);
                var bottomRightPoint = new Point2d(maxPoint.X + offset, minPoint.Y - offset);

                var wipeout = new Wipeout();
                wipeout.SetFrom(
                    new Point2dCollection
                    {
                        bottomLeftPoint, topLeftPoint, topRightPoint, bottomRightPoint, bottomLeftPoint
                    }, Vector3d.ZAxis);
                return wipeout;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                if (ex.Message == "eNullExtents")
                {
                    return null;
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Возвращает номер узла в зависимости от номера узла последнего созданного объекта
        /// </summary>
        /// <param name="lastNodeNumber">Номер узла последнего созданного объекта</param>
        /// <param name="cachedNodeNumber">Кэш номера узла для корректного обновления объекта</param>
        public static string GetNodeNumberByLastNodeNumber(string lastNodeNumber, ref string cachedNodeNumber)
        {
            var number = "1";

            if (!string.IsNullOrEmpty(lastNodeNumber))
            {
                if (int.TryParse(lastNodeNumber, out var i))
                {
                    cachedNodeNumber = (i + 1).ToString();
                }
                else if (Invariables.AxisRusAlphabet.Contains(lastNodeNumber))
                {
                    var index = Invariables.AxisRusAlphabet.IndexOf(lastNodeNumber);
                    cachedNodeNumber = index == Invariables.AxisRusAlphabet.Count - 1
                        ? Invariables.AxisRusAlphabet[0]
                        : Invariables.AxisRusAlphabet[index + 1];
                }
                else if (Invariables.AxisEngAlphabet.Contains(lastNodeNumber))
                {
                    var index = Invariables.AxisEngAlphabet.IndexOf(lastNodeNumber);
                    cachedNodeNumber = index == Invariables.AxisEngAlphabet.Count - 1
                        ? Invariables.AxisEngAlphabet[0]
                        : Invariables.AxisEngAlphabet[index + 1];
                }
            }

            if (!string.IsNullOrEmpty(cachedNodeNumber))
            {
                number = cachedNodeNumber;
            }

            return number;
        }
    }
}
