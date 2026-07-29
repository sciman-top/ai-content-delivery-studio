using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using ContentDeliveryStudio.App.ViewModels;

namespace ContentDeliveryStudio.App;

public partial class MainWindow : Window
{
    private string? _lastLanguageSelectorAutomationName;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void LanguageSelectorAutomationName_TargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (e.Property != AutomationProperties.NameProperty || sender is not ComboBox comboBox)
        {
            return;
        }

        var currentName = AutomationProperties.GetName(comboBox);
        var previousName = _lastLanguageSelectorAutomationName;
        _lastLanguageSelectorAutomationName = currentName;

        if (previousName is null || previousName == currentName)
        {
            return;
        }

        UIElementAutomationPeer.FromElement(comboBox)?.RaisePropertyChangedEvent(
            AutomationElementIdentifiers.NameProperty,
            previousName,
            currentName);
    }
}
