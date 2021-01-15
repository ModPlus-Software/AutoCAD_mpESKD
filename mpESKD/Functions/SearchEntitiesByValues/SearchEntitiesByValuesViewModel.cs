namespace mpESKD.Functions.SearchEntitiesByValues
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Input;
    using Autodesk.AutoCAD.DatabaseServices;
    using Base;
    using Base.Abstractions;
    using Base.Attributes;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Контекст поиска по значениям
    /// </summary>
    public class SearchEntitiesByValuesViewModel : VmBase
    {
        private string _searchValue;
        private string _noFoundMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchEntitiesByValuesViewModel"/> class.
        /// </summary>
        public SearchEntitiesByValuesViewModel()
        {
            ResultItems = new ObservableCollection<SearchByValuesResultItem>();
        }

        /// <summary>
        /// Коллекция результатов
        /// </summary>
        public ObservableCollection<SearchByValuesResultItem> ResultItems { get; }

        /// <summary>
        /// Значение для поиска
        /// </summary>
        public string SearchValue
        {
            get => _searchValue;
            set
            {
                if (_searchValue == value)
                    return;
                _searchValue = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Полное совпадение
        /// </summary>
        public bool SearchByValuesFullMatch
        {
            get => bool.TryParse(UserConfigFile.GetValue(Invariables.LangItem, nameof(SearchByValuesFullMatch)), out var b) && b;
            set
            {
                UserConfigFile.SetValue(Invariables.LangItem, nameof(SearchByValuesFullMatch), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Поиск с учетом регистра
        /// </summary>
        public bool SearchByValuesCaseSensitive
        {
            get => bool.TryParse(UserConfigFile.GetValue(Invariables.LangItem, nameof(SearchByValuesCaseSensitive)), out var b) && b;
            set
            {
                UserConfigFile.SetValue(Invariables.LangItem, nameof(SearchByValuesCaseSensitive), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Сообщение, если ничего не найдено
        /// </summary>
        public string NoFoundMessage
        {
            get => _noFoundMessage;
            set
            {
                if (_noFoundMessage == value)
                    return;
                _noFoundMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoFoundMessageVisibility));
            }
        }

        /// <summary>
        /// Видимость сообщения, если ничего не найдено
        /// </summary>
        public System.Windows.Visibility NoFoundMessageVisibility => string.IsNullOrEmpty(NoFoundMessage)
            ? System.Windows.Visibility.Collapsed
            : System.Windows.Visibility.Visible;

        /// <summary>
        /// Искать
        /// </summary>
        public ICommand SearchCommand => new RelayCommandWithoutParameter(Search);

        private void Search()
        {
            try
            {
                var fullMatch = SearchByValuesFullMatch;
                var caseSensitive = SearchByValuesCaseSensitive;
                NoFoundMessage = string.Empty;
                ResultItems.Clear();
                var searchValue = SearchValue.Trim();

                if (string.IsNullOrEmpty(searchValue))
                    return;

                var typesToProceed = TypeFactory.Instance.GetEntityTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(ITextValueEntity)) ||
                                t.GetInterfaces().Contains(typeof(INumericValueEntity)))
                    .ToList();

                using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                {
                    var blockReferences = GetBlockReferencesOfSmartEntities(typesToProceed, tr).ToList();
                    if (blockReferences.Any())
                    {
                        foreach (var pair in blockReferences)
                        {
                            var match = new List<BlockReference>();
                            foreach (var blockReference in pair.Value)
                            {
                                var smartEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
                                if (smartEntity == null)
                                    continue;

                                foreach (var propertyInfo in smartEntity.GetType().GetProperties())
                                {
                                    var attribute = propertyInfo.GetCustomAttribute<ValueToSearchByAttribute>();
                                    if (attribute == null)
                                        continue;

                                    if (IsMatch(smartEntity, propertyInfo, searchValue, fullMatch, caseSensitive))
                                        match.Add(blockReference);
                                }
                            }

                            if (match.Any())
                            {
                                var resultItem = new SearchByValuesResultItem(pair.Key);
                                resultItem.Blocks.AddRange(match);
                                ResultItems.Add(resultItem);
                            }
                        }
                    }

                    if (!ResultItems.Any())
                    {
                        NoFoundMessage = string.Format(Language.GetItem("h124"), searchValue);
                    }

                    tr.Commit();
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static Dictionary<Type, List<BlockReference>> GetBlockReferencesOfSmartEntities(
            IEnumerable<Type> types, Transaction tr)
        {
            var typeNames = types.Select(t => new Tuple<string, Type>($"mp{t.Name}", t)).ToList();
            var dictionary = new Dictionary<Type, List<BlockReference>>();
            var btr = (BlockTableRecord)tr.GetObject(AcadUtils.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (var objectId in btr)
            {
                if (tr.GetObject(objectId, OpenMode.ForRead) is BlockReference blockReference &&
                    blockReference.XData != null)
                {
                    var typedValue = blockReference.XData.AsArray()
                        .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName);
                    var value = typedValue.Value as string;

                    foreach (var tuple in typeNames)
                    {
                        if (tuple.Item1 == value)
                        {
                            if (dictionary.ContainsKey(tuple.Item2))
                            {
                                dictionary[tuple.Item2].Add(blockReference);
                            }
                            else
                            {
                                dictionary.Add(tuple.Item2, new List<BlockReference> { blockReference });
                            }

                            break;
                        }
                    }
                }
            }

            return dictionary;
        }

        private static bool IsMatch(
            object obj, PropertyInfo propertyInfo, string searchString, bool fullMatch, bool caseSensitive)
        {
            var value = propertyInfo.GetValue(obj).ToString();

            if (fullMatch)
            {
                return string.Equals(
                    value,
                    searchString,
                    caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
            }

            return caseSensitive 
                ? value.Contains(searchString) 
                : value.ToUpper().Contains(searchString.ToUpper());
        }
    }
}
