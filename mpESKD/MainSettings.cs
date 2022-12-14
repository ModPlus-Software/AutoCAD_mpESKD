namespace mpESKD;

using Base.Enums;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// Main plugin settings
/// </summary>
public class MainSettings : ObservableObject
{
    private readonly UserConfigFileUtils _userConfigFileUtils;
    private static MainSettings _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainSettings"/> class.
    /// </summary>
    public MainSettings()
    {
        _userConfigFileUtils = new UserConfigFileUtils(ModPlusConnector.Instance);
    }

    /// <summary>
    /// Singleton
    /// </summary>
    public static MainSettings Instance => _instance ??= new MainSettings();

    #region Main

    /// <summary>
    /// Запуск палитры свойств вместе с AutoCAD
    /// </summary>
    public bool AutoLoad
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подключать палитру свойство к палитре ModPlus
    /// </summary>
    public bool AddToMpPalette
    {
        get => _userConfigFileUtils.GetValue(false);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать масштаб из стиля
    /// </summary>
    public bool UseScaleFromStyle
    {
        get => _userConfigFileUtils.GetValue(false);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать слой из стиля
    /// </summary>
    public bool UseLayerFromStyle
    {
        get => _userConfigFileUtils.GetValue(false);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Поведение при отсутствии слоя: 0 - применить текущий, 1 - создать новый
    /// </summary>
    public int IfNoLayer
    {
        get => _userConfigFileUtils.GetValue(0);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать текстовый стиль из стиля
    /// </summary>
    public bool UseTextStyleFromStyle
    {
        get => _userConfigFileUtils.GetValue(false);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Поведение при отсутствии текстового стиля: 0 - применить текущий, 1 - создать новый
    /// </summary>
    public int IfNoTextStyle
    {
        get => _userConfigFileUtils.GetValue(0);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Предельное количество выбранных объектов для работы палитры
    /// </summary>
    public int MaxSelectedObjects
    {
        get => _userConfigFileUtils.GetValue(100);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Работа со слоем при команде "Создать аналог". Возможные значения: Спросить, Копировать, Не копировать
    /// </summary>
    public LayerActionOnCreateAnalog LayerActionOnCreateAnalog
    {
        get => _userConfigFileUtils.GetValue(LayerActionOnCreateAnalog.Ask);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Глобальное значение разделителя целой и дробной части для интеллектуальных объектов с числом,
    /// устанавливаемое при создании объекта
    /// </summary>
    public GlobalNumberSeparator GlobalNumberSeparator
    {
        get => _userConfigFileUtils.GetValue(GlobalNumberSeparator.FromStyle);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Axis

    /// <summary>
    /// Менять масштаб типа линии прямой оси пропорционально масштабу примитива
    /// </summary>
    public bool AxisLineTypeScaleProportionScale
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Сохранять значения последних созданных осей и продолжать значения создаваемых осей
    /// </summary>
    public bool AxisSaveLastTextAndContinueNew
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Section

    /// <summary>
    /// Сохранять значения последних созданных разрезов и продолжать значения создаваемых разрезов
    /// </summary>
    public bool SectionSaveLastTextAndContinueNew
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Показывать вспомогательную линию сечения
    /// </summary>
    public bool SectionShowHelpLineOnSelection
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Зависимое перемещение текста
    /// </summary>
    public bool SectionDependentTextMovement
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region ViewLabel

    /// <summary>
    /// Сохранять значения последних созданных разрезов и продолжать значения создаваемых разрезов
    /// </summary>
    public bool ViewLabelSaveLastTextAndContinueNew
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region LevelMark

    /// <summary>
    /// Показывать вспомогательные линии
    /// </summary>
    public bool LevelMarkShowHelpLinesOnSelection
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region NodalLeader

    /// <summary>
    /// Продолжать нумерацию узла при создании новой узловой выноски
    /// </summary>
    public bool NodalLeaderContinueNodeNumber
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region SecantNodalLeader

    /// <summary>
    /// Продолжать нумерацию узла при создании новой узловой выноски
    /// </summary>
    public bool SecantNodalLeaderContinueNodeNumber
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region FragmentMarker

    /// <summary>
    /// Продолжать нумерацию фрагмента при создании нового фрагмента
    /// </summary>
    public bool FragmentMarkerContinueNodeNumber
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region ChainLeader

    /// <summary>
    /// Продолжать нумерацию выноски при создании новой выноски
    /// </summary>
    public bool ChainLeaderContinueNodeNumber
    {
        get => _userConfigFileUtils.GetValue(true);
        set
        {
            _userConfigFileUtils.SetValue(value);
            OnPropertyChanged();
        }
    }

    #endregion

}