namespace mpESKD.Functions.mpBreakLine.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class BreakLineObjectOverrule : ObjectOverrule
    {
        private static BreakLineObjectOverrule _breakLineObjectOverrule;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static BreakLineObjectOverrule Instance()
        {
            if (_breakLineObjectOverrule != null)
            {
                return _breakLineObjectOverrule;
            }

            _breakLineObjectOverrule = new BreakLineObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _breakLineObjectOverrule.SetXDataFilter(BreakLine.GetDescriptor().Name);
            return _breakLineObjectOverrule;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print(dbObject?.GetRXClass().Name);
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<BreakLine>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <inheritdoc/>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, BreakLine.GetDescriptor().Name, true);
        }
    }
}