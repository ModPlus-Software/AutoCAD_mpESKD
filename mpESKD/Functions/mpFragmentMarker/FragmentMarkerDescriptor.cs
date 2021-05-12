namespace mpESKD.Functions.mpFragmentMarker
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class FragmentMarkerDescriptor : IIntellectualEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(FragmentMarker);

        /// <inheritdoc />
        public string Name => "mpFragmentMarker";

        /// <inheritdoc />
        // "обозначение фрагмента
        public string LName => Language.GetItem("h48");

        /// <inheritdoc />
        // "Создание линии фрагмента"
        public string Description => Language.GetItem("h56");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию фрагмента, путем указания двух точек"
        public string FullDescription => Language.GetItem("h57");

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
            Language.GetItem("h58"),
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Создание фрагмента"
            Language.GetItem("h60"),
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
