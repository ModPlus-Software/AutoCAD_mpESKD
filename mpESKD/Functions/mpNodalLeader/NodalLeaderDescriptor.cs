namespace mpESKD.Functions.mpNodalLeader
{
    using System.Collections.Generic;
    using Base.Abstractions;
    using ModPlusAPI;

    /// <inheritdoc/>
    public class NodalLeaderDescriptor : IIntellectualEntityDescriptor
    {
        private static NodalLeaderDescriptor _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static NodalLeaderDescriptor Instance => _instance ?? (_instance = new NodalLeaderDescriptor());

        /// <inheritdoc/>
        public string Name => "mpNodalLeader";

        /// <inheritdoc/>
        // Узловая выноска
        public string LName => Language.GetItem("h126");

        /// <inheritdoc/>
        // Создание узловой выноски
        public string Description => Language.GetItem("h127");

        /// <inheritdoc/>
        // Создание интеллектуального объекта на основе анонимного блока, описывающего узловую выноску
        public string FullDescription => Language.GetItem("h128");

        /// <inheritdoc/>
        public string ToolTipHelpImage { get; }

        /// <inheritdoc/>
        public List<string> SubFunctionsNames { get; }

        /// <inheritdoc/>
        public List<string> SubFunctionsLNames { get; }

        /// <inheritdoc/>
        public List<string> SubDescriptions { get; }

        /// <inheritdoc/>
        public List<string> SubFullDescriptions { get; }

        /// <inheritdoc/>
        public List<string> SubHelpImages { get; }
    }
}
