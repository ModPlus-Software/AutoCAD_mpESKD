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
        // "Буквенная линия";
        public string LName => Language.GetItem("h161");

        /// <inheritdoc />
        // "Отрисовка обозначения буквенной линии";
        public string Description => Language.GetItem("h163");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего буквенную линию";
        public string FullDescription => Language.GetItem("h164");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new ()
        {
            "mpLetterLineFromPolyline"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new ()
        {
            // "Буквенная линия  из полилинии"
            Language.GetItem("h165")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new ()
        {
            // "Конвертирование выбранной полилинии в буквенную линию"
            Language.GetItem("h166")
        };

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new ()
        {
            string.Empty
        };

        /// <inheritdoc />
        public List<string> SubHelpImages => new ()
        {
            string.Empty
        };
    }
}
