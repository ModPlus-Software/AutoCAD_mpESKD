namespace mpESKD.Base.Styles
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Utils;

    public class EntityStyles
    {
        public EntityStyles(Type entityType)
        {
            EntityType = entityType;
            Styles = new ObservableCollection<SmartEntityStyle>();
        }

        /// <summary>
        /// Тип примитива
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Отображаемое имя примитива
        /// </summary>
        public string DisplayName => LocalizationUtils.GetEntityLocalizationName(EntityType);

        /// <summary>
        /// Коллекция стилей для указанного типа примитива
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
