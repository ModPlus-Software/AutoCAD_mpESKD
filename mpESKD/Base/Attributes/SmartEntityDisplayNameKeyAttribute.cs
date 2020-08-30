namespace mpESKD.Base.Attributes
{
    using System;

    /// <summary>
    /// Атрибут, указывающий ключ локализации для имени интеллектуального примитива
    /// </summary>
    public class SmartEntityDisplayNameKeyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmartEntityDisplayNameKeyAttribute"/> class.
        /// </summary>
        /// <param name="localizationKey">Ключ локализации (указывает имя узла в файле xml локализации)</param>
        public SmartEntityDisplayNameKeyAttribute(string localizationKey)
        {
            LocalizationKey = localizationKey;
        }

        /// <summary>
        /// Ключ локализации (указывает имя узла в файле xml локализации)
        /// </summary>
        public string LocalizationKey { get; }
    }
}