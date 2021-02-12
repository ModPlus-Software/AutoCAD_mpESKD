namespace mpESKD.Functions.mpNodalLeader
{
    using System;
    using Base.Abstractions;

    /// <summary>
    /// Логика взаимодействия для NodalLeaderDoubleClickEditControl.xaml
    /// </summary>
    public partial class NodalLeaderDoubleClickEditControl : IDoubleClickEditControl
    {
        private NodalLeader _nodalLeader;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLeaderDoubleClickEditControl"/> class.
        /// </summary>
        public NodalLeaderDoubleClickEditControl()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
        }

        /// <inheritdoc/>
        public Type EntityType => typeof(NodalLeader);

        /// <inheritdoc/>
        public void Initialize(IWithDoubleClickEditor smartEntity)
        {
            if (!(smartEntity is NodalLeader nodalLeader))
                throw new ArgumentException("Wrong type of entity");

            _nodalLeader = nodalLeader;

            TbNodeNumber.Text = _nodalLeader.NodeNumber;
            TbNodeAddress.Text = _nodalLeader.NodeAddress;
            TbSheetNumber.Text = _nodalLeader.SheetNumber;
        }

        /// <inheritdoc/>
        public void OnAccept()
        {
            _nodalLeader.NodeNumber = TbNodeNumber.Text;
            _nodalLeader.NodeAddress = TbNodeAddress.Text;
            _nodalLeader.SheetNumber = TbSheetNumber.Text;
        }
    }
}
