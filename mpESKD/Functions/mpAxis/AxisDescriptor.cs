namespace mpESKD.Functions.mpAxis
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc />
    public class AxisDescriptor : ISmartEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(Axis);

        /// <inheritdoc />
        public string Name => "mpAxis";

        /// <inheritdoc />
        // "Прямая ось";
        public string LName => Language.GetItem("h41");

        /// <inheritdoc />
        // "Отрисовка прямой оси по ГОСТ 21.101-97";
        public string Description => Language.GetItem("h65");

        /// <inheritdoc />
        // "Создание интеллектуального объекта на основе анонимного блока, описывающего прямую ось по ГОСТ 21.101-97, путем указания двух точек";
        public string FullDescription => Language.GetItem("h66");

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubFunctionsNames => new List<string>();

        /// <inheritdoc />
        public List<string> SubFunctionsLNames => new List<string>();

        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>();
    }
}
