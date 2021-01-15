namespace mpESKD.Functions.mpLevelMark.Overrules
{
    using System.Diagnostics;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Utils;

    /// <inheritdoc />
    public class LevelMarkObjectOverrule : ObjectOverrule
    {
        private static LevelMarkObjectOverrule _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static LevelMarkObjectOverrule Instance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = new LevelMarkObjectOverrule();

            // Фильтр "отлова" примитива по расширенным данным. Работает лучше, чем проверка вручную!
            _instance.SetXDataFilter(LevelMark.GetDescriptor().Name);
            return _instance;
        }

        /// <inheritdoc />
        public override void Close(DBObject dbObject)
        {
            Debug.Print("LevelMarkObjectOverrule");
            if (IsApplicable(dbObject))
            {
                EntityUtils.ObjectOverruleProcess(
                    dbObject, () => EntityReaderService.Instance.GetFromEntity<LevelMark>(dbObject));
            }

            base.Close(dbObject);
        }

        /// <summary>
        /// Проверка валидности примитива. Проверка происходит по наличию XData с определенным AppName
        /// </summary>
        /// <param name="overruledSubject">Instance of <see cref="RXObject"/></param>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            return ExtendedDataUtils.IsApplicable(overruledSubject, LevelMark.GetDescriptor().Name, true);
        }
    }
}