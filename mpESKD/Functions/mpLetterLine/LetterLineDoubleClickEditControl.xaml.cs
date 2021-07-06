namespace mpESKD.Functions.mpLetterLine
{
    using System;
    using Base.Abstractions;

    /// <summary>
    /// Interaction logic for LetterLineDoubleClickEditControl.xaml
    /// </summary>
    public partial class LetterLineDoubleClickEditControl : IDoubleClickEditControl
    {
        private LetterLine _letterLine { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LetterLineDoubleClickEditControl"/> class.
        /// </summary>
        public LetterLineDoubleClickEditControl()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
        }

        /// <inheritdoc/>
        public Type EntityType => typeof(LetterLine);
        public void Initialize(IWithDoubleClickEditor smartEntity)
        {
            if (!(smartEntity is LetterLine letterLine))
                throw new ArgumentException("Wrong type of entity");

            _letterLine = letterLine;
            
            TbDesignation.Text = _letterLine.MainText;
            TbDesignationPrefix.Text = _letterLine.MainText;
            //TbSheetNumber.Text = _letterLine.;
            TbDesignation.Focus();
        }

        public void OnAccept()
        {
            _letterLine.MainText = TbDesignation.Text;
            //_letterLine.DesignationPrefix = TbDesignationPrefix.Text;
            //_letterLine.SheetNumber = TbSheetNumber.Text;
        }
    }
}
