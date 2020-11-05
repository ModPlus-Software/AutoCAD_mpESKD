namespace mpESKD.Functions.mpGroundLine
{
    using System.Collections.Generic;
    using Base;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class GroundLineDescriptor : IIntellectualEntityDescriptor
    {
        private static GroundLineDescriptor _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GroundLineDescriptor Instance => _instance ?? (_instance = new GroundLineDescriptor());

        /// <inheritdoc />
        public string Name => "mpGroundLine";

        /// <inheritdoc />
        public string LName => Language.GetItem(Invariables.LangItem, "h73"); // "Линия грунта";

        /// <inheritdoc />
        public string Description => Language.GetItem(Invariables.LangItem, "h74"); // "Отрисовка линии обозначения грунта";

        /// <inheritdoc />
        public string FullDescription => Language.GetItem(Invariables.LangItem, "h75"); // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию грунта";

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpGroundLineFromPolyline"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // "Линия грунта из полилинии"
            Language.GetItem(Invariables.LangItem, "h76")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Конвертирование выбранной полилинии в линию обозначения грунта"
            Language.GetItem(Invariables.LangItem, "h77")
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>
        {
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>
        {
            string.Empty
        };
    }
}
