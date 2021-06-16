namespace mpESKD.Functions.mpFragmentMarker
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class FragmentMarkerDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(FragmentMarker);

        /// <inheritdoc />
        public string Name => "mpFragmentMarker";

        /// <inheritdoc />
        // "обозначение фрагмента
        public string LName => Language.GetItem("h145");

        /// <inheritdoc />
        // "Создание линии фрагмента"
        public string Description => Language.GetItem("h147");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию фрагмента, путем указания двух точек"
        public string FullDescription => Language.GetItem("h148");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpFragmentMarker"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // "Обозначение фрагмента"
            Language.GetItem("h145"),
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Создание фрагмента"
            Language.GetItem("h147"),
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>
        {
            // "Создание интеллектуального объекта на основе анонимного блока, описывающего криволинейный обрыв, путем указания двух точек"
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string> { string.Empty};
    }
}
