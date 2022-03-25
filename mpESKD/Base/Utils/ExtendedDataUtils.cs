namespace mpESKD.Base.Utils;

using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

/// <summary>Вспомогательные методы работы с расширенными данными
/// Есть аналогичные в MpCadHelpers. Некоторые будут совпадать
/// но все-равно делаю отдельно</summary>
public static class ExtendedDataUtils
{
    /// <summary>
    /// Добавление регистрации приложения в соответствующую таблицу чертежа
    /// </summary>
    /// <typeparam name="T">Тип, унаследованный от <see cref="SmartEntity"/></typeparam>
    public static void AddRegAppTableRecord<T>() 
        where T : SmartEntity
    {
        AddRegAppTableRecord(SmartEntityUtils.GetDescriptor<T>().Name);
    }
        
    /// <summary>
    /// Проверка поддерживаемости примитива для Overrule
    /// </summary>
    /// <param name="rxObject">Instance of <see cref="RXObject"/></param>
    /// <param name="appName">Имя типа интеллектуального объекта</param>
    /// <param name="checkIsNullId">comment #16 - http://adn-cis.org/forum/index.php?topic=8910.15 </param>
    public static bool IsApplicable(RXObject rxObject, string appName, bool checkIsNullId = false)
    {
        var dbObject = rxObject as DBObject;
        if (dbObject == null)
        {
            return false;
        }

        if (checkIsNullId)
        {
            if (dbObject.ObjectId == ObjectId.Null)
            {
                return false;
            }
        }

        if (dbObject is BlockReference)
        {
            // Всегда нужно проверять по наличию расширенных данных
            // иначе может привести к фатальным ошибкам при работе с динамическими блоками
            return IsIntellectualEntity(dbObject, appName);
        }

        return false;
    }

    /// <summary>
    /// Проверка поддерживаемости вставки блока путем проверки наличия XData с поддерживаемым кодом 1001
    /// </summary>
    /// <param name="blockReference">Вхождение блока</param>
    public static bool IsApplicable(BlockReference blockReference)
    {
        if (blockReference.XData == null)
        {
            return false;
        }

        var applicableCommands = TypeFactory.Instance.GetEntityCommandNames();
        var typedValue = blockReference.XData.AsArray()
            .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName &&
                                  applicableCommands.Contains(tv.Value.ToString()));
        return typedValue.Value != null;
    }

    /// <summary>
    /// Проверка поддерживаемости вставки блока путем проверки наличия XData с поддерживаемым кодом 1001
    /// </summary>
    /// <param name="rxObject"><see cref="RXObject"/></param>
    /// <param name="checkIsNullId">comment #16 - http://adn-cis.org/forum/index.php?topic=8910.15 </param>
    public static bool IsApplicable(RXObject rxObject, bool checkIsNullId)
    {
        var dbObject = rxObject as DBObject;
        if (dbObject == null)
        {
            return false;
        }

        if (checkIsNullId)
        {
            if (dbObject.ObjectId == ObjectId.Null)
            {
                return false;
            }
        }

        if (dbObject.XData == null)
        {
            return false;
        }

        var applicableCommands = TypeFactory.Instance.GetEntityCommandNames();
        var typedValue = dbObject.XData.AsArray()
            .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName && 
                                  applicableCommands.Contains(tv.Value.ToString()));
        return typedValue.Value != null;
    }

    /// <summary>
    /// Возвращает имя типа поддерживаемого интеллектуального объекта, если такое содержится в расширенных данных блока
    /// </summary>
    /// <param name="blockReference">Вхождение блока</param>
    public static string ApplicableAppName(BlockReference blockReference)
    {
        if (blockReference.XData == null)
        {
            return null;
        }

        var applicableCommands = TypeFactory.Instance.GetEntityCommandNames();
        foreach (var typedValue in blockReference.XData.AsArray())
        {
            var value = typedValue.Value.ToString();
            if (typedValue.TypeCode == (int)DxfCode.ExtendedDataRegAppName && applicableCommands.Contains(value))
                return value;
        }

        return null;
    }

    /// <summary>
    /// Проверка по XData вхождения блока, что он является любым ЕСКД примитивом
    /// </summary>
    /// <param name="blkRef">Вхождение блока</param>
    /// <param name="appName">Название типа интеллектуального объекта</param>
    public static bool IsIntellectualEntity(Entity blkRef, string appName)
    {
        var rb = blkRef.GetXDataForApplication(appName);
        return rb != null;
    }

    /// <summary>
    /// Проверка по XData вхождения блока, что он является любым ЕСКД примитивом
    /// </summary>
    /// <param name="dbObject">Вхождение блока</param>
    /// <param name="appName">Название типа интеллектуального объекта</param>
    public static bool IsIntellectualEntity(DBObject dbObject, string appName)
    {
        var rb = dbObject.GetXDataForApplication(appName);
        return rb != null;
    }
        
    /// <summary>
    /// Добавление регистрации приложения в соответствующую таблицу чертежа
    /// </summary>
    /// <param name="appName">Имя типа интеллектуального объекта</param>
    private static void AddRegAppTableRecord(string appName)
    {
        using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
        {
            var rat =
                (RegAppTable)tr.GetObject(AcadUtils.Database.RegAppTableId, OpenMode.ForRead, false);
            if (!rat.Has(appName))
            {
                rat.UpgradeOpen();
                var regAppTableRecord = new RegAppTableRecord
                {
                    Name = appName
                };
                rat.Add(regAppTableRecord);
                tr.AddNewlyCreatedDBObject(regAppTableRecord, true);
            }

            tr.Commit();
        }
    }
}