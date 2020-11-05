namespace mpESKD.Functions.mpBreakLine
{
    using System.Collections.Generic;
    using Base;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class BreakLineDescriptor : IIntellectualEntityDescriptor
    {
        private static BreakLineDescriptor _instance;

        public static BreakLineDescriptor Instance => _instance ?? (_instance = new BreakLineDescriptor());

        /// <inheritdoc />
        public string Name => "mpBreakLine";

        /// <inheritdoc />
        // "Линия обрыва"
        public string LName => Language.GetItem(Invariables.LangItem, "h48");

        /// <inheritdoc />
        // "Создание линии обрыва по ГОСТ 2.303-68"
        public string Description => Language.GetItem(Invariables.LangItem, "h56");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию обрыва по ГОСТ 2.303-68, путем указания двух точек"
        public string FullDescription => Language.GetItem(Invariables.LangItem, "h57");

        /// <inheritdoc />
        public string ToolTipHelpImage => "Linear.png";

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpBreakLineCurve", 
            "mpBreakLineCylinder"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // "Криволинейный обрыв"
            Language.GetItem(Invariables.LangItem, "h58"),

            // "Цилиндрический обрыв"
            Language.GetItem(Invariables.LangItem, "h59")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Создание криволинейного обрыва"
            Language.GetItem(Invariables.LangItem, "h60"),

            // "Создание цилиндрического обрыва"
            Language.GetItem(Invariables.LangItem, "h61")
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>
        {
            // "Создание интеллектуального объекта на основе анонимного блока, описывающего криволинейный обрыв, путем указания двух точек"
            Language.GetItem(Invariables.LangItem, "h62"),

            // "Создание интеллектуального объекта на основе анонимного блока, описывающего цилиндрический обрыв, путем указания двух точек"
            Language.GetItem(Invariables.LangItem, "h63")
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string> { "Curvilinear.png", "Cylindrical.png" };
    }
}
