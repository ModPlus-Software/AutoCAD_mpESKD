namespace mpESKD.Functions.mpLevelMark
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class LevelMarkDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(LevelMark);

        /// <inheritdoc />
        public string Name => "mpLevelMark";

        /// <inheritdoc />
        /// Отметка уровня
        public string LName => Language.GetItem("h105");

        /// <inheritdoc />
        /// Создание высотной отметки по ГОСТ 21.101-97
        public string Description => Language.GetItem("h106");

        /// <inheritdoc />
        /// Создание интеллектуального объекта на основе анонимного блока, описывающего высотную отметку по ГОСТ 21.101-97
        public string FullDescription => Language.GetItem("h107");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>
        {
            "mpLevelMarkAlign"
        };

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>
        {
            // Выравнивание отметок уровня
            Language.GetItem("h110")
        };

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>
        {
            // Команда позволяет выравнивать отметки уровня по различным условиям
            Language.GetItem("h113")
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
