namespace mpESKD.Functions.mpAxis
{
    using System.Collections.Generic;
    using Base;
    using Base.Abstractions;
    using ModPlusAPI;

    public class AxisDescriptor : IIntellectualEntityDescriptor
    {
        private static AxisDescriptor _instance;

        public static AxisDescriptor Instance => _instance ?? (_instance = new AxisDescriptor());

        /// <inheritdoc />
        public string Name => "mpAxis";

        /// <inheritdoc />
        public string LName => Language.GetItem(Invariables.LangItem, "h41"); // "Прямая ось";

        /// <inheritdoc />
        public string Description => Language.GetItem(Invariables.LangItem, "h65"); // "Отрисовка прямой оси по ГОСТ 21.101-97";

        /// <inheritdoc />
        public string FullDescription => Language.GetItem(Invariables.LangItem, "h66"); // "Создание интеллектуального объекта на основе анонимного блока, описывающего прямую ось по ГОСТ 21.101-97, путем указания двух точек";

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>();

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>();

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>();
    }
}
