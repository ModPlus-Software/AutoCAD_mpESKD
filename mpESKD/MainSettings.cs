namespace mpESKD;

using System;
using Base.Enums;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// Main plugin settings
/// </summary>
public class MainSettings : ObservableObject
{
    private const string PName = "mpESKD";
    private static MainSettings _instance;

    /// <summary>
    /// Singleton
    /// </summary>
    public static MainSettings Instance => _instance ?? (_instance = new MainSettings());

    #region Main

    /// <summary>
    /// Запуск палитры свойств вместе с AutoCAD
    /// </summary>
    public bool AutoLoad
    {
        get => !bool.TryParse(UserConfigFile.GetValue(PName, nameof(AutoLoad)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(AutoLoad), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Подключать палитру свойство к палитре ModPlus
    /// </summary>
    public bool AddToMpPalette
    {
        get => bool.TryParse(UserConfigFile.GetValue(PName, nameof(AddToMpPalette)), out var b) && b; // false
        set
        {
            UserConfigFile.SetValue(PName, nameof(AddToMpPalette), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать масштаб из стиля
    /// </summary>
    public bool UseScaleFromStyle
    {
        get => bool.TryParse(UserConfigFile.GetValue(PName, nameof(UseScaleFromStyle)), out var b) && b; // false
        set
        {
            UserConfigFile.SetValue(PName, nameof(UseScaleFromStyle), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать слой из стиля
    /// </summary>
    public bool UseLayerFromStyle
    {
        get => bool.TryParse(UserConfigFile.GetValue(PName, nameof(UseLayerFromStyle)), out var b) && b; // false
        set
        {
            UserConfigFile.SetValue(PName, nameof(UseLayerFromStyle), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Поведение при отсутствии слоя: 0 - применить текущий, 1 - создать новый
    /// </summary>
    public int IfNoLayer
    {
        get => int.TryParse(UserConfigFile.GetValue(PName, nameof(IfNoLayer)), out var i) ? i
            : 0;
        set
        {
            UserConfigFile.SetValue(PName, nameof(IfNoLayer), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Использовать текстовый стиль из стиля
    /// </summary>
    public bool UseTextStyleFromStyle
    {
        get => bool.TryParse(UserConfigFile.GetValue(PName, nameof(UseTextStyleFromStyle)), out var b) && b; // false
        set
        {
            UserConfigFile.SetValue(PName, nameof(UseTextStyleFromStyle), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Поведение при отсутствии текстового стиля: 0 - применить текущий, 1 - создать новый
    /// </summary>
    public int IfNoTextStyle
    {
        get => int.TryParse(UserConfigFile.GetValue(PName, nameof(IfNoTextStyle)), out var i) ? i : 0;
        set
        {
            UserConfigFile.SetValue(PName, nameof(IfNoTextStyle), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Предельное количество выбранных объектов для работы палитры
    /// </summary>
    public int MaxSelectedObjects
    {
        get => int.TryParse(UserConfigFile.GetValue(PName, nameof(MaxSelectedObjects)), out var i) ? i : 100;
        set
        {
            UserConfigFile.SetValue(PName, nameof(MaxSelectedObjects), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Работа со слоем при команде "Создать аналог". Возможные значения: Спросить, Копировать, Не копировать
    /// </summary>
    public LayerActionOnCreateAnalog LayerActionOnCreateAnalog
    {
        get => Enum.TryParse(
            UserConfigFile.GetValue(PName, nameof(LayerActionOnCreateAnalog)),
            out LayerActionOnCreateAnalog e) ? e : LayerActionOnCreateAnalog.Ask;
        set
        {
            UserConfigFile.SetValue(PName, nameof(LayerActionOnCreateAnalog), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Глобальное значение разделителя целой и дробной части для интеллектуальных объектов с числом,
    /// устанавливаемое при создании объекта
    /// </summary>
    public GlobalNumberSeparator GlobalNumberSeparator
    {
        get => Enum.TryParse(
            UserConfigFile.GetValue(PName, nameof(GlobalNumberSeparator)),
            out GlobalNumberSeparator s) ? s : GlobalNumberSeparator.FromStyle;
        set
        {
            UserConfigFile.SetValue(PName, nameof(GlobalNumberSeparator), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(AxisLineTypeScaleProportionScale)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(AxisLineTypeScaleProportionScale), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Сохранять значения последних созданных осей и продолжать значения создаваемых осей
    /// </summary>
    public bool AxisSaveLastTextAndContinueNew
    {
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(AxisSaveLastTextAndContinueNew)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(AxisSaveLastTextAndContinueNew), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(SectionSaveLastTextAndContinueNew)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(SectionSaveLastTextAndContinueNew), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Показывать вспомогательную линию сечения
    /// </summary>
    public bool SectionShowHelpLineOnSelection
    {
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(SectionShowHelpLineOnSelection)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(SectionShowHelpLineOnSelection), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Зависимое перемещение текста
    /// </summary>
    public bool SectionDependentTextMovement
    {
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(SectionDependentTextMovement)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(SectionDependentTextMovement), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(ViewLabelSaveLastTextAndContinueNew)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(ViewLabelSaveLastTextAndContinueNew), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(LevelMarkShowHelpLinesOnSelection)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(LevelMarkShowHelpLinesOnSelection), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(NodalLeaderContinueNodeNumber)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(NodalLeaderContinueNodeNumber), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(SecantNodalLeaderContinueNodeNumber)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(SecantNodalLeaderContinueNodeNumber), value.ToString(), true);
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
        get => !bool.TryParse(
            UserConfigFile.GetValue(PName, nameof(FragmentMarkerContinueNodeNumber)), out var b) || b; // true
        set
        {
            UserConfigFile.SetValue(PName, nameof(FragmentMarkerContinueNodeNumber), value.ToString(), true);
            OnPropertyChanged();
        }
    }

    #endregion

}