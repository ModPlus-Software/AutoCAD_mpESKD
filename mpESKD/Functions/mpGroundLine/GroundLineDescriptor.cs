namespace mpESKD.Functions.mpGroundLine
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class GroundLineDescriptor : IIntellectualEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(GroundLine);

        /// <inheritdoc />
        public string Name => "mpGroundLine";

        /// <inheritdoc />
        // "Линия грунта";
        public string LName => Language.GetItem("h73");

        /// <inheritdoc />
        // "Отрисовка линии обозначения грунта";
        public string Description => Language.GetItem("h74");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию грунта";
        public string FullDescription => Language.GetItem("h75");

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
            Language.GetItem("h76")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Конвертирование выбранной полилинии в линию обозначения грунта"
            Language.GetItem("h77")
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
