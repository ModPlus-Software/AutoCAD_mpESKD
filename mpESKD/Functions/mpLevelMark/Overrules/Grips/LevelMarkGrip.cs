namespace mpESKD.Functions.mpLevelMark.Overrules.Grips
{
    using System.Collections.Generic;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Обычная ручка высотной отметки
    /// </summary>
    public class LevelMarkGrip : SmartEntityGripData
    {
        private readonly List<Point3d> _points;

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelMarkGrip"/> class.
        /// </summary>
        /// <param name="levelMark">Экземпляр класса <see cref="mpLevelMark.LevelMark"/></param>
        /// <param name="gripType">Вид ручки</param>
        /// <param name="gripName">Имя ручки</param>
        /// <param name="gripPoint">Точка ручки</param>
        public LevelMarkGrip(
            LevelMark levelMark,
            GripType gripType,
            LevelMarkGripName gripName,
            Point3d gripPoint)
        {
            LevelMark = levelMark;
            GripName = gripName;
            GripType = gripType;
            GripPoint = gripPoint;

            /* При инициализации ручки нужно собрать все точки и поместить их в поле _points.
             * Это создаст кэш точек. Если в методе WorldDraw брать точки из самого объекта (LevelMark),
             * то вспомогательные линии будут меняться при зуммировании. Это связано с тем, что в методе
             * MoveGripPointsAt происходит вызов метода UpdateEntities */
            _points = new List<Point3d>
            {
                LevelMark.InsertionPoint,
                LevelMark.ObjectPoint,
                LevelMark.BottomShelfStartPoint
            };
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpLevelMark.LevelMark"/>
        /// </summary>
        public LevelMark LevelMark { get; }

        /// <summary>
        /// Имя ручки
        /// </summary>
        public LevelMarkGripName GripName { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            // Переместить
            return Language.GetItem(Invariables.LangItem, "gp1");
        }

        // Временное значение ручки
        private Point3d _gripTmp;

        /// <inheritdoc />
        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
        {
            try
            {
                // При начале перемещения запоминаем первоначальное положение ручки
                // Запоминаем начальные значения
                if (newStatus == Status.GripStart)
                {
                    _gripTmp = GripPoint;
                }

                // При удачном перемещении ручки записываем новые значения в расширенные данные
                // По этим данным я потом получаю экземпляр класса LevelMark
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(LevelMark.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = LevelMark.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    LevelMark.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    if (_gripTmp != null)
                    {
                        switch (GripName)
                        {
                            case LevelMarkGripName.BasePoint:
                                LevelMark.InsertionPoint = _gripTmp;
                                break;
                            case LevelMarkGripName.ObjectPoint:
                                LevelMark.ObjectPoint = _gripTmp;
                                break;
                            case LevelMarkGripName.BottomShelfStartPoint:
                                LevelMark.BottomShelfStartPoint = _gripTmp;
                                break;
                            case LevelMarkGripName.ArrowPoint:
                                LevelMark.EndPoint = _gripTmp;
                                break;
                            case LevelMarkGripName.TopShelfPoint:
                                LevelMark.ShelfPoint = _gripTmp;
                                break;
                        }
                    }
                }

                base.OnGripStatusChanged(entityId, newStatus);
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }

        /// <inheritdoc />
        public override bool WorldDraw(
            WorldDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, double dGripSize)
        {
            if (MainSettings.Instance.LevelMarkShowHelpLinesOnSelection)
            {
                var backupColor = worldDraw.SubEntityTraits.Color;
                var backupFillType = worldDraw.SubEntityTraits.FillType;

                worldDraw.SubEntityTraits.FillType = FillType.FillAlways;
                worldDraw.SubEntityTraits.Color = 40;
                worldDraw.Geometry.WorldLine(_points[0], _points[1]);
                worldDraw.Geometry.WorldLine(_points[1], _points[2]);

                // restore
                worldDraw.SubEntityTraits.Color = backupColor;
                worldDraw.SubEntityTraits.FillType = backupFillType;
            }

            return base.WorldDraw(worldDraw, entityId, type, imageGripPoint, dGripSize);
        }
    }
}
