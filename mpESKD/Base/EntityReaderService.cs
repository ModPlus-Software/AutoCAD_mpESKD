namespace mpESKD.Base;

using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using JetBrains.Annotations;

/// <summary>
/// Сервис распаковки (чтения) интеллектуальных объектов из расширенных данных блока
/// </summary>
public class EntityReaderService
{
    private static EntityReaderService _entityReaderService;

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static EntityReaderService Instance => _entityReaderService ??= new EntityReaderService();

    /// <summary>
    /// Распаковка интеллектуального объекта, приведенного к базовому типу <see cref="SmartEntity"/> из
    /// расширенных данных объекта <see cref="DBObject"/>
    /// </summary>
    /// <param name="entity">Объект AutoCAD</param>
    [CanBeNull]
    public SmartEntity GetFromEntity(DBObject entity)
    {
        var applicableCommands = TypeFactory.Instance.GetEntityCommandNames();
        if (entity.XData == null)
        {
            return null;
        }

        var typedValue = entity.XData.AsArray()
            .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName && applicableCommands.Contains(tv.Value.ToString()));
        if (typedValue.Value != null)
        {
            var appName = typedValue.Value.ToString();
            var entityType = TypeFactory.Instance.GetEntityTypes().FirstOrDefault(t => t.Name == appName.Substring(2));
            if (entityType != null)
            {
                return GetEntity(entity, entityType, appName);
            }
        }

        return null;
    }

    /// <summary>
    /// Распаковка интеллектуального объекта с приведением к типу <see cref="T"/> из
    /// расширенных данных объекта <see cref="DBObject"/>
    /// </summary>
    /// <typeparam name="T">Тип интеллектуального объекта</typeparam>
    /// <param name="entity">Объект AutoCAD</param>
    [CanBeNull]
    public T GetFromEntity<T>(Entity entity) 
        where T : SmartEntity
    {
        return (T)GetFromEntity(entity);
    }

    /// <summary>
    /// Распаковка интеллектуального объекта с приведением к типу <see cref="T"/> из
    /// расширенных данных объекта <see cref="DBObject"/>
    /// </summary>
    /// <typeparam name="T">Тип интеллектуального объекта</typeparam>
    /// <param name="entity">Объект AutoCAD</param>
    [CanBeNull]
    public T GetFromEntity<T>(DBObject entity) 
        where T : SmartEntity
    {
        return (T)GetFromEntity(entity);
    }
        
    private static SmartEntity GetEntity(DBObject ent, Type type, string appName)
    {
        using (var resBuf = ent.GetXDataForApplication(appName))
        {
            // В случае команды ОТМЕНА может вернуть null
            if (resBuf == null)
            {
                return null;
            }

            var entity = (SmartEntity)Activator.CreateInstance(type);
            entity.BlockId = ent.ObjectId;

            // Получаем параметры из самого блока
            // ОБЯЗАТЕЛЬНО СНАЧАЛА ИЗ БЛОКА!!!!!!
            entity.GetPropertiesFromCadEntity(ent);

            // Получаем параметры из XData
            entity.SetPropertiesValuesFromXData(resBuf);

            return entity;
        }
    }
}