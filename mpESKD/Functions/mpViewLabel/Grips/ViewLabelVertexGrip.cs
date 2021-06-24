﻿namespace mpESKD.Functions.mpViewLabel.Grips
{
    using System.Collections.Generic;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using Autodesk.AutoCAD.Runtime;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using ViewLabel = mpViewLabel.ViewLabel;

    /// <summary>
    /// Ручка вершин
    /// </summary>
    public class ViewLabelVertexGrip : SmartEntityGripData
    {
        private readonly List<Point3d> _points;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewLabelVertexGrip"/> class.
        /// </summary>
        /// <param name="viewLabel">Экземпляр класса <see cref="mpViewLabel.ViewLabel"/></param>
        /// <param name="index">Индекс ручки</param>
        public ViewLabelVertexGrip(ViewLabel viewLabel, int index)
        {
            ViewLabel = viewLabel;
            GripIndex = index;
            GripType = GripType.Point;
            
            /* При инициализации ручки нужно собрать все точки разреза и поместить их в поле _points.
             * Это создаст кэш точек разреза. Если в методе WorldDraw брать точки из самого разреза (Section),
             * то вспомогательные линии будут меняться при зуммировании. Это связано с тем, что в методе
             * MoveGripPointsAt происходит вызов метода UpdateEntities */
            _points = new List<Point3d> { ViewLabel.InsertionPoint };
            _points.Add(ViewLabel.EndPoint);
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpViewLabel.ViewLabel"/>
        /// </summary>
        public ViewLabel ViewLabel { get; }

        /// <summary>
        /// Индекс ручки
        /// </summary>
        public int GripIndex { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem("gp1"); // stretch
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
                // По этим данным я потом получаю экземпляр класса section
                if (newStatus == Status.GripEnd)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(ViewLabel.BlockId, OpenMode.ForWrite, true, true);
                        using (var resBuf = ViewLabel.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    ViewLabel.Dispose();
                }

                // При отмене перемещения возвращаем временные значения
                if (newStatus == Status.GripAbort)
                {
                    if (_gripTmp != null)
                    {
                        if (GripIndex == 0)
                        {
                            ViewLabel.InsertionPoint = _gripTmp;
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
        public override bool WorldDraw(WorldDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, double dGripSize)
        {
            if (GripIndex > 0 && MainSettings.Instance.SectionShowHelpLineOnSelection)
            {
                var backupColor = worldDraw.SubEntityTraits.Color;
                var backupFillType = worldDraw.SubEntityTraits.FillType;

                worldDraw.SubEntityTraits.FillType = FillType.FillAlways;
                worldDraw.SubEntityTraits.Color = 40;
                worldDraw.Geometry.WorldLine(_points[GripIndex - 1], _points[GripIndex]);

                // restore
                worldDraw.SubEntityTraits.Color = backupColor;
                worldDraw.SubEntityTraits.FillType = backupFillType;
            }

            return base.WorldDraw(worldDraw, entityId, type, imageGripPoint, dGripSize);
        }
    }
}