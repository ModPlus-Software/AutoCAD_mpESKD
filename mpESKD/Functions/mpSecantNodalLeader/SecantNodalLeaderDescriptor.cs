namespace mpESKD.Functions.mpSecantNodalLeader
{
    using System;
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc/>
    public class SecantNodalLeaderDescriptor : IIntellectualEntityDescriptor
    {
        /// <inheritdoc/>
        public Type EntityType => typeof(SecantNodalLeader);

        /// <inheritdoc/>
        public string Name => "mpSecantNodalLeader";

        /// <inheritdoc/>
        // Секущая узловая выноска
        public string LName => Language.GetItem("h133");

        /// <inheritdoc/>
        // Создание секущей узловой выноски
        public string Description => Language.GetItem("h134");

        /// <inheritdoc/>
        // Создание интеллектуального объекта на основе анонимного блока, описывающего секущую узловую выноску
        public string FullDescription => Language.GetItem("h135");

        /// <inheritdoc/>
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc/>
        public List<string> SubFunctionsNames => new List<string>();

        /// <inheritdoc/>
        public List<string> SubFunctionsLNames => new List<string>();

        /// <inheritdoc/>
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc/>
        public List<string> SubHelpImages => new List<string>();
    }
}
