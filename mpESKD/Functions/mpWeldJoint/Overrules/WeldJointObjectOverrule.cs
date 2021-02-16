namespace mpESKD.Functions.mpWeldJoint.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class WeldJointObjectOverrule : ObjectOverrule
    {
        private static WeldJointObjectOverrule _groundLineObjectOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        /// <returns></returns>
        public static WeldJointObjectOverrule Instance()
        {
            if (_groundLineObjectOverrule != null)
            {
                return _groundLineObjectOverrule;
            }

            _groundLineObjectOverrule = new WeldJointObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _groundLineObjectOverrule.SetXDataFilter(WeldJoint.GetDescriptor().Name);
            return _groundLineObjectOverrule;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("WeldJointObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<WeldJoint>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc/>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, WeldJoint.GetDescriptor().Name, true);
        }
    }
}
