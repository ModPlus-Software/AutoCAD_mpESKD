namespace mpESKD.Functions.mpView
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class ViewDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(View);

        /// <inheritdoc />
        public string Name => "mpView";

        /// <inheritdoc />
        // Вид
        public string LName => Language.GetItem("h167");

        /// <inheritdoc />
        // Создание обозначения вида по ГОСТ 2.305-68
        public string Description => Language.GetItem("h169");

        /// <inheritdoc />
        // Создание интеллектуального объекта на основе анонимного блока, описывающего вида по ГОСТ 2.305-68
        public string FullDescription => Language.GetItem("h170");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            string.Empty
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
