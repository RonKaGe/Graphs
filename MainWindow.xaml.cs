using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Graphs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Главное окно приложения. Отвечает ТОЛЬКО за отображение.
    /// Вся логика будет в ViewModel, связанной через DataContext.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); // Важно! Эта строка генерируется автоматически и загружает XAML.
            
            // Позже здесь будет установка DataContext, например:
            // DataContext = new MainViewModel(...);
            
            // Временная подсказка в заголовке, что логика не подключена
            Title += " [UI Only - Logic Pending]";
        }
        
        // Пока НЕ добавляйте обработчики кликов по меню здесь!
        // Это нарушит принцип MVVM. Вместо этого мы позже используем Command.
    }
}
