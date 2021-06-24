namespace mpESKD.Functions.mpViewLabel
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    class ViewLabelDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(ViewLabel);

        /// <inheritdoc />
        public string Name => "mpViewLabel";

        /// <inheritdoc />
        // Вид
        public string LName => Language.GetItem("h153");

        /// <inheritdoc />
        // Создание обозначения вида (разреза)
        public string Description => Language.GetItem("h154");

        /// <inheritdoc />
        // Создание интеллектуального объекта вида или разреза на основе анонимного блока
        public string FullDescription => Language.GetItem("h155");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {

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
