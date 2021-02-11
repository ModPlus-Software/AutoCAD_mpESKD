namespace mpESKD.Base.View
{
    using System.Windows;
    using Abstractions;

    /// <summary>
    /// Логика взаимодействия для DoubleClickEditor.xaml
    /// </summary>
    public partial class DoubleClickEditor
    {
        private readonly IDoubleClickEditControl _entityEditControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleClickEditor"/> class.
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="entityEditControl">Элемент управления - редактор свойств интеллектуального объекта</param>
        public DoubleClickEditor(string title, IDoubleClickEditControl entityEditControl)
        {
            InitializeComponent();
            Title = title;
            _entityEditControl = entityEditControl;
            ContentControl.Content = entityEditControl;
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            _entityEditControl.OnAccept();
            DialogResult = true;
        }
    }
}
