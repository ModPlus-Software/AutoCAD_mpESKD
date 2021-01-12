namespace mpESKD.Functions.mpWaterProofing
{
    using System.Collections.Generic;
    using Base;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class WaterProofingDescriptor : IIntellectualEntityDescriptor
    {
        private static WaterProofingDescriptor _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static WaterProofingDescriptor Instance => _instance ?? (_instance = new WaterProofingDescriptor());

        /// <inheritdoc />
        public string Name => "mpWaterProofing";

        /// <inheritdoc />
        // "Гидроизоляция";
        public string LName => Language.GetItem(Invariables.LangItem, "h114");

        /// <inheritdoc />
        // Создание линии обозначения гидроизоляции
        public string Description => Language.GetItem(Invariables.LangItem, "h115");

        /// <inheritdoc />
        // Создание интеллектуального объекта на основе анонимного блока, описывающего линию гидроизоляции
        public string FullDescription => Language.GetItem(Invariables.LangItem, "h116");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpWaterProofingFromPolyline"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // Гидроизоляция из полилинии
            Language.GetItem(Invariables.LangItem, "h117")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // Конвертирование выбранной полилинии в линию обозначения гидроизоляции
            Language.GetItem(Invariables.LangItem, "h118")
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
