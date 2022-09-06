using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using mpESKD.Base.Utils;

namespace mpESKD.Functions.mpNodalLeader
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class NodalLeaderFrameTypeMenu : Window
    {
        /// <inheritdoc/>
        
        private NodalLeader _nodalLeader;
        public NodalLeaderFrameTypeMenu(NodalLeader nodalLeader)
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
            _nodalLeader = nodalLeader;
        }

        //public void Initialize(NodalLeader smartEntity)
        //{
        //    if (!(smartEntity is NodalLeader nodalLeader))
        //        throw new ArgumentException("Wrong type of entity");
        //    _nodalLeader = nodalLeader;
        //}

        //private void Window_Initialized(object sender, System.EventArgs e)
        //{
        //    ContextMenu cm = this.FindResource("cmButton") as ContextMenu;
        //    cm.PlacementTarget = sender as Window;
        //    cm.IsOpen = true;
        //}

      
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] menus=
            {
                "Round",
                "Rectangle"
            };

            ContextMenu cm = this.FindResource("cmButton") as ContextMenu;
            cm.PlacementTarget = sender as Window;
            foreach (var prop in menus)
            {
                MenuItem item = new MenuItem();
                item.IsCheckable = true;
                item.Header = prop;
                item.Click += new RoutedEventHandler(MenuItem_OnClick);
                cm.Items.Add(item);
            }

            cm.IsOpen = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Close();

            MenuItem menuItem = (MenuItem)sender;
            var frameType = menuItem.Header.ToString();
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            if (frameType == "Round")
            {
                _nodalLeader.FrameType = FrameType.Round;
            }
            else
            {
                _nodalLeader.FrameType = FrameType.Rectangular;
            }
            
            
            _nodalLeader.UpdateEntities();
            _nodalLeader.BlockRecord.UpdateAnonymousBlocks();
            using (AcadUtils.Document.LockDocument())
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(_nodalLeader.BlockId, OpenMode.ForWrite, true, true);
                    
                    using (var resBuf = _nodalLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }
    }
}
