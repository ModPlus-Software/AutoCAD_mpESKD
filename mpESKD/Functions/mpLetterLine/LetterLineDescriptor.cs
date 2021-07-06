namespace mpESKD.Functions.mpLetterLine
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class LetterLineDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(LetterLine);

        /// <inheritdoc />
        public string Name => "mpLetterLine";

        /// <inheritdoc />
        // "Линия грунта";
        public string LName => Language.GetItem("h73"); //Todo correct

        /// <inheritdoc />
        // "Отрисовка линии обозначения грунта";
        public string Description => Language.GetItem("h74"); //Todo correct

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего линию грунта";
        public string FullDescription => Language.GetItem("h75"); //Todo correct

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpLetterLineFromPolyline"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // "Линия грунта из полилинии"
            Language.GetItem("h76") //Todo correct
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // "Конвертирование выбранной полилинии в линию обозначения грунта"
            Language.GetItem("h77") //Todo correct
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
