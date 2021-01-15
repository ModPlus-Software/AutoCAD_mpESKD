namespace mpESKD.Functions.mpWaterProofing
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class WaterProofingDescriptor : IIntellectualEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(WaterProofing);

        /// <inheritdoc />
        public string Name => "mpWaterProofing";

        /// <inheritdoc />
        // "Гидроизоляция";
        public string LName => Language.GetItem("h114");

        /// <inheritdoc />
        // Создание линии обозначения гидроизоляции
        public string Description => Language.GetItem("h115");

        /// <inheritdoc />
        // Создание интеллектуального объекта на основе анонимного блока, описывающего линию гидроизоляции
        public string FullDescription => Language.GetItem("h116");

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
            Language.GetItem("h117")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // Конвертирование выбранной полилинии в линию обозначения гидроизоляции
            Language.GetItem("h118")
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
