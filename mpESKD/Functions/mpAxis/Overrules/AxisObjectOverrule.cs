namespace mpESKD.Functions.mpAxis.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class AxisObjectOverrule : ObjectOverrule
    {
        private static AxisObjectOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AxisObjectOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new AxisObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(Axis.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc/>
        public override void Close(DBObject dbObject)
        {
            Debug.Print("AxisObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<Axis>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc />
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, Axis.GetDescriptor().Name, true);
        }
    }
}