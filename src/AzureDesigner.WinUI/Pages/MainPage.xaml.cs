using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AzureDesigner.WinUI.Controls;
using AzureDesigner.WinUI.Models;
using AzureDesigner.WinUI.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AzureDesigner.Models; // coremodels alias in XAML

namespace AzureDesigner.WinUI.Pages
{
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<TraceMessage>? _traceMessages;

        public UIElement TitleBar => titleBar;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            viewModel.OnShowNotImplementedDialog += viewModel_OnShowNotImplementedDialog;

            viewModel.DependenciesDetermined += ViewModel_DependenciesDeterminedAsync;

            // Auto-scroll traces list when new items are added
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            HookTraceMessages(ViewModel.TraceMessages);
        }

        private void viewModel_OnShowNotImplementedDialog(object? sender, EventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Not Implemented";
            dialog.PrimaryButtonText = "OK";
            dialog.Content = new NotImplementedContent();
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = dialog.ShowAsync();
        }

        private void ViewModel_DependenciesDeterminedAsync(object? sender, NodeViewModelEventArgs e)
        {
            dependencyCanvas.DrawAsync(e.Node).GetAwaiter().GetResult();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.TraceMessages))
            {
                HookTraceMessages(ViewModel.TraceMessages);
            }
        }

        private void HookTraceMessages(ObservableCollection<TraceMessage>? messages)
        {
            if (_traceMessages != null)
                _traceMessages.CollectionChanged -= TraceMessages_CollectionChanged;

            _traceMessages = messages;

            if (_traceMessages != null)
                _traceMessages.CollectionChanged += TraceMessages_CollectionChanged;
        }

        private void TraceMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Add
                or NotifyCollectionChangedAction.Reset
                or NotifyCollectionChangedAction.Move
                or NotifyCollectionChangedAction.Replace)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (tracesListBox != null && tracesListBox.Items.Count > 0)
                    {
                        var last = tracesListBox.Items[tracesListBox.Items.Count - 1];
                        tracesListBox.ScrollIntoView(last);
                    }
                });
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            HookTraceMessages(null);
        }

        public MainViewModel ViewModel { get; set; }

        private void TreeViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NodeViewModel nodeViewModel)
            {
                ViewModel.SelectedService = nodeViewModel;               
            }
        }

        private void fixIssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Issue issue)
            {
                ViewModel.FixIssue(issue);
            }

        }

        private void fixRioskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Risk risk)
            {
                ViewModel.SelectedRisk= risk;
                ViewModel.FixRisk(); // 
            }
        }
    }
}
