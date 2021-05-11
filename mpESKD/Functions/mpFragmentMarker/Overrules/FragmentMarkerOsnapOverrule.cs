namespace mpESKD.Functions.mpFragmentMarker.Overrules
{
    using System;
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base.Utils;

    /// <inheritdoc />
    public class FragmentMarkerOsnapOverrule : OsnapOverrule
    {
        private static FragmentMarkerOsnapOverrule _fragmentMakerOsnapOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FragmentMarkerOsnapOverrule Instance()
        {
            if (_fragmentMakerOsnapOverrule != null)
            {
                return _fragmentMakerOsnapOverrule;
            }

            _fragmentMakerOsnapOverrule = new FragmentMarkerOsnapOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _fragmentMakerOsnapOverrule.SetXDataFilter(FragmentMarker.GetDescriptor().Name);
            return _fragmentMakerOsnapOverrule;
        }

        /// <inheritdoc />
        public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint,
            Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
        {
            Debug.Print("FragmentMakerOsnapOverrule");
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
            return ExtendedDataUtils.IsApplicable(overruledSubject, FragmentMarker.GetDescriptor().Name);
        }
    }
}