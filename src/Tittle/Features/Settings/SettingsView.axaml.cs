using Avalonia.Controls;

namespace Tittle.Features.Settings;

/// <summary>Settings as an in-app page ("tab") rather than a floating window (☰ ▸ Раскладка / palette /
/// rail ⚙). DataContext is the shell <c>MainWindowViewModel</c>: the Раскладка/Чтение pages bind to its
/// shared <c>Layout</c>, Диаграммы to <c>Diagrams</c>, and Готово calls <c>CloseSettingsCommand</c> — so
/// every toggle persists and re-renders the chrome live, with no code-behind wiring.</summary>
public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();
}
