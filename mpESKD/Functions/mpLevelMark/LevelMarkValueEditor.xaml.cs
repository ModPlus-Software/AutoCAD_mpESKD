namespace mpESKD.Functions.mpLevelMark
{
    using System.Windows;
    using Base;

    /// <summary>
    /// Логика взаимодействия для LevelMarkValueEditor.xaml
    /// </summary>
    public partial class LevelMarkValueEditor
    {
        private readonly LevelMark _levelMark;

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelMarkValueEditor"/> class.
        /// </summary>
        /// <param name="intellectualEntity">Редактируемый экземпляр интеллектуального объекта</param>
        public LevelMarkValueEditor(SmartEntity intellectualEntity)
        {
            _levelMark = (LevelMark)intellectualEntity;
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("h105");

            SetValues();
        }

        private void SetValues()
        {
            TbOverrideValue.Text = _levelMark.OverrideValue;
            TbNote.Text = _levelMark.Note;
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            OnAccept();
            DialogResult = true;
        }

        private void OnAccept()
        {
            _levelMark.OverrideValue = TbOverrideValue.Text;
            _levelMark.Note = TbNote.Text;
        }
    }
}
