namespace mpESKD;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Base;
using Base.Enums;
using Base.Utils;
using Functions.SearchEntities;
using ModPlus;
using ModPlusAPI;
using mpESKD.Base.Properties;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using StyleEditor = Base.View.StyleEditor;
using SystemVariableChangedEventArgs = Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs;

/// <summary>
/// Основные команды и инициализация приложения
/// </summary>
public class MainFunction : IExtensionApplication
{
    private static ContextMenuExtension _intellectualEntityContextMenu;
    private static StyleEditor _styleEditor;

    /// <summary>
    /// Путь к папке хранения пользовательских стилей
    /// </summary>
    public static string StylesPath { get; private set; } = string.Empty;

    /// <inheritdoc />
    public void Initialize()
    {
        StartUpInitialize();

        // Functions Init
        TypeFactory.Instance.GetEntityFunctionTypes().ForEach(f => f.Initialize());

        Overrule.Overruling = true;
            
        // ribbon build for
        Autodesk.Windows.ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;

        // palette
        var loadPropertiesPalette = MainSettings.Instance.AutoLoad;
        var addPropertiesPaletteToMpPalette = MainSettings.Instance.AddToMpPalette;

        if (loadPropertiesPalette & !addPropertiesPaletteToMpPalette)
        {
            PropertiesPaletteFunction.Start();
        }
        else if (loadPropertiesPalette & addPropertiesPaletteToMpPalette)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        AcApp.SystemVariableChanged += AcAppOnSystemVariableChanged;

        // bedit watcher
        BeditCommandWatcher.Initialize();
        AcApp.BeginDoubleClick += AcApp_BeginDoubleClick;

        AcadUtils.Documents.DocumentCreated += Documents_DocumentCreated;
        AcadUtils.Documents.DocumentActivated += Documents_DocumentActivated;

        foreach (Document document in AcadUtils.Documents)
        {
            document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;
        }
    }

    private void AcAppOnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
    {
        if (e.Name.Equals("WSCURRENT") && 
            MainSettings.Instance.AutoLoad & !MainSettings.Instance.AddToMpPalette)
        {
            PropertiesPaletteFunction.Start();
        }
    }

    /// <inheritdoc />
    public void Terminate()
    {
        DetachCreateAnalogContextMenu();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    public static void StartUpInitialize()
    {
        var curDir = Constants.CurrentDirectory;
        if (!string.IsNullOrEmpty(curDir))
        {
            var mpcoStylesPath = Path.Combine(Constants.UserDataDirectory, "Styles");
            if (!Directory.Exists(mpcoStylesPath))
            {
                Directory.CreateDirectory(mpcoStylesPath);
            }

            // set public parameter
            StylesPath = mpcoStylesPath;
        }
        else
        {
            ModPlusAPI.Windows.MessageBox.Show(
                Language.GetItem("err5"),
                ModPlusAPI.Windows.MessageBoxIcon.Close);
        }
    }

    /// <summary>
    /// Создание вкладки на ленте
    /// </summary>
    [CommandMethod("ModPlus", "mpESKDCreateRibbonTab", CommandFlags.Modal)]
    public void CreateRibbon()
    {
        if (Autodesk.Windows.ComponentManager.Ribbon == null)
        {
            return;
        }

        RibbonBuilder.BuildRibbon();
    }

    /// <summary>
    /// Команда открытия редактора стилей и настроек
    /// </summary>
    [CommandMethod("ModPlus", "mpStyleEditor", CommandFlags.Modal)]
    public static void OpenStyleEditor()
    {
        if (_styleEditor == null)
        {
            _styleEditor = new StyleEditor();
            _styleEditor.Closed += (sender, args) => _styleEditor = null;
        }

        if (_styleEditor.IsLoaded)
        {
            _styleEditor.Activate();
        }
        else
        {
            AcApp.ShowModalWindow(AcApp.MainWindow.Handle, _styleEditor, false);
        }
    }

    /// <summary>
    /// Команда "Создать аналог"
    /// </summary>
    [CommandMethod("ModPlus", "mpESKDCreateAnalog", CommandFlags.UsePickSet)]
    public void CreateAnalogCommand()
    {
        var psr = AcadUtils.Editor.SelectImplied();
        if (psr.Value == null || psr.Value.Count != 1) 
            return;

        SmartEntity intellectualEntity = null;
        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = new OpenCloseTransaction())
            {
                foreach (SelectedObject selectedObject in psr.Value)
                {
                    if (selectedObject.ObjectId == ObjectId.Null)
                    {
                        continue;
                    }

                    var obj = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
                    if (obj is BlockReference blockReference)
                    {
                        intellectualEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
                    }
                }

                tr.Commit();
            }
        }

        if (intellectualEntity == null)
            return;

        var copyLayer = true;
        var layerActionOnCreateAnalog = MainSettings.Instance.LayerActionOnCreateAnalog;
        if (layerActionOnCreateAnalog == LayerActionOnCreateAnalog.NotCopy)
        {
            copyLayer = false;
        }
        else if (layerActionOnCreateAnalog == LayerActionOnCreateAnalog.Ask)
        {
            var promptKeywordOptions =
                new PromptKeywordOptions($"\n{Language.GetItem("msg8")}", "Yes No");
            var promptResult = AcadUtils.Editor.GetKeywords(promptKeywordOptions);
            if (promptResult.Status == PromptStatus.OK)
            {
                if (promptResult.StringResult == "No")
                {
                    copyLayer = false;
                }
            }
            else
            {
                copyLayer = false;
            }
        }

        var function = TypeFactory.Instance.GetEntityFunctionTypes().FirstOrDefault(f =>
        {
            var functionName = $"{intellectualEntity.GetType().Name}Function";
            var fName = f.GetType().Name;
            return fName == functionName;
        });
        function?.CreateAnalog(intellectualEntity, copyLayer);
    }

    /// <summary>
    /// Создание блока для интеллектуального объекта
    /// </summary>
    /// <param name="intellectualEntity">Интеллектуальный объект</param>
    public static BlockReference CreateBlock(SmartEntity intellectualEntity)
    {
        BlockReference blockReference;
        using (AcadUtils.Document.LockDocument())
        {
            ObjectId objectId;
            using (var transaction = AcadUtils.Document.TransactionManager.StartTransaction())
            {
                using (var blockTable = AcadUtils.Database.BlockTableId.Write<BlockTable>())
                {
                    var blockTableRecordObjectId = blockTable.Add(intellectualEntity.BlockRecord);
                    blockReference = new BlockReference(intellectualEntity.InsertionPoint, blockTableRecordObjectId);
                    using (var blockTableRecord = AcadUtils.Database.CurrentSpaceId.Write<BlockTableRecord>())
                    {
                        blockTableRecord.BlockScaling = BlockScaling.Uniform;
                        objectId = blockTableRecord.AppendEntity(blockReference);
                    }

                    transaction.AddNewlyCreatedDBObject(blockReference, true);
                    transaction.AddNewlyCreatedDBObject(intellectualEntity.BlockRecord, true);
                }

                transaction.Commit();
            }

            intellectualEntity.BlockId = objectId;
        }

        return blockReference;
    }

    /// <summary>
    /// Подключение палитры свойств интеллектуальных примитивов к палитре ModPlus
    /// </summary>
    public static void AddToMpPalette()
    {
        var mpPaletteSet = MpPalette.MpPaletteSet;
        if (mpPaletteSet != null)
        {
            var flag = false;
            foreach (Palette palette in mpPaletteSet)
            {
                if (palette.Name.Equals(Language.GetItem("h11"))) //// Свойства примитивов ModPlus
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                var lmPalette = new PropertiesPalette();
                mpPaletteSet.Add(Language.GetItem("h11"), new ElementHost
                {
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Child = lmPalette
                });
                    
                mpPaletteSet.Visible = true;
            }
        }

        if (PropertiesPaletteFunction.PaletteSet != null)
        {
            PropertiesPaletteFunction.PaletteSet.Visible = false;
        }
    }

    /// <summary>
    /// Отключение палитры свойств интеллектуальных примитивов от палитры ModPlus
    /// </summary>
    /// <param name="fromSettings">True - метод запущен из окна настроек палитры</param>
    public static void RemoveFromMpPalette(bool fromSettings)
    {
        var mpPaletteSet = MpPalette.MpPaletteSet;
        if (mpPaletteSet != null)
        {
            var num = 0;
            while (num < mpPaletteSet.Count)
            {
                if (!mpPaletteSet[num].Name.Equals(Language.GetItem("h11")))
                {
                    num++;
                }
                else
                {
                    mpPaletteSet.Remove(num);
                    break;
                }
            }
        }

        if (PropertiesPaletteFunction.PaletteSet != null)
        {
            PropertiesPaletteFunction.PaletteSet.Visible = true;
        }
        else if (fromSettings)
        {
            if (AcApp.DocumentManager.MdiActiveDocument != null)
            {
                PropertiesPaletteFunction.Start();
            }
        }
    }

    private static void ComponentManager_ItemInitialized(object sender, Autodesk.Windows.RibbonItemEventArgs e)
    {
        if (Autodesk.Windows.ComponentManager.Ribbon == null)
            return;

        Autodesk.Windows.ComponentManager.Ribbon.BackgroundRenderFinished += RibbonOnBackgroundRenderFinished;

        RibbonBuilder.BuildRibbon();

        Autodesk.Windows.ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
    }

    private static void RibbonOnBackgroundRenderFinished(object sender, EventArgs e)
    {
        RibbonBuilder.BuildRibbon();
    }

    /// <summary>
    /// Обработка двойного клика по блоку
    /// </summary>
    private static void AcApp_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
    {
        var psr = AcadUtils.Editor.SelectImplied();
        if (psr.Status != PromptStatus.OK)
        {
            return;
        }

        var ids = psr.Value.GetObjectIds();

        if (ids.Length != 1) 
            return;

        using (AcadUtils.Document.LockDocument())
        {
            using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
            {
                var obj = tr.GetObject(ids[0], OpenMode.ForWrite, true, true);
                if (obj is BlockReference blockReference)
                {
                    var applicableAppName = ExtendedDataUtils.ApplicableAppName(blockReference);

                    if (string.IsNullOrEmpty(applicableAppName))
                        BeditCommandWatcher.UseBedit = true;
                    else
                        EntityUtils.DoubleClickEdit(blockReference);
                }
                else
                {
                    BeditCommandWatcher.UseBedit = true;
                }

                tr.Commit();
            }
        }
    }
        
    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (!args.Name.Contains("ModPlus_"))
            return null;

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        PropertiesPaletteFunction.Start();

        return null;
    }
        
    private static void Documents_DocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
        if (e.Document == null)
            return;

        e.Document.ImpliedSelectionChanged -= Document_ImpliedSelectionChanged;
        e.Document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;

        // при открытии документа соберу все блоки в текущем пространстве (?) и вызову обновление их внутренних
        // примитивов. Нужно, так как в некоторых случаях (пока не ясно в каких) внутренние примитивы отсутствуют
        try
        {
            var timer = Stopwatch.StartNew();
            using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var blockReference in SearchEntitiesCommand.GetBlockReferencesOfSmartEntities(
                             TypeFactory.Instance.GetEntityCommandNames(), tr))
                {
                    var ie = EntityReaderService.Instance.GetFromEntity(blockReference);
                    if (ie != null)
                    {
                        blockReference.UpgradeOpen();
                        ie.UpdateEntities();
                        ie.GetBlockTableRecordForUndo(blockReference)?.UpdateAnonymousBlocks();
                    }
                }

                tr.Commit();
            }

            timer.Stop();
            Debug.Print($"Time for update entities: {timer.ElapsedMilliseconds} milliseconds");
        }
        catch
        {
            // ignore
        }
    }

    private static void Documents_DocumentCreated(object sender, DocumentCollectionEventArgs e)
    {
        if (e.Document == null) 
            return;

        e.Document.ImpliedSelectionChanged -= Document_ImpliedSelectionChanged;
        e.Document.ImpliedSelectionChanged += Document_ImpliedSelectionChanged;
    }

    private static void Document_ImpliedSelectionChanged(object sender, EventArgs e)
    {
        var psr = AcadUtils.Editor.SelectImplied();
        var detach = true;
        if (psr.Value != null && psr.Value.Count == 1)
        {
            using (AcadUtils.Document.LockDocument())
            {
                using (var tr = new OpenCloseTransaction())
                {
                    foreach (SelectedObject selectedObject in psr.Value)
                    {
                        if (selectedObject.ObjectId == ObjectId.Null)
                        {
                            continue;
                        }

                        var obj = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
                        if (obj is BlockReference blockReference &&
                            ExtendedDataUtils.IsApplicable(blockReference))
                        {
                            AttachCreateAnalogContextMenu();
                            detach = false;
                        }
                    }

                    tr.Commit();
                }
            }
        }

        if (detach)
        {
            DetachCreateAnalogContextMenu();
        }
    }
        
    private static void AttachCreateAnalogContextMenu()
    {
        if (_intellectualEntityContextMenu == null)
        {
            _intellectualEntityContextMenu = new ContextMenuExtension();
            var menuItem = new Autodesk.AutoCAD.Windows.MenuItem(Language.GetItem("h95"));
            menuItem.Click += CreateAnalogMenuItem_Click;
            _intellectualEntityContextMenu.MenuItems.Add(menuItem);
        }

        var rxObject = RXObject.GetClass(typeof(BlockReference));
        Autodesk.AutoCAD.ApplicationServices.Application.AddObjectContextMenuExtension(
            rxObject, _intellectualEntityContextMenu);
    }

    private static void CreateAnalogMenuItem_Click(object sender, EventArgs e)
    {
        AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute(
            "_.mpESKDCreateAnalog ", false, false, false);
    }

    private static void DetachCreateAnalogContextMenu()
    {
        if (_intellectualEntityContextMenu == null)
            return;

        var rxObject = RXObject.GetClass(typeof(BlockReference));
        Autodesk.AutoCAD.ApplicationServices.Application.RemoveObjectContextMenuExtension(
            rxObject, _intellectualEntityContextMenu);
    }
}