namespace mpESKD.Functions.mpNodalLeader.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class NodalLeaderObjectOverrule : ObjectOverrule
    {
        private static NodalLeaderObjectOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static NodalLeaderObjectOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new NodalLeaderObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(NodalLeader.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("NodalLeaderObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<NodalLeader>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <summary>
        /// Проверка валидности примитива. Проверка происходит по наличию XData с определенным AppName
        /// </summary>
        /// <param name="overruledSubject">Instance of <see cref="RXObject"/></param>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, NodalLeader.GetDescriptor().Name, true);
        }
    }
}
