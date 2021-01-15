namespace mpESKD.Functions.mpLevelMark.Overrules
{
    using System;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base.Utils;

    /// <inheritdoc />
    public class LevelMarkOsnapOverrule : OsnapOverrule
    {
        private static LevelMarkOsnapOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static LevelMarkOsnapOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new LevelMarkOsnapOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(LevelMark.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void GetObjectSnapPoints(
            Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
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

        /// <summary>
        /// Проверка валидности примитива. Проверка происходит по наличию XData с определенным AppName
        /// </summary>
        /// <param name="overruledSubject">Instance of <see cref="RXObject"/></param>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, LevelMark.GetDescriptor().Name);
        }
    }
}