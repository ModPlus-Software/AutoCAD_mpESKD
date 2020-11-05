// ReSharper disable RedundantNameQualifier
namespace mpESKD.Base
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Abstractions;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using JetBrains.Annotations;
    using ModPlusAPI.FunctionDataHelpers.mpESKD;
    using ModPlusAPI.Windows;
    using mpESKD.Base.Attributes;
    using mpESKD.Base.Enums;
    using mpESKD.Base.Styles;
    using Utils;

    /// <summary>
    /// Абстрактный класс интеллектуального объекта
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "<Ожидание>")]
    public abstract class SmartEntity : ISmartEntity, IDisposable
    {
        private BlockTableRecord _blockRecord;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartEntity"/> class.
        /// </summary>
        protected SmartEntity()
        {
            BlockTransform = Matrix3d.Identity;
            var blockTableRecord = new BlockTableRecord
            {
                Name = "*U",
                BlockScaling = BlockScaling.Uniform
            };
            BlockRecord = blockTableRecord;
        }

        /// <summary>
        /// Инициализация экземпляра класса SmartEntity без заполнения данными
        /// В данном случае уже все данные получены и нужно только "построить" базовые примитивы
        /// </summary>
        /// <param name="blockId">ObjectId анонимного блока, представляющего интеллектуальный объект</param>
        protected SmartEntity(ObjectId blockId)
        {
            BlockId = blockId;
        }

        /// <summary>
        /// Первая точка примитива в мировой системе координат.
        /// Должна соответствовать точке вставке блока
        /// </summary>
        public Point3d InsertionPoint { get; set; } = Point3d.Origin;

        /// <summary>
        /// Первая точка примитива в системе координат блока для работы с геометрией в
        /// методе <see cref="UpdateEntities"/> ("внутри" блока)
        /// </summary>
        public Point3d InsertionPointOCS => InsertionPoint.TransformBy(BlockTransform.Inverse());

        /// <summary>
        /// Конечная точка примитива в мировой системе координат. Свойство содержится в базовом классе для
        /// работы <see cref="DefaultEntityJig"/>. Имеется в каждом примитиве, но
        /// если не требуется, то просто не использовать её
        /// </summary>
        [SaveToXData]
        public virtual Point3d EndPoint { get; set; } = Point3d.Origin;

        /// <summary>
        /// Конечная точка примитива в системе координат блока для работы с геометрией в
        /// методе <see cref="UpdateEntities"/> ("внутри" блока). Имеется в каждом примитиве, но
        /// если не требуется, то просто не использовать её
        /// </summary>
        public Point3d EndPointOCS => EndPoint.TransformBy(BlockTransform.Inverse());

        /// <summary>
        /// Минимальное расстояние между точками (обычно начальной и конечной точкой)
        /// </summary>
        public abstract double MinDistanceBetweenPoints { get; }

        /// <summary>
        /// Коллекция примитивов, создающих графическое представление интеллектуального примитива
        /// согласно его свойств
        /// </summary>
        public abstract IEnumerable<Entity> Entities { get; }

        /// <summary>
        /// Коллекция примитивов, которая передается в BlockReference
        /// </summary>
        private IEnumerable<Entity> _entitiesToBeDrawn
        {
            get { return Entities.Where(e => e != null); }
        }

        /// <summary>
        /// Is value created
        /// </summary>
        public bool IsValueCreated { get; set; }

        /// <summary>
        /// Матрица трансформации BlockReference
        /// </summary>
        public Matrix3d BlockTransform { get; set; }

        /// <summary>
        /// Стиль примитива. Свойство используется для работы палитры, а стиль задается через свойство <see cref="StyleGuid"/>
        /// </summary>
        [EntityProperty(PropertiesCategory.General, 1, "h50", "", propertyScope: PropertyScope.Palette, descLocalKey: "h52")]
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// Имя слоя
        /// </summary>
        [EntityProperty(PropertiesCategory.General, 2, "p7", "", descLocalKey: "d7")]
        [SaveToXData]
        public string LayerName { get; set; } = string.Empty;

        private AnnotationScale _scale;

        /// <summary>
        /// Масштаб примитива
        /// </summary>
        [EntityProperty(PropertiesCategory.General, 3, "p5", "1:1", descLocalKey: "d5")]
        [SaveToXData]
        public AnnotationScale Scale
        {
            get
            {
                if (_scale != null)
                {
                    return _scale;
                }

                return new AnnotationScale { Name = "1:1", DrawingUnits = 1, PaperUnits = 1 };
            }

            set
            {
                var oldScale = _scale;
                _scale = value;
                if (oldScale != null && oldScale != value)
                {
                    ProcessScaleChange(oldScale, value);
                }
            }
        }

        /// <summary>
        /// Метод обработки события изменения масштаба
        /// </summary>
        /// <param name="oldScale">Старый масштаб</param>
        /// <param name="newScale">Новый масштаб</param>
        protected virtual void ProcessScaleChange(AnnotationScale oldScale, AnnotationScale newScale)
        {
        }

        /// <summary>
        /// Тип линии. Свойство является абстрактным, так как в зависимости от интеллектуального примитива
        /// может отличатся описание или может вообще быть не нужным. Индекс всегда нужно ставить = 4
        /// </summary>
        [SaveToXData]
        public abstract string LineType { get; set; }

        /// <summary>
        /// Масштаб типа линии для примитивов, имеющих изменяемый тип линии.
        /// Свойство является абстрактным, так как в зависимости от интеллектуального примитива
        /// может отличатся описание или может вообще быть не нужным. Индекс всегда нужно ставить = 5
        /// </summary>
        [SaveToXData]
        public abstract double LineTypeScale { get; set; }

        /// <summary>
        /// Текстовый стиль.
        /// Свойство является абстрактным, так как в зависимости от интеллектуального примитива
        /// может отличатся описание или может вообще быть не нужным. Индекс всегда нужно ставить = 1
        /// Категория всегда Content
        /// </summary>
        [SaveToXData]
        public abstract string TextStyle { get; set; }

        /// <summary>Текущий численный масштаб масштаб</summary>
        public double GetScale()
        {
            return Scale.GetNumericScale();
        }

        /// <summary>
        /// Текущий полный численный масштаб (с учетом масштаба блока)
        /// </summary>
        public double GetFullScale()
        {
            return GetScale() * BlockTransform.GetScale();
        }

        /// <summary>
        /// Возвращает коллекцию точек, которые используются для привязки
        /// </summary>
        public abstract IEnumerable<Point3d> GetPointsForOsnap();

        #region Block

        /// <summary>
        /// Идентификатор (ObjectId) блока
        /// </summary>
        public ObjectId BlockId { get; set; }

        /// <summary>
        /// Запись (описание) блока
        /// </summary>
        public BlockTableRecord BlockRecord
        {
            get
            {
                try
                {
                    if (!BlockId.IsNull)
                    {
                        using (AcadUtils.Document.LockDocument())
                        {
                            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                            {
                                var blkRef = (BlockReference)tr.GetObject(BlockId, OpenMode.ForWrite, true, true);
                                _blockRecord = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite, true, true);
                                if (_blockRecord.GetBlockReferenceIds(true, true).Count <= 1)
                                {
                                    // Debug.Print("Erasing");
                                    foreach (var objectId in _blockRecord)
                                    {
                                        tr.GetObject(objectId, OpenMode.ForWrite, true, true).Erase();
                                    }
                                }
                                else
                                {
                                    _blockRecord = new BlockTableRecord { Name = "*U", BlockScaling = BlockScaling.Uniform };
                                    using (var blockTable = AcadUtils.Database.BlockTableId.Write<BlockTable>())
                                    {
                                        // Debug.Print("Creating new (no erasing)");
                                        blockTable.Add(_blockRecord);
                                        tr.AddNewlyCreatedDBObject(_blockRecord, true);
                                    }

                                    blkRef.BlockTableRecord = _blockRecord.Id;
                                }

                                tr.Commit();
                            }

                            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                            {
                                var blkRef = (BlockReference)tr.GetObject(BlockId, OpenMode.ForWrite, true, true);
                                _blockRecord = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite, true, true);
                                _blockRecord.BlockScaling = BlockScaling.Uniform;

                                var matrix3D = Matrix3d.Displacement(-InsertionPoint.TransformBy(BlockTransform.Inverse()).GetAsVector());

                                // Debug.Print("Transformed copy");
                                foreach (var entity in _entitiesToBeDrawn)
                                {
                                    if (entity.Visible)
                                    {
                                        var transformedCopy = entity.GetTransformedCopy(matrix3D);
                                        _blockRecord.AppendEntity(transformedCopy);
                                        tr.AddNewlyCreatedDBObject(transformedCopy, true);
                                    }
                                }

                                tr.Commit();
                            }

                            AcadUtils.Document.TransactionManager.FlushGraphics();
                        }
                    }
                    else if (!IsValueCreated)
                    {
                        // Debug.Print("Value not created");
                        var matrix3D = Matrix3d.Displacement(-InsertionPoint.TransformBy(BlockTransform.Inverse()).GetAsVector());
                        foreach (var entity in _entitiesToBeDrawn)
                        {
                            var transformedCopy = entity.GetTransformedCopy(matrix3D);
                            _blockRecord.AppendEntity(transformedCopy);
                        }

                        IsValueCreated = true;
                    }
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }

                return _blockRecord;
            }
            set => _blockRecord = value;
        }

        /// <summary>
        /// Возвращает <see cref="BlockTableRecord"/> для обработки команды Undo
        /// </summary>
        /// <param name="blockReference"><see cref="BlockReference"/></param>
        public BlockTableRecord GetBlockTableRecordForUndo(BlockReference blockReference)
        {
            BlockTableRecord blockTableRecord;
            using (AcadUtils.Document.LockDocument())
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    blockTableRecord = new BlockTableRecord { Name = "*U", BlockScaling = BlockScaling.Uniform };
                    using (var blockTable = AcadUtils.Database.BlockTableId.Write<BlockTable>())
                    {
                        blockTable.Add(blockTableRecord);
                        tr.AddNewlyCreatedDBObject(blockTableRecord, true);
                    }

                    blockReference.BlockTableRecord = blockTableRecord.Id;
                    tr.Commit();
                }

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    blockTableRecord = (BlockTableRecord)tr.GetObject(blockReference.BlockTableRecord, OpenMode.ForWrite, true, true);
                    blockTableRecord.BlockScaling = BlockScaling.Uniform;
                    var matrix3D = Matrix3d.Displacement(-InsertionPoint.TransformBy(BlockTransform.Inverse()).GetAsVector());
                    foreach (var entity in _entitiesToBeDrawn)
                    {
                        var transformedCopy = entity.GetTransformedCopy(matrix3D);
                        blockTableRecord.AppendEntity(transformedCopy);
                        tr.AddNewlyCreatedDBObject(transformedCopy, true);
                    }

                    tr.Commit();
                }
            }

            _blockRecord = blockTableRecord;
            return blockTableRecord;
        }

#pragma warning disable CS0618 // Тип или член устарел
        /// <summary>
        /// Возвращает описание блока с открытием без использования транзакции
        /// </summary>
        /// <param name="blockReference">Вхождение блока</param>
        public BlockTableRecord GetBlockTableRecordWithoutTransaction(BlockReference blockReference)
        {
            BlockTableRecord blockTableRecord;
            using (AcadUtils.Document.LockDocument())
            {
                using (blockTableRecord = blockReference.BlockTableRecord.Open(OpenMode.ForWrite) as BlockTableRecord)
                {
                    if (blockTableRecord != null)
                    {
                        foreach (var objectId in blockTableRecord)
                        {
                            using (var ent = objectId.Open(OpenMode.ForWrite))
                            {
                                ent.Erase(true);
                            }
                        }

                        foreach (var entity in _entitiesToBeDrawn)
                        {
                            using (entity)
                            {
                                blockTableRecord.AppendEntity(entity);
                            }
                        }
                    }
                }
            }

            _blockRecord = blockTableRecord;
            return blockTableRecord;
        }
#pragma warning restore CS0618 // Тип или член устарел

        #endregion

        /// <summary>
        /// Получение свойств блока, которые присуще примитиву
        /// </summary>
        /// <param name="entity">Объект <see cref="DBObject"/></param>
        public void GetPropertiesFromCadEntity(DBObject entity)
        {
            var blockReference = (BlockReference)entity;
            if (blockReference != null)
            {
                InsertionPoint = blockReference.Position;
                BlockTransform = blockReference.BlockTransform;
            }
        }

        /// <summary>
        /// Идентификатор стиля
        /// </summary>
        [SaveToXData]
        public string StyleGuid { get; set; } = "00000000-0000-0000-0000-000000000000";

        /// <summary>
        /// Перерисовка элементов блока по параметрам ЕСКД элемента
        /// </summary>
        public abstract void UpdateEntities();

        /// <summary>
        /// Сериализация значений параметров, помеченных атрибутом <see cref="SaveToXDataAttribute"/>, в экземпляр <see cref="ResultBuffer"/>
        /// </summary>
        public ResultBuffer GetDataForXData()
        {
            return GetDataForXData("mp" + GetType().Name);
        }

        [CanBeNull]
        private ResultBuffer GetDataForXData(string appName)
        {
            try
            {
                // ReSharper disable once UseObjectOrCollectionInitializer
                var resultBuffer = new ResultBuffer();

                // 1001 - DxfCode.ExtendedDataRegAppName. AppName
                resultBuffer.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));

                var propertiesDataDictionary = new Dictionary<string, object>();
                foreach (var propertyInfo in GetType().GetProperties())
                {
                    var attribute = propertyInfo.GetCustomAttribute<SaveToXDataAttribute>();
                    if (attribute != null)
                    {
                        var value = propertyInfo.GetValue(this);
                        switch (value)
                        {
                            case AnnotationScale scale:
                                propertiesDataDictionary.Add(propertyInfo.Name, scale.Name);
                                break;
                            case Point3d point:
                                var vector = point.TransformBy(BlockTransform.Inverse()) - InsertionPointOCS;
                                propertiesDataDictionary.Add(propertyInfo.Name, vector.AsString());
                                break;
                            case List<Point3d> list:
                                var str = string.Join(
                                    "#",
                                    list.Select(p => (p.TransformBy(BlockTransform.Inverse()) - InsertionPointOCS).AsString()));
                                propertiesDataDictionary.Add(propertyInfo.Name, str);
                                break;
                            case Enum _:
                                propertiesDataDictionary.Add(propertyInfo.Name, value.ToString());
                                break;
                            default:
                                propertiesDataDictionary.Add(propertyInfo.Name, value);
                                break;
                        }
                    }
                }

                /*
                 * Так как SerializationBinder использует имя сборки, то данные сохраненные в 2013 версии, не будут
                 * работать в других, так как имя сборки отличается. В связи с этим вспомогательный класс хранения
                 * данных вынесен в ModPlusAPI, которая имеет всегда одинаковое имя и версию
                 */

                var dataHolder = new DataHolder(propertiesDataDictionary);
                var binaryFormatter = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    binaryFormatter.Serialize(ms, dataHolder);
                    ms.Position = 0;
                    AcadUtils.WriteMessageInDebug($"MemoryStream Length: {ms.Length} bytes or {ms.Length / 1024} KB");
                    var kMaxChunkSize = 127;
                    for (var i = 0; i < ms.Length; i += kMaxChunkSize)
                    {
                        var length = (int)Math.Min(ms.Length - i, kMaxChunkSize);
                        var dataChunk = new byte[length];
                        ms.Read(dataChunk, 0, length);
                        resultBuffer.Add(new TypedValue((int)DxfCode.ExtendedDataBinaryChunk, dataChunk));
                    }
                }

                return resultBuffer;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return null;
            }
        }

        /// <summary>
        /// Установка значений свойств, отмеченных атрибутом <see cref="SaveToXDataAttribute"/> из расширенных данных примитива AutoCAD
        /// </summary>
        /// <param name="resultBuffer"><see cref="ResultBuffer"/></param>
        /// <param name="skipPoints">Пропускать ли точки</param>
        public void SetPropertiesValuesFromXData(ResultBuffer resultBuffer, bool skipPoints = false)
        {
            try
            {
                var typedValue1001 = resultBuffer.AsArray().FirstOrDefault(tv =>
                    tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName && tv.Value.ToString() == "mp" + GetType().Name);
                if (typedValue1001.Value != null)
                {
                    var binaryFormatter = new BinaryFormatter { Binder = new Binder() };
                    var memoryStream = GetMemoryStreamFromResultBuffer(resultBuffer);
                    var deserialize = binaryFormatter.Deserialize(memoryStream);
                    if (deserialize is DataHolder dataHolder)
                    {
                        WritePropertiesFromReadedData(skipPoints, dataHolder.Data);
                    }
                }
            }
            catch (SerializationException)
            {
                // ignore
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private MemoryStream GetMemoryStreamFromResultBuffer(ResultBuffer resultBuffer)
        {
            var memoryStream = new MemoryStream();

            foreach (var typedValue in resultBuffer.AsArray()
                .Where(tv => tv.TypeCode == (int)DxfCode.ExtendedDataBinaryChunk))
            {
                var dataChunk = (byte[])typedValue.Value;
                memoryStream.Write(dataChunk, 0, dataChunk.Length);
            }

            memoryStream.Position = 0;

            return memoryStream;
        }

        /// <summary>
        /// Запись свойств текущего экземпляра интеллектуального объекта, полученных из расширенных
        /// данных блока в виде словаря
        /// </summary>
        /// <param name="skipPoints">Пропускать ли точки</param>
        /// <param name="data">Словарь данных</param>
        private void WritePropertiesFromReadedData(bool skipPoints, Dictionary<string, object> data)
        {
            /*
             * Сначала нужно установить свойство LineTypeScale
             * Затем нужно заполнить свойство Scale, так как масштаб может использоваться в сеттерах
             * многих свойств. А если он еще не установлен из словаря data, то будет браться по умолчанию 1:1
             * И только потом можно устанавливать прочие свойства
             */

            var properties = GetType().GetProperties();

            var property = properties.FirstOrDefault(p => p.Name == nameof(LineTypeScale));
            if (property != null && data.TryGetValue(nameof(LineTypeScale), out var dataValue))
            {
                property.SetValue(this, dataValue);
                data.Remove(nameof(LineTypeScale));
            }
            
            property = properties.FirstOrDefault(p => p.Name == nameof(Scale));
            if (property != null && data.TryGetValue(nameof(Scale), out dataValue))
            {
                Scale = AcadUtils.GetAnnotationScaleByName(dataValue?.ToString() ?? string.Empty);
                data.Remove(nameof(Scale));
            }

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<SaveToXDataAttribute>();
                if (attribute != null && data.ContainsKey(propertyInfo.Name))
                {
                    var valueForProperty = data[propertyInfo.Name] != null
                        ? data[propertyInfo.Name].ToString()
                        : string.Empty;
                    
                    if (string.IsNullOrEmpty(valueForProperty))
                        continue;

                    if (propertyInfo.Name == nameof(StyleGuid))
                    {
                        StyleGuid = valueForProperty;
                        Style = StyleManager.GetStyleNameByGuid(GetType(), valueForProperty);
                    }
                    else if (propertyInfo.PropertyType == typeof(Point3d))
                    {
                        if (skipPoints)
                            continue;

                        var vector = valueForProperty.ParseToPoint3d().GetAsVector();
                        var point = (InsertionPointOCS + vector).TransformBy(BlockTransform);
                        propertyInfo.SetValue(this, point);
                    }
                    else if (propertyInfo.PropertyType == typeof(List<Point3d>))
                    {
                        if (skipPoints)
                            continue;

                        var points = new List<Point3d>();
                        foreach (var s in valueForProperty.Split('#'))
                        {
                            var vector = s.ParseToPoint3d().GetAsVector();
                            var point = (InsertionPointOCS + vector).TransformBy(BlockTransform);
                            points.Add(point);
                        }

                        propertyInfo.SetValue(this, points);
                    }
                    else if (propertyInfo.PropertyType == typeof(int))
                    {
                        propertyInfo.SetValue(this, Convert.ToInt32(valueForProperty));
                    }
                    else if (propertyInfo.PropertyType == typeof(double))
                    {
                        propertyInfo.SetValue(this, Convert.ToDouble(valueForProperty));
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        propertyInfo.SetValue(this, Convert.ToBoolean(valueForProperty));
                    }
                    else if (propertyInfo.PropertyType.BaseType == typeof(Enum))
                    {
                        propertyInfo.SetValue(this, Enum.Parse(propertyInfo.PropertyType, valueForProperty));
                    }
                    else
                    {
                        propertyInfo.SetValue(this, valueForProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Копирование свойств, отмеченных атрибутом <see cref="SaveToXDataAttribute"/> из расширенных данных примитива AutoCAD
        /// в текущий интеллектуальный примитив
        /// </summary>
        /// <param name="sourceEntity">Интеллектуальный объекта</param>
        /// <param name="copyLayer">Копировать слой</param>
        public void SetPropertiesFromSmartEntity(SmartEntity sourceEntity, bool copyLayer)
        {
            var dataForXData = sourceEntity.GetDataForXData();
            if (dataForXData != null)
            {
                SetPropertiesValuesFromXData(dataForXData, true);

                if (sourceEntity.BlockId != ObjectId.Null)
                {
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var entity = tr.GetObject(sourceEntity.BlockId, OpenMode.ForRead) as Entity;
                        var destinationBlockReference = tr.GetObject(BlockId, OpenMode.ForWrite, true, true) as BlockReference;
                        if (entity != null && destinationBlockReference != null)
                        {
                            destinationBlockReference.LinetypeId = entity.LinetypeId;
                            if (copyLayer)
                            {
                                destinationBlockReference.Layer = entity.Layer;
                            }
                        }

                        tr.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Установка свойств для примитивов, которые не меняются
        /// </summary>
        /// <param name="entity">Примитив AutoCAD</param>
        public void SetImmutablePropertiesToNestedEntity(Entity entity)
        {
            entity.Layer = "0";
            entity.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
            entity.LineWeight = LineWeight.ByBlock;
            entity.Linetype = "Continuous";
            entity.LinetypeScale = 1.0;
        }

        /// <summary>
        /// Установка свойств для примитива, которые могут меняться "из вне" (ByBlock)
        /// </summary>
        /// <param name="entity">Примитив AutoCAD</param>
        public void SetChangeablePropertiesToNestedEntity(Entity entity)
        {
            entity.Layer = "0";
            entity.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
            entity.LineWeight = LineWeight.ByBlock;
            entity.Linetype = "ByBlock";
            entity.LinetypeScale = LineTypeScale;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _blockRecord?.Dispose();
        }
    }
}