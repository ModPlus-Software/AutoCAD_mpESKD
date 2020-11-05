namespace mpESKD.Functions.mpSection
{
    using System;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Abstractions;
    using Base.Enums;
    using Base.Styles;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Overrules;

    /// <inheritdoc />
    public class SectionFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), SectionGripPointOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), SectionOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), SectionObjectOverrule.Instance(), true);
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), SectionGripPointOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), SectionOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), SectionObjectOverrule.Instance());
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(SectionDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(SectionDescriptor.Instance.Name);

                var sectionLastLetterValue = string.Empty;
                var sectionLastIntegerValue = string.Empty;
                FindLastSectionValues(ref sectionLastLetterValue, ref sectionLastIntegerValue);
                var section = new Section(sectionLastIntegerValue, sectionLastLetterValue);

                var blockReference = MainFunction.CreateBlock(section);

                section.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertSectionWithJig(true, section, blockReference);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Overrule.Overruling = true;
            }
        }

        /// <summary>
        /// Команда создания обозначения разреза
        /// </summary>
        [CommandMethod("ModPlus", "mpSection", CommandFlags.Modal)]
        public void CreateSectionCommand()
        {
            CreateSection(true);
        }

        /// <summary>
        /// Команда создания обозначения ломанного разреза
        /// </summary>
        [CommandMethod("ModPlus", "mpSectionBroken", CommandFlags.Modal)]
        public void CreateSimplySectionCommand()
        {
            CreateSection(false);
        }

        /// <summary>
        /// Команда создания обозначения разреза из полилинии
        /// </summary>
        [CommandMethod("ModPlus", "mpSectionFromPolyline", CommandFlags.Modal)]
        public void CreateSectionFromPolylineCommand()
        {
#if !DEBUG
            Statistic.SendCommandStarting("mpSectionFromPolyline", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                // Выберите полилинию:
                var peo = new PromptEntityOptions($"\n{Language.GetItem(Invariables.LangItem, "msg6")}")
                {
                    AllowNone = false,
                    AllowObjectOnLockedLayer = true
                };
                peo.SetRejectMessage($"\n{Language.GetItem(Invariables.LangItem, "wrong")}");
                peo.AddAllowedClass(typeof(Polyline), true);

                var per = AcadUtils.Editor.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(SectionDescriptor.Instance.Name);

                var style = StyleManager.GetCurrentStyle(typeof(Section));
                var sectionLastLetterValue = string.Empty;
                var sectionLastIntegerValue = string.Empty;
                FindLastSectionValues(ref sectionLastLetterValue, ref sectionLastIntegerValue);
                var section = new Section(sectionLastIntegerValue, sectionLastLetterValue);

                MainFunction.CreateBlock(section);
                section.ApplyStyle(style, true);

                var plineId = per.ObjectId;

                using (AcadUtils.Document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
                {
                    using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
                    {
                        var dbObj = tr.GetObject(plineId, OpenMode.ForRead);
                        if (dbObj is Polyline pline)
                        {
                            for (int i = 0; i < pline.NumberOfVertices; i++)
                            {
                                if (i == 0)
                                {
                                    section.InsertionPoint = pline.GetPoint3dAt(i);
                                }
                                else if (i == pline.NumberOfVertices - 1)
                                {
                                    section.EndPoint = pline.GetPoint3dAt(i);
                                }
                                else
                                {
                                    section.MiddlePoints.Add(pline.GetPoint3dAt(i));
                                }
                            }

                            section.UpdateEntities();
                            section.BlockRecord.UpdateAnonymousBlocks();

                            var ent = (BlockReference)tr.GetObject(section.BlockId, OpenMode.ForWrite, true, true);
                            ent.Position = pline.GetPoint3dAt(0);
                            ent.XData = section.GetDataForXData();
                        }

                        tr.Commit();
                    }

                    AcadUtils.Document.TransactionManager.QueueForGraphicsFlush();
                    AcadUtils.Document.TransactionManager.FlushGraphics();

                    // "Удалить исходную полилинию?"
                    if (MessageBox.ShowYesNo(Language.GetItem(Invariables.LangItem, "msg7"), MessageBoxIcon.Question))
                    {
                        using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                        {
                            var dbObj = tr.GetObject(plineId, OpenMode.ForWrite, true, true);
                            dbObj.Erase(true);
                            tr.Commit();
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void CreateSection(bool isSimple)
        {
#if !DEBUG
            Statistic.SendCommandStarting(SectionDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(SectionDescriptor.Instance.Name);

                var style = StyleManager.GetCurrentStyle(typeof(Section));
                var sectionLastLetterValue = string.Empty;
                var sectionLastIntegerValue = string.Empty;
                FindLastSectionValues(ref sectionLastLetterValue, ref sectionLastIntegerValue);
                var section = new Section(sectionLastIntegerValue, sectionLastLetterValue);

                var blockReference = MainFunction.CreateBlock(section);
                section.ApplyStyle(style, true);

                InsertSectionWithJig(isSimple, section, blockReference);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Overrule.Overruling = true;
            }
        }

        private static void InsertSectionWithJig(bool isSimple, Section section, BlockReference blockReference)
        {
            var nextPointPrompt = Language.GetItem(Invariables.LangItem, "msg5");
            var entityJig = new DefaultEntityJig(
                section,
                blockReference,
                new Point3d(20, 0, 0));
            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (isSimple)
                    {
                        if (entityJig.JigState == JigState.PromptInsertPoint)
                        {
                            entityJig.JigState = JigState.PromptNextPoint;
                            entityJig.PromptForNextPoint = nextPointPrompt;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        entityJig.JigState = JigState.PromptNextPoint;
                        if (entityJig.PreviousPoint == null)
                        {
                            entityJig.PreviousPoint = section.MiddlePoints.Any()
                                ? section.MiddlePoints.Last()
                                : section.InsertionPoint;
                        }
                        else
                        {
                            section.RebasePoints();
                            entityJig.PreviousPoint = section.MiddlePoints.Last();
                        }
                    }
                }
                else
                {
                    if (section.MiddlePoints.Any())
                    {
                        section.EndPoint = section.MiddlePoints.Last();
                        section.MiddlePoints.RemoveAt(section.MiddlePoints.Count - 1);
                        section.UpdateEntities();
                        section.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else
                    {
                        // if no middle points - remove entity
                        using (AcadUtils.Document.LockDocument())
                        {
                            using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                            {
                                var obj = (BlockReference)tr.GetObject(blockReference.Id, OpenMode.ForWrite, true, true);
                                obj.Erase(true);
                                tr.Commit();
                            }
                        }
                    }

                    break;
                }
            }
            while (true);

            if (!section.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(section.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = section.GetDataForXData();
                    tr.Commit();
                }
            }
        }

        /// <summary>
        /// Поиск последних цифровых и буквенных значений разрезов на текущем виде
        /// </summary>
        private static void FindLastSectionValues(ref string sectionLastLetterValue, ref string sectionLastIntegerValue)
        {
            if (MainSettings.Instance.SectionSaveLastTextAndContinueNew)
            {
                var sections = AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<Section>(typeof(Section));
                if (sections.Any())
                {
                    sections.Sort((s1, s2) => string.Compare(s1.BlockRecord.Name, s2.BlockRecord.Name, StringComparison.Ordinal));
                    var v = sections.Last().Designation;
                    if (int.TryParse(v, out var i))
                    {
                        sectionLastIntegerValue = i.ToString();
                    }
                    else
                    {
                        sectionLastLetterValue = v;
                    }
                }
            }
        }
    }
}
