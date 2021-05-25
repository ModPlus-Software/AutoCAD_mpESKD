namespace mpESKD.Functions.mpFragmentMarker.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class FragmentMarkerObjectOverrule : ObjectOverrule
    {
        private static FragmentMarkerObjectOverrule _breakLineObjectOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FragmentMarkerObjectOverrule Instance()
        {
            if (_breakLineObjectOverrule != null)
            {
                return _breakLineObjectOverrule;
            }

            _breakLineObjectOverrule = new FragmentMarkerObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _breakLineObjectOverrule.SetXDataFilter(FragmentMarker.GetDescriptor().Name);
            return _breakLineObjectOverrule;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print(dbObject?.GetRXClass().Name);
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<FragmentMarker>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc/>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, FragmentMarker.GetDescriptor().Name, true);
        }
    }
}