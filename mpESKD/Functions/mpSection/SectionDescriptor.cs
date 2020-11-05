namespace mpESKD.Functions.mpSection
{
    using System.Collections.Generic;
    using Base;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class SectionDescriptor : IIntellectualEntityDescriptor
    {
        private static SectionDescriptor _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static SectionDescriptor Instance => _instance ?? (_instance = new SectionDescriptor());

        /// <inheritdoc />
        public string Name => "mpSection";

        /// <inheritdoc />
        // Разрез
        public string LName => Language.GetItem(Invariables.LangItem, "h79");

        /// <inheritdoc />
        // Создание обозначения разреза (сечения) по ГОСТ 2.305-68
        public string Description => Language.GetItem(Invariables.LangItem, "h80");

        /// <inheritdoc />
        // Создание интеллектуального объекта на основе анонимного блока, описывающего разрез (сечение) по ГОСТ 2.305-68
        public string FullDescription => Language.GetItem(Invariables.LangItem, "h81");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpSectionBroken",
            "mpSectionFromPolyline"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // Ломаный разрез
            Language.GetItem(Invariables.LangItem, "h82"),

            // Разрез из полилинии
            Language.GetItem(Invariables.LangItem, "h83")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // Отрисовка обозначения ломаного разреза (сечения) по ГОСТ 2.305-68
            Language.GetItem(Invariables.LangItem, "h84"),

            // Конвертирование выбранной полилинии в обозначение разреза
            Language.GetItem(Invariables.LangItem, "h85")
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>
        {
            string.Empty,
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>
        {
            string.Empty,
            string.Empty
        };
    }
}
