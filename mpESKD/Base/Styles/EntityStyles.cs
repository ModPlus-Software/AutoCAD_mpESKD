namespace mpESKD.Base.Styles
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Utils;

    /// <summary>
    /// Стили интеллектуального объекта
    /// </summary>
    public class EntityStyles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityStyles"/> class.
        /// </summary>
        /// <param name="entityType">Тип интеллектуального объекта</param>
        public EntityStyles(Type entityType)
        {
            EntityType = entityType;
            Styles = new ObservableCollection<SmartEntityStyle>();
        }

        /// <summary>
        /// Тип интеллектуального объекта
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Отображаемое имя интеллектуального объекта
        /// </summary>
        public string DisplayName => LocalizationUtils.GetEntityLocalizationName(EntityType);

        /// <summary>
        /// Коллекция стилей для указанного типа интеллектуального объекта
        /// </summary>
        public ObservableCollection<SmartEntityStyle> Styles { get; }

        /// <summary>
        /// Есть ли в списке стили с одинаковыми именами
        /// </summary>
        public bool HasStylesWithSameName => Styles.Select(s => s.Name).Distinct().Count() != Styles.Count;

        /// <summary>
        /// Сделать слой текущим
        /// </summary>
        public void SetCurrent(SmartEntityStyle style)
        {
            foreach (SmartEntityStyle entityStyle in Styles)
            {
                if (entityStyle != style)
                {
                    if (entityStyle.IsCurrent)
                    {
                        entityStyle.IsCurrent = false;
                    }
                }
            }

            style.IsCurrent = true;
        }
    }
}
