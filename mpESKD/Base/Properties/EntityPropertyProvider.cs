#pragma warning disable CS0618
namespace mpESKD.Base.Properties
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Attributes;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI.Windows;
    using Styles;
    using Utils;

    public class EntityPropertyProvider
    {
        public EntityPropertyProvider(ObjectId blkRefObjectId)
        {
            if (Verify(blkRefObjectId))
            {
                IsValid = true;
                _blkRefObjectId = blkRefObjectId;
                using (var blkRef = blkRefObjectId.Open(OpenMode.ForRead, false, true) as BlockReference)
                {
                    if (blkRef != null)
                    {
                        blkRef.Modified += BlkRef_Modified;
                        Create(blkRef);
                    }
                }
            }
            else
            {
                IsValid = false;
            }
        }

        /// <summary>
        /// Тип примитива
        /// </summary>
        public Type EntityType { get; private set; }

        /// <summary>
        /// Идентификатор вставки блока
        /// </summary>
        private ObjectId _blkRefObjectId;

        private SmartEntity _intellectualEntity;

        public ObservableCollection<SmartEntityProperty> Properties { get; } = new ObservableCollection<SmartEntityProperty>();

        public bool IsValid { get; set; }

        /// <summary>
        /// Событие, происходящее при попытке отредактировать свойство примитива, находящегося на заблокированном слое
        /// </summary>
        public event EventHandler<SmartEntityProperty> OnLockedLayerEventHandler;

        public bool Verify(ObjectId breakLineObjectId)
        {
            return !breakLineObjectId.IsNull &&
                   breakLineObjectId.IsValid &
                   !breakLineObjectId.IsErased &
                   !breakLineObjectId.IsEffectivelyErased;
        }

        private void BlkRef_Modified(object sender, EventArgs e)
        {
            var blkRef = sender as BlockReference;
            if (blkRef != null)
            {
                Update(blkRef);
            }
        }

        private void Create(BlockReference blockReference)
        {
            if (blockReference == null)
            {
                _blkRefObjectId = ObjectId.Null;
                return;
            }

            var intellectualEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
            if (intellectualEntity != null)
            {
                _intellectualEntity = intellectualEntity;

                var entityType = intellectualEntity.GetType();
                EntityType = entityType;
                foreach (var propertyInfo in entityType.GetProperties().Where(x => x.GetCustomAttribute<EntityPropertyAttribute>() != null))
                {
                    var regexInputRestrictionAttribute = propertyInfo.GetCustomAttribute<RegexInputRestrictionAttribute>();
                    var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                    if (attribute != null)
                    {
                        if (attribute.Name == "Style")
                        {
                            var property = new SmartEntityProperty(
                                attribute,
                                entityType,
                                StyleManager.GetStyleNameByGuid(entityType, _intellectualEntity.StyleGuid),
                                _blkRefObjectId,
                                regexInputRestrictionAttribute);
                            property.PropertyChanged += Property_PropertyChanged;
                            Properties.Add(property);
                        }
                        else if (attribute.Name == "LayerName")
                        {
                            var property = new SmartEntityProperty(
                                attribute, 
                                entityType,
                                blockReference.Layer,
                                _blkRefObjectId,
                                regexInputRestrictionAttribute);
                            property.PropertyChanged += Property_PropertyChanged;
                            Properties.Add(property);
                        }
                        else if (attribute.Name == "LineType")
                        {
                            var property = new SmartEntityProperty(
                                attribute,
                                entityType,
                                blockReference.Linetype,
                                _blkRefObjectId,
                                regexInputRestrictionAttribute);
                            property.PropertyChanged += Property_PropertyChanged;
                            Properties.Add(property);
                        }
                        else
                        {
                            var value = propertyInfo.GetValue(intellectualEntity);
                            if (value != null)
                            {
                                var property = new SmartEntityProperty(
                                    attribute, 
                                    entityType, 
                                    value,
                                    _blkRefObjectId,
                                    regexInputRestrictionAttribute);
                                property.PropertyChanged += Property_PropertyChanged;
                                Properties.Add(property);
                            }
                        }
                    }
                }
            }
        }

        private bool _isModifiedFromAutocad;

        /// <summary>
        /// Обработка события изменения примитива.
        /// Так как изменения свойства в палитре тоже вызывает изменение примитива, то в этом
        /// методе происходит зацикливание. Чтобы это исправить вводим дополнительную переменную
        /// <see cref="_isModifiedFromAutocad"/> чтобы не вызывать обработку события изменения
        /// свойства <see cref="Property_PropertyChanged"/>
        /// </summary>
        private void Update(BlockReference blockReference)
        {
            _isModifiedFromAutocad = true;
            try
            {
                if (blockReference == null)
                {
                    _blkRefObjectId = ObjectId.Null;
                    return;
                }

                var intellectualEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
                if (intellectualEntity != null)
                {
                    _intellectualEntity = intellectualEntity;

                    Debug.Print($"Style name: {_intellectualEntity.Style}");
                    Debug.Print($"Style guid: {_intellectualEntity.StyleGuid}");

                    var entityType = intellectualEntity.GetType();

                    foreach (var propertyInfo in entityType.GetProperties().Where(x => x.GetCustomAttribute<EntityPropertyAttribute>() != null))
                    {
                        var attribute = propertyInfo.GetCustomAttribute<EntityPropertyAttribute>();
                        if (attribute != null)
                        {
                            foreach (var property in Properties)
                            {
                                if (property.Name == attribute.Name)
                                {
                                    if (attribute.Name == "Style")
                                    {
                                        property.Value = StyleManager.GetStyleNameByGuid(entityType, _intellectualEntity.StyleGuid);
                                    }
                                    else if (attribute.Name == "LayerName")
                                    {
                                        property.Value = blockReference.Layer;
                                    }
                                    else if (attribute.Name == "LineType")
                                    {
                                        property.Value = blockReference.Linetype;
                                    }
                                    else
                                    {
                                        property.Value = propertyInfo.GetValue(intellectualEntity);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            _isModifiedFromAutocad = false;
        }

        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isModifiedFromAutocad)
            {
                return;
            }

            Overrule.Overruling = false;
            var intellectualEntityProperty = (SmartEntityProperty)sender;
            try
            {
                using (AcadUtils.Document.LockDocument())
                {
                    using (var blockReference = _blkRefObjectId.Open(OpenMode.ForWrite, true, true) as BlockReference)
                    {
                        var entityType = _intellectualEntity.GetType();
                        var propertyInfo = entityType.GetProperty(intellectualEntityProperty.Name);
                        if (propertyInfo != null)
                        {
                            if (intellectualEntityProperty.Name == "Style")
                            {
                                var style = StyleManager.GetStyleByName(entityType, intellectualEntityProperty.Value.ToString());
                                if (style != null)
                                {
                                    _intellectualEntity.ApplyStyle(style, false);
                                }
                            }
                            else if (intellectualEntityProperty.Name == "LayerName")
                            {
                                if (blockReference != null)
                                {
                                    blockReference.Layer = intellectualEntityProperty.Value.ToString();
                                }
                            }
                            else if (intellectualEntityProperty.Name == "LineType")
                            {
                                if (blockReference != null)
                                {
                                    blockReference.Linetype = intellectualEntityProperty.Value.ToString();
                                }
                            }
                            else
                            {
                                propertyInfo.SetValue(_intellectualEntity, intellectualEntityProperty.Value);
                            }

                            _intellectualEntity.UpdateEntities();
                            _intellectualEntity.GetBlockTableRecordWithoutTransaction(blockReference);
                            using (var resBuf = _intellectualEntity.GetDataForXData())
                            {
                                if (blockReference != null)
                                {
                                    blockReference.XData = resBuf;
                                }
                            }

                            if (blockReference != null)
                            {
                                blockReference.ResetBlock();
                            }
                        }
                    }
                }

                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
            catch (System.Exception exception)
            {
                if (exception.Message != "eOnLockedLayer")
                {
                    ExceptionBox.Show(exception);
                }
                else
                {
                    OnLockedLayerEventHandler?.Invoke(this, intellectualEntityProperty);
                }
            }

            Overrule.Overruling = true;
        }
    }
}