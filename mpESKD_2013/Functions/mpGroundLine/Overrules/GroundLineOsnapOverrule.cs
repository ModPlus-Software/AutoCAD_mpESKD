﻿// ReSharper disable InconsistentNaming
namespace mpESKD.Functions.mpGroundLine.Overrules
{
    using System;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Helpers;
    using ModPlusAPI.Windows;

    public class GroundLineOsnapOverrule : OsnapOverrule
    {
        protected static GroundLineOsnapOverrule _groundLineOsnapOverrule;

        public static GroundLineOsnapOverrule Instance()
        {
            if (_groundLineOsnapOverrule != null) return _groundLineOsnapOverrule;
            _groundLineOsnapOverrule = new GroundLineOsnapOverrule();
            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _groundLineOsnapOverrule.SetXDataFilter(GroundLineInterface.Name);
            return _groundLineOsnapOverrule;
        }

        public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
            Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
        {
            // Проверка дополнительных условий
            if (IsApplicable(entity))
            {
                try
                {
                    // todo old
                    // GroundLine.GetGroundLineFromEntity(entity);
                    var groundLine = EntityReaderFactory.Instance.GetFromEntity<GroundLine>(entity); 
                    if (groundLine != null)
                    {
                        snapPoints.Add(groundLine.InsertionPoint);
                        groundLine.MiddlePoints.ForEach(p => snapPoints.Add(p));
                        snapPoints.Add(groundLine.EndPoint);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }
            else base.GetObjectSnapPoints(entity, snapMode, gsSelectionMark, pickPoint, lastPoint, viewTransform, snapPoints, geometryIds);
        }

        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataHelpers.IsApplicable(overruledSubject, GroundLineInterface.Name);
        }
    }
}