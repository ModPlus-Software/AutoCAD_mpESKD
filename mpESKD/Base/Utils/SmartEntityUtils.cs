namespace mpESKD.Base.Utils;

using Abstractions;

/// <summary>
/// Утилиты для <see cref="SmartEntity"/>
/// </summary>
public static class SmartEntityUtils
{
    /// <summary>
    /// Возвращает дескриптор <see cref="ISmartEntityDescriptor"/> для типа, унаследованного от <see cref="SmartEntity"/>
    /// </summary>
    /// <typeparam name="T">Тип, унаследованный от <see cref="SmartEntity"/></typeparam>
    public static ISmartEntityDescriptor GetDescriptor<T>()
        where T : SmartEntity
    {
        return TypeFactory.Instance.GetDescriptor(typeof(T));
    }

    /// <summary>
    /// Отправить статистику запуска команды создания интеллектуального объекта определенного типа
    /// </summary>
    /// <typeparam name="T">Тип, унаследованный от <see cref="SmartEntity"/></typeparam>
    public static void SendStatistic<T>()
        where T : SmartEntity
    {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                GetDescriptor<T>().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
    }
}