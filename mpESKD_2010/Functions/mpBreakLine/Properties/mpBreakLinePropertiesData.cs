﻿using System;
using Autodesk.AutoCAD.DatabaseServices;
using mpESKD.Base.Helpers;
// ReSharper disable InconsistentNaming

namespace mpESKD.Functions.mpBreakLine.Properties
{
    public class mpBreakLinePropertiesData
    {
        private ObjectId _blkRefObjectId;

        private int _overgang;
        public int Overhang
        {
            get => _overgang;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.Overhang = value;
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }
        private int _breakHeight;
        public int BreakHeight
        {
            get => _breakHeight;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.BreakHeight = value;
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }
        private int _breakWidth;
        public int BreakWidth
        {
            get => _breakWidth;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.BreakWidth = value;
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }

        private string _breakLineType;

        public string BreakLineType
        {
            get => _breakLineType;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.BreakLineType = mpBreakLinePropertiesHelpers.GetBreakLineTypeByLocalName(value);
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }

        private string _scale;

        public string Scale
        {
            get => _scale;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.Scale = AcadHelpers.GetAnnotationScaleByName(value);
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }

        private double _lineTypeScale;

        public double LineTypeScale
        {
            get => _lineTypeScale;
            set
            {
                using (AcadHelpers.Document.LockDocument())
                {
                    using (var blkRef = _blkRefObjectId.Open(OpenMode.ForWrite) as BlockReference)
                    {
                        using (var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkRef))
                        {
                            breakLine.LineTypeScale = value;
                            breakLine.UpdateEntities();
                            breakLine.GetBlockTableRecordWithoutTransaction(blkRef);
                            using (var resBuf = breakLine.GetParametersForXData())
                            {
                                blkRef.XData = resBuf;
                            }
                        }
                        blkRef.ResetBlock();
                    }
                }
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
        }

        public bool IsValid { get; set; }

        public mpBreakLinePropertiesData(ObjectId blkRefObjectId)
        {
            if (Verify(blkRefObjectId))
            {
                IsValid = true;
                _blkRefObjectId = blkRefObjectId;
                using (BlockReference blkRef = blkRefObjectId.Open(OpenMode.ForRead, false, true) as BlockReference)
                {
                    blkRef.Modified += BlkRef_Modified;
                    Update(blkRef);
                }
            }
            else IsValid = false;
        }

        private void BlkRef_Modified(object sender, EventArgs e)
        {
            BlockReference blkRef = sender as BlockReference;
            if (blkRef != null)
                Update(blkRef);
        }

        void Update(BlockReference blkReference)
        {
            if (blkReference == null)
            {
                _blkRefObjectId = ObjectId.Null;
                return;
            }
            var breakLine = BreakLineXDataHelper.GetBreakLineFromEntity(blkReference);
            if (breakLine != null)
            {
                _overgang = breakLine.Overhang;
                _breakHeight = breakLine.BreakHeight;
                _breakWidth = breakLine.BreakWidth;
                _breakLineType = mpBreakLinePropertiesHelpers.GetLocalBreakLineTypeName(breakLine.BreakLineType);
                _scale = breakLine.Scale.Name;
                _lineTypeScale = breakLine.LineTypeScale;
                AnyPropertyChangedReise();
            }
        }

        static bool Verify(ObjectId breakLineObjectId)
        {
            return !breakLineObjectId.IsNull &&
                   breakLineObjectId.IsValid &
                   !breakLineObjectId.IsErased &
                   !breakLineObjectId.IsEffectivelyErased;
        }

        public event EventHandler AnyPropertyChanged;
        /// <summary>
        /// Вызов события изменения какого-либо свойства
        /// </summary>
        protected void AnyPropertyChangedReise()
        {
            if (AnyPropertyChanged != null)
            {
                AnyPropertyChanged(this, null);
            }
        }
    }
}
