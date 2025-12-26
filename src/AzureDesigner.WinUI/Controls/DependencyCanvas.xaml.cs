using System;
using System.Linq;
using System.Threading.Tasks;
using AzureDesigner.Models;
using AzureDesigner.WinUI.Models;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AzureDesigner.WinUI.Controls;

public sealed partial class DependencyCanvas : UserControl
{
    public DependencyCanvas()
    {
        InitializeComponent();
    }

    #region DependencyProperty Node

    public static readonly DependencyProperty NodeProperty =
        DependencyProperty.Register(
            nameof(Node),
            typeof(NodeViewModel),
            typeof(DependencyCanvas),
            new PropertyMetadata(null, OnNodeChanged));

    public NodeViewModel Node
    {
        get => (NodeViewModel)GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }

    private static async void OnNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DependencyCanvas canvas && e.NewValue is NodeViewModel node)
        {
            await canvas.DrawAsync(node);
        }
    }

    #endregion

    public async Task DrawAsync(NodeViewModel rootNode)
    {
        if (rootNode == null)
            return;

        canvas.Children.Clear();

        // Constants
        const double nodeSize = 36;
        const double imageSize = 32;
        const double borderThickness = 2;
        const double cornerRadius = 3;
        const double lineLength = 150;
        const double labelHeight = 18;
        const double labelMargin = 4;
        const double labelWidth = nodeSize * 2; // Double the nodeSize for TextBlock width

        // Center of canvas
        double canvasWidth = canvas.ActualWidth;
        double canvasHeight = canvas.ActualHeight;
        if (canvasWidth == 0) canvasWidth = canvas.MinWidth;
        if (canvasHeight == 0) canvasHeight = canvas.MinHeight;
        double centerX = canvasWidth / 2;
        double centerY = canvasHeight / 2;

        // Helper to create node visual (Image in Border + TextBlock in StackPanel)
        FrameworkElement CreateNodeVisual(NodeViewModel node, Color borderColor)
        {
            //var borderColor = node.Risks?.Any() == true ? Colors.Orange : Colors.DarkGray;
            var border = new Border
            {
                Width = nodeSize,
                Height = nodeSize,
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(borderThickness),
                CornerRadius = new CornerRadius(cornerRadius)
            };

            var image = new Image
            {
                Width = imageSize,
                Height = imageSize,
                Stretch = Stretch.Uniform
            };

            if (!string.IsNullOrEmpty(node.IconPath))
            {
                var path = $"ms-appx://{node.IconPath}";
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                var bitmap = new BitmapImage(uri);
                image.Source = bitmap;
            }

            border.Child = image;

            var textBlock = new TextBlock
            {
                Text = node.Name,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, labelMargin, 0, 0),
                Width = labelWidth,
                Height = labelHeight,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = labelWidth,
                Height = nodeSize + labelHeight + labelMargin
            };
            stackPanel.Children.Add(border);
            stackPanel.Children.Add(textBlock);

            return stackPanel;
        }

        // Draw root node
        // Step-by-step plan:
        // 1. Create a Flyout with desired content (e.g., node details).
        // 2. Attach the Flyout to rootVisual.
        // 3. Show the Flyout on PointerEntered (mouse hover).
        // 4. Hide the Flyout on PointerExited.

        // Add this inside DrawAsync after creating rootVisual:
        var borderColor = rootNode.Issues?.Any() == true ? Colors.Red : 
            (rootNode.Risks?.Any() == true ? Colors.Orange : Colors.DarkGray);  
        var rootVisual = CreateNodeVisual(rootNode, borderColor);
        AttachTooltip(rootNode, rootVisual);

        Canvas.SetLeft(rootVisual, centerX - labelWidth / 2);
        Canvas.SetTop(rootVisual, centerY - (nodeSize + labelHeight + labelMargin) / 2);
        canvas.Children.Add(rootVisual);

        // Draw dependencies radially
        var dependencies = rootNode.Dependencies?.ToList() ?? [];
        int depCount = dependencies.Count;
        if (depCount > 0)
        {
            double angleStep = 360.0 / depCount;
            for (int i = 0; i < depCount; i++)
            {
                var depNodeViewModel = dependencies[i];
                bool hasIssues = rootNode.Issues != null && rootNode.Issues.Any(o => o.DependencyIssues.ServiceId == depNodeViewModel.Id && o.DependencyIssues.Issues.Any());
                Color lineColor = hasIssues ? Colors.Red : Colors.Gray;
                double angleDeg = i * angleStep;
                double angleRad = angleDeg * Math.PI / 180.0;

                // Calculate dependency position
                double depX = centerX + lineLength * Math.Cos(angleRad);
                double depY = centerY + lineLength * Math.Sin(angleRad);

                // Calculate line start/end at border edge, avoiding label overlap
                double dx = depX - centerX;
                double dy = depY - centerY;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                // Offset from center to border edge (image only)
                double offset = nodeSize / 2;
                double labelOffset = (labelHeight + labelMargin) / 2;

                // For root node, start at image border
                double startX = centerX + (dx / dist) * offset;
                double startY = centerY + (dy / dist) * offset;

                // For dependency node, end at image border, but if arrow points downward, move up by label height
                double depVisualHeight = nodeSize + labelHeight + labelMargin;
                double endX = depX - (dx / dist) * offset;
                double endY = depY - (dy / dist) * offset;

                // If arrow points downward (dy > 0), move endY up by labelHeight/2 to avoid label overlap
                if (dy > 0)
                    endY -= labelHeight + labelMargin / 2;

                // If arrow points upward (dy < 0), move endY down by labelMargin/2 (optional, for symmetry)
                if (dy < 0)
                    endY += labelMargin / 2;

                // Draw arrow from root to dependency (border to border)
                var arrowLine = new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = startX,
                    Y1 = startY,
                    X2 = endX,
                    Y2 = endY,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2
                };
                //canvas.Children.Add(arrowLine);

                // Draw arrow head
                const double arrowHeadLength = 10;
                const double arrowHeadAngle = 30; // degrees

                double angle = Math.Atan2(endY - startY, endX - startX);

                // Calculate points for arrow head
                double arrowAngleRad = arrowHeadAngle * Math.PI / 180.0;

                double x1 = endX - arrowHeadLength * Math.Cos(angle - arrowAngleRad);
                double y1 = endY - arrowHeadLength * Math.Sin(angle - arrowAngleRad);

                double x2 = endX - arrowHeadLength * Math.Cos(angle + arrowAngleRad);
                double y2 = endY - arrowHeadLength * Math.Sin(angle + arrowAngleRad);

                var arrowHead1 = new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = endX,
                    Y1 = endY,
                    X2 = x1,
                    Y2 = y1,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2
                };
                canvas.Children.Add(arrowHead1);

                var arrowHead2 = new Microsoft.UI.Xaml.Shapes.Line
                {
                    X1 = endX,
                    Y1 = endY,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2
                };
                canvas.Children.Add(arrowHead2);

                // Draw dependency node

                var depVisual = CreateNodeVisual(depNodeViewModel, Colors.DarkGray);
                AttachTooltip(depNodeViewModel, depVisual);
                Canvas.SetLeft(depVisual, depX - labelWidth / 2);
                Canvas.SetTop(depVisual, depY - (nodeSize + labelHeight + labelMargin) / 2);
                canvas.Children.Add(depVisual);
                // After creating arrowLine, add this before canvas.Children.Add(arrowLine):
                if (hasIssues)
                {
                    //var issueDescriptions = rootNode.Issues
                    //    .Where(o => o.ServiceId == depNodeViewModel.Id && o.Descriptions.Any())
                    //    .SelectMany(o => o.Descriptions)
                    //    .ToList();

                    //var tooltipPanel = new StackPanel();
                    //tooltipPanel.Children.Add(new TextBlock
                    //{
                    //    Text = $"Issues with {depNodeViewModel.Name}!",
                    //    Foreground = new SolidColorBrush(Colors.Red),
                    //    FontWeight = FontWeights.Bold,
                    //    Margin = new Thickness(0, 0, 0, 4)
                    //});

                    //foreach (var desc in issueDescriptions)
                    //{
                    //    tooltipPanel.Children.Add(new TextBlock
                    //    {
                    //        Text = desc,
                    //        TextWrapping = TextWrapping.Wrap,
                    //        Margin = new Thickness(0, 0, 0, 2)
                    //    });
                    //}

                    //var tooltip = new ToolTip
                    //{
                    //    Content = tooltipPanel
                    //};

                    //ToolTipService.SetToolTip(arrowLine, tooltip);
                    var tooltip = CreateIssuesTooltip(rootNode, depNodeViewModel);
                    ToolTipService.SetToolTip(arrowLine, tooltip);
                }
                canvas.Children.Add(arrowLine);
            }
        }
    }

    // Keeping this - we might want to use Flyout instead of ToolTip for richer content and better control
    private static void AttachFlyout(NodeViewModel rootNode, FrameworkElement rootVisual)
    {
        var flyout = new Flyout
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = $"{rootNode.Name}", FontWeight = FontWeights.Bold },
                    new TextBlock { Text = $"Type: {rootNode.TypeFriendlyName}" },
                    new TextBlock { Text = $"Location: {rootNode.Location}" },
                    new TextBlock { Text = $"Resource Group: {rootNode.ResourceGroupName}" }
                }
            }
        };

        // Attach Flyout to rootVisual
        FlyoutBase.SetAttachedFlyout(rootVisual, flyout);

        // Show on hover
        rootVisual.PointerEntered += (s, e) =>
        {
            FlyoutBase.ShowAttachedFlyout(rootVisual);
        };

        //rootVisual.PointerExited += (s, e) =>
        //{
        //    flyout.Hide();
        //};
    }



    private static void AttachTooltip(NodeViewModel rootNode, FrameworkElement rootVisual)
    {
        var tooltip = new ToolTip
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = $"{rootNode.Name}", FontWeight = FontWeights.Bold },
                    new TextBlock { Text = $"Type: {rootNode.TypeFriendlyName}" },
                    new TextBlock { Text = $"Location: {rootNode.Location}" },
                    new TextBlock { Text = $"Resource Group: {rootNode.ResourceGroupName}" }
                }
            }
        };

        ToolTipService.SetToolTip(rootVisual, tooltip);
    }
    private static ToolTip CreateIssuesTooltip(NodeViewModel rootNode, NodeViewModel depNodeViewModel)
    {
        var issueDescriptions = rootNode.Issues
            .Where(o => o.DependencyIssues.ServiceId == depNodeViewModel.Id && o.DependencyIssues.Issues.Any())
            .SelectMany(o => o.DependencyIssues.Issues)
            .ToList();

        var tooltipPanel = new StackPanel();
        tooltipPanel.Children.Add(new TextBlock
        {
            Text = $"Issues with {depNodeViewModel.Name}!",
            Foreground = new SolidColorBrush(Colors.Red),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        foreach (var desc in issueDescriptions)
        {
            tooltipPanel.Children.Add(new TextBlock
            {
                Text = desc.Description,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 2)
            });
        }

        return new ToolTip
        {
            Content = tooltipPanel
        };
    }
}
