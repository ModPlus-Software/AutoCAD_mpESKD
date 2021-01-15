namespace mpESKD.Functions.mpWaterProofing.Overrules
{
    using System;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base.Utils;

    /// <inheritdoc />
    public class WaterProofingOsnapOverrule : OsnapOverrule
    {
        private static WaterProofingOsnapOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static WaterProofingOsnapOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new WaterProofingOsnapOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(WaterProofing.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
            Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
        {
            Debug.Print("WaterProofingOsnapOverrule");
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
            return ExtendedDataUtils.IsApplicable(overruledSubject, WaterProofing.GetDescriptor().Name);
        }
    }
}
