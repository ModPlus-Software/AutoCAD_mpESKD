namespace mpESKD.Functions.SearchEntitiesByValues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Base;
    using Base.Utils;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Результат поиска по значению
    /// </summary>
    public class SearchByValuesResultItem : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchByValuesResultItem"/> class.
        /// </summary>
        /// <param name="typeOfSmartEntity">Тип интеллектуального объекта, соответствующий данному результату поиска</param>
        public SearchByValuesResultItem(Type typeOfSmartEntity)
        {
            TypeOfSmartEntity = typeOfSmartEntity;
            Blocks = new List<BlockReference>();
        }

        /// <summary>
        /// Тип интеллектуального объекта, соответствующий данному результату поиска
        /// </summary>
        public Type TypeOfSmartEntity { get; }

        /// <summary>
        /// Коллекция блоков, содержащих данный тип интеллектуального объекта и совпавших по искомому значению
        /// </summary>
        public List<BlockReference> Blocks { get; }

        /// <summary>
        /// Заголовок. Включает имя интеллектуального объекта и количество блоков
        /// </summary>
        public string Title => $"{TypeFactory.Instance.GetDescriptor(TypeOfSmartEntity).LName} [{Blocks.Count}]";

        /// <summary>
        /// Выбрать все
        /// </summary>
        public ICommand SelectAllCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                var selection = AcadUtils.Editor.SelectImplied();
                var ids = Blocks.Select(b => b.ObjectId).ToList();

                if (selection != null && selection.Status == PromptStatus.OK)
                {
                    foreach (SelectedObject o in selection.Value)
                        ids.Add(o.ObjectId);
                }

                AcadUtils.Editor.SetImpliedSelection(ids.ToArray());

                Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });
    }
}
