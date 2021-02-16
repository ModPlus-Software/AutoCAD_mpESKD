namespace mpESKD.Functions.mpWeldJoint.Overrules
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    public class WeldJointGripPointOverrule : GripOverrule
    {
        private static WeldJointGripPointOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static WeldJointGripPointOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new WeldJointGripPointOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(WeldJoint.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void GetGripPoints(
            Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            try
            {
                if (IsApplicable(entity))
                {
                    // Удаляю все ручки - это удалит ручку вставки блока
                    grips.Clear();

                    var weldJoint = EntityReaderService.Instance.GetFromEntity<WeldJoint>(entity);
                    if (weldJoint != null)
                    {
                        foreach (var grip in EntityUtils.GetLinearEntityGeneralGrips(weldJoint, curViewUnitSize))
                        {
                            grips.Add(grip);
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
                    EntityUtils.LinearEntityGripPointMoveProcess(
                        entity, grips, offset, () => base.MoveGripPointsAt(entity, grips, offset, bitFlags));
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
            return ExtendedDataUtils.IsApplicable(overruledSubject, WeldJoint.GetDescriptor().Name);
        }
    }
}
