
namespace mpESKD.Functions.mpFragmentMarker
{
    using System;
    using Base.Abstractions;

    /// <summary>
    /// Interaction logic for FragmentMarkerDoubleClickEditControl.xaml
    /// </summary>
    public partial class FragmentMarkerDoubleClickEditControl : IDoubleClickEditControl
    {
        private FragmentMarker _fragmentMarker;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentMarkerDoubleClickEditControl"/> class.
        /// </summary>
        public FragmentMarkerDoubleClickEditControl()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
        }

        /// <inheritdoc/>
        public Type EntityType => typeof(FragmentMarker);

        /// <inheritdoc/>
        public void Initialize(IWithDoubleClickEditor smartEntity)
        {
            if (!(smartEntity is FragmentMarker fragmentMarker))
                throw new ArgumentException("Wrong type of entity");   

            _fragmentMarker = fragmentMarker;

            TbNodeNumber.Text = _fragmentMarker.MainText;
            TbNodeAddress.Text = _fragmentMarker.SmallText;
        }

        /// <inheritdoc/>
        public void OnAccept()
        {
            _fragmentMarker.MainText = TbNodeNumber.Text;
            _fragmentMarker.SmallText = TbNodeAddress.Text;
        }
    }
}
