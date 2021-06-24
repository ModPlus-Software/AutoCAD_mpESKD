﻿namespace mpESKD.Functions.mpViewLabel
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
        // Разрез
        public string LName => Language.GetItem("h79");

        /// <inheritdoc />
        // Создание обозначения разреза (сечения) по ГОСТ 2.305-68
        public string Description => Language.GetItem("h80");

        /// <inheritdoc />
        // Создание интеллектуального объекта на основе анонимного блока, описывающего разрез (сечение) по ГОСТ 2.305-68
        public string FullDescription => Language.GetItem("h81");

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
