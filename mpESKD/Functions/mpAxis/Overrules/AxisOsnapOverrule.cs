namespace mpESKD.Functions.mpAxis.Overrules
{
    using System;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base.Utils;

    /// <inheritdoc />
    public class AxisOsnapOverrule : OsnapOverrule
    {
        private static AxisOsnapOverrule _breakLineOsnapOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AxisOsnapOverrule Instance()
        {
            if (_breakLineOsnapOverrule != null)
            {
                return _breakLineOsnapOverrule;
            }

            _breakLineOsnapOverrule = new AxisOsnapOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _breakLineOsnapOverrule.SetXDataFilter(Axis.GetDescriptor().Name);
            return _breakLineOsnapOverrule;
        }

        /// <inheritdoc />
        public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
            Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
        {
            Debug.Print("AxisOsnapOverrule");
            if (IsApplicable(entity))
            {
                EntityUtils.OsnapOverruleProcess(entity, snapPoints);
            }
            else
            {
                base.GetObjectSnapPoints(entity, snapMode, gsSelectionMark, pickPoint, lastPoint, viewTransform, snapPoints, geometryIds);
            }
        }

        /// <inheritdoc />
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, Axis.GetDescriptor().Name);
        }
    }
}
