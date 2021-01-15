namespace mpESKD.Functions.mpWaterProofing.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class WaterProofingObjectOverrule : ObjectOverrule
    {
        private static WaterProofingObjectOverrule _groundLineObjectOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        /// <returns></returns>
        public static WaterProofingObjectOverrule Instance()
        {
            if (_groundLineObjectOverrule != null)
            {
                return _groundLineObjectOverrule;
            }

            _groundLineObjectOverrule = new WaterProofingObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _groundLineObjectOverrule.SetXDataFilter(WaterProofing.GetDescriptor().Name);
            return _groundLineObjectOverrule;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("WaterProofingObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<WaterProofing>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc/>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, WaterProofing.GetDescriptor().Name, true);
        }
    }
}
