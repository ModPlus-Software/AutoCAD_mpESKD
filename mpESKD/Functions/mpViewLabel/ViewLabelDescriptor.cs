namespace mpESKD.Functions.mpViewLabel
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class ViewLabelDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(ViewLabel);

        /// <inheritdoc />
        public string Name => "mpViewLabel";

        /// <inheritdoc />
        // Обозначение вида
        public string LName => Language.GetItem("h153");

        /// <inheritdoc />
        // Создание обозначения вида
        public string Description => Language.GetItem("h154");

        /// <inheritdoc />
        // Создание интеллектуального объекта обозначения вида на основе анонимного блока
        public string FullDescription => Language.GetItem("h155");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpSectionLabel"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // "Обозначение разреза"
            Language.GetItem("h158"),
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Создание обозначения разреза"
            Language.GetItem("h159")
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>
        {
            // "Создание интеллектуального объекта обозначения разреза на основе анонимного блока"
            Language.GetItem("h160"),
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>
        {
            string.Empty,
        };
    }
}
