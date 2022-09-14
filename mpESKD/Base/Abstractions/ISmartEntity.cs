// ReSharper disable InconsistentNaming
namespace mpESKD.Base.Abstractions;

using System.Collections.Generic;
using Attributes;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

/// <summary>
/// Интеллектуальный объект
/// </summary>
public interface ISmartEntity
{
    /// <summary>
    /// True - происходит чтение и установка данных из XData
    /// </summary>
    bool IsXDataReadingProcess { get; }
        
    /// <summary>
    /// Первая точка примитива в мировой системе координат.
    /// Должна соответствовать точке вставке блока
    /// </summary>
    Point3d InsertionPoint { get; set; }

    /// <summary>
    /// Первая точка примитива в системе координат блока для работы с геометрией в
    /// методе <see cref="UpdateEntities"/> ("внутри" блока)
    /// </summary>
    Point3d InsertionPointOCS { get; }

    /// <summary>
    /// Конечная точка примитива в мировой системе координат. Свойство содержится в базовом классе для
    /// работы <see cref="DefaultEntityJig"/>. Имеется в каждом примитиве, но
    /// если не требуется, то просто не использовать её
    /// </summary>
    Point3d EndPoint { get; set; }

    /// <summary>
    /// Конечная точка примитива в системе координат блока для работы с геометрией в
    /// методе <see cref="UpdateEntities"/> ("внутри" блока). Имеется в каждом примитиве, но
    /// если не требуется, то просто не использовать её
    /// </summary>
    Point3d EndPointOCS { get; }

    /// <summary>
    /// Минимальное расстояние между точками (обычно начальной и конечной точкой)
    /// </summary>
    double MinDistanceBetweenPoints { get; }

    /// <summary>
    /// Коллекция примитивов, создающих графическое представление интеллектуального примитива
    /// согласно его свойств
    /// </summary>
    IEnumerable<Entity> Entities { get; }
        
    /// <summary>
    /// Is value created
    /// </summary>
    bool IsValueCreated { get; set; }

    /// <summary>
    /// Матрица трансформации BlockReference
    /// </summary>
    Matrix3d BlockTransform { get; set; }
        
    /// <summary>
    /// Угол поворота BlockReference
    /// </summary>
    double Rotation { get; set; }

    /// <summary>
    /// Угол поворота BlockReference
    /// </summary>
    double ScaleFactorX { get; set; }
        
    /// <summary>
    /// BlockReference повернут (имеет угол, отличный от 0,0)
    /// </summary>
    bool IsRotated { get; }

    /// <summary>
    /// Стиль примитива. Свойство используется для работы палитры, а стиль задается через свойство <see cref="StyleGuid"/>
    /// </summary>
    string Style { get; set; }

    /// <summary>
    /// Имя слоя
    /// </summary>
    string LayerName { get; set; }

    /// <summary>
    /// Масштаб примитива
    /// </summary>
    AnnotationScale Scale { get; set; }
        
    /// <summary>
    /// Тип линии. Свойство является абстрактным, так как в зависимости от интеллектуального примитива
    /// может отличатся описание или может вообще быть не нужным. Индекс (группа General) всегда нужно ставить = 4
    /// </summary>
    string LineType { get; set; }

    /// <summary>
    /// Масштаб типа линии для примитивов, имеющих изменяемый тип линии.
    /// Свойство является абстрактным, так как в зависимости от интеллектуального примитива
    /// может отличатся описание или может вообще быть не нужным. Индекс (группа General) всегда нужно ставить = 5
    /// </summary>
    double LineTypeScale { get; set; }

    /// <summary>
    /// Текстовый стиль.
    /// Свойство является абстрактным, так как в зависимости от интеллектуального примитива
    /// может отличатся описание или может вообще быть не нужным. Индекс (группа Content) всегда нужно ставить = 1
    /// Категория всегда Content
    /// </summary>
    string TextStyle { get; set; }
        
    /// <summary>
    /// Идентификатор стиля
    /// </summary>
    string StyleGuid { get; set; }
        
    /// <summary>
    /// Идентификатор (ObjectId) блока
    /// </summary>
    ObjectId BlockId { get; set; }

    /// <summary>
    /// Запись (описание) блока
    /// </summary>
    BlockTableRecord BlockRecord { get; set; }

    /// <summary>
    /// Текущий численный масштаб масштаб
    /// </summary>
    double GetScale();

    /// <summary>
    /// Текущий полный численный масштаб (с учетом масштаба блока)
    /// </summary>
    double GetFullScale();
        
    /// <summary>
    /// Возвращает матрицу обратного поворота по отношению к углу поворота блока <see cref="Rotation"/>.
    /// Поворот выполняется вокруг оси Z
    /// </summary>
    /// <param name="center">Центр поворота</param>
    Matrix3d GetBackRotationMatrix(Point3d center);

    /// <summary>
    /// Возвращает коллекцию точек, которые используются для привязки
    /// </summary>
    IEnumerable<Point3d> GetPointsForOsnap();

    /// <summary>
    /// Возвращает <see cref="BlockTableRecord"/> для обработки команды Undo
    /// </summary>
    /// <param name="blockReference"><see cref="BlockReference"/></param>
    BlockTableRecord GetBlockTableRecordForUndo(BlockReference blockReference);

    /// <summary>
    /// Возвращает описание блока с открытием без использования транзакции
    /// </summary>
    /// <param name="blockReference">Вхождение блока</param>
    BlockTableRecord GetBlockTableRecordWithoutTransaction(BlockReference blockReference);

    /// <summary>
    /// Получение свойств блока, которые присуще примитиву
    /// </summary>
    /// <param name="entity">Объект <see cref="DBObject"/></param>
    void GetPropertiesFromCadEntity(DBObject entity);
        
    /// <summary>
    /// Перерисовка элементов блока по параметрам ЕСКД элемента
    /// </summary>
    void UpdateEntities();

    /// <summary>
    /// Сериализация значений параметров, помеченных атрибутом <see cref="SaveToXDataAttribute"/>, в экземпляр <see cref="ResultBuffer"/>
    /// </summary>
    ResultBuffer GetDataForXData();

    /// <summary>
    /// Установка значений свойств, отмеченных атрибутом <see cref="SaveToXDataAttribute"/> из расширенных данных примитива AutoCAD
    /// </summary>
    /// <param name="resultBuffer"><see cref="ResultBuffer"/></param>
    /// <param name="skipPoints">Пропускать ли точки</param>
    void SetPropertiesValuesFromXData(ResultBuffer resultBuffer, bool skipPoints = false);

    /// <summary>
    /// Копирование свойств, отмеченных атрибутом <see cref="SaveToXDataAttribute"/> из расширенных данных примитива AutoCAD
    /// в текущий интеллектуальный примитив
    /// </summary>
    /// <param name="sourceEntity">Интеллектуальный объекта</param>
    /// <param name="copyLayer">Копировать слой</param>
    void SetPropertiesFromSmartEntity(SmartEntity sourceEntity, bool copyLayer);

    /// <summary>
    /// Установка свойств для примитивов, которые не меняются
    /// </summary>
    /// <param name="entity">Примитив AutoCAD</param>
    void SetImmutablePropertiesToNestedEntity(Entity entity);

    /// <summary>
    /// Установка свойств для примитива, которые могут меняться "из вне" (ByBlock)
    /// </summary>
    /// <param name="entity">Примитив AutoCAD</param>
    void SetChangeablePropertiesToNestedEntity(Entity entity);
}