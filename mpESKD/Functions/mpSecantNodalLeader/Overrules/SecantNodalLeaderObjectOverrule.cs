namespace mpESKD.Functions.mpSecantNodalLeader.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class SecantNodalLeaderObjectOverrule : ObjectOverrule
    {
        private static SecantNodalLeaderObjectOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static SecantNodalLeaderObjectOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new SecantNodalLeaderObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(SecantNodalLeader.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("SecantNodalLeaderObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<SecantNodalLeader>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <summary>
        /// Проверка валидности примитива. Проверка происходит по наличию XData с определенным AppName
        /// </summary>
        /// <param name="overruledSubject">Instance of <see cref="RXObject"/></param>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, SecantNodalLeader.GetDescriptor().Name, true);
        }
    }
}
