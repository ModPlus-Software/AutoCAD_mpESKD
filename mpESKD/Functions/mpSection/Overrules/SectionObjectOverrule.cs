namespace mpESKD.Functions.mpSection.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class SectionObjectOverrule : ObjectOverrule
    {
        private static SectionObjectOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static SectionObjectOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new SectionObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(mpSection.Section.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("SectionObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<mpSection.Section>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc/>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, mpSection.Section.GetDescriptor().Name, true);
        }
    }
}