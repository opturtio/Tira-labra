﻿
namespace PathFinder2D.UI
{
    using PathFinder2D.DataStructures;
    using PathFinder2D.Managers;
    using PathFinder2D.PathFindingAlgorithms;
    using PathFinder2D.Services;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    public partial class MainWindow : Window
    {
        private Graph graph;
        private Node startNode, endNode;
        private PathVisualizerWPF visualizer;
        private FileLoader fileLoader;
        private FileModifier fileModifier;
        private Dijkstra dijkstra;
        private Astar aStar;
        private JPS jps;
        private bool isDraggingStartNode = false;
        private bool isDraggingEndNode = false;
        private int nodeSize = 20;
        private double zoomScale = 1.0;
        private const double ZoomFactor = 1.1;

        public MainWindow()
        {
            this.fileModifier = new FileModifier();
            this.fileLoader = new FileLoader();

            string mapString = this.fileLoader.LoadMapByName("Berlin_256x256.map");

            InitializeComponent();
            InitializeGraph(mapString);
            DrawGrid();
            DrawObstacles();

            PathCanvas.MouseWheel += PathCanvas_MouseWheel;
            this.KeyDown += MainWindow_KeyDown;

            Speed.ValueChanged += Speed_ValueChanged;

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Event handler for mouse wheel to handle zooming
        private void PathCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = (ScrollViewer)PathCanvas.Parent;
            var mousePos = e.GetPosition(PathCanvas);

            double scale = (e.Delta > 0) ? ZoomFactor : 1.0 / ZoomFactor;
            zoomScale *= scale;

            PathCanvas.LayoutTransform = new ScaleTransform(zoomScale, zoomScale, mousePos.X, mousePos.Y);

            e.Handled = true;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            var scrollViewer = (ScrollViewer)PathCanvas.Parent;
            double moveAmount = 10;

            switch (e.Key)
            {
                case Key.Up:
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - moveAmount);
                    break;
                case Key.Down:
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + moveAmount);
                    break;
                case Key.Left:
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - moveAmount);
                    break;
                case Key.Right:
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + moveAmount);
                    break;
            }

            e.Handled = true;
        }

        private void InitializeGraph(string mapString)
        {
            graph = GraphBuilder.CreateGraphFromString(mapString);
            visualizer = new PathVisualizerWPF(PathCanvas, graph)
            {
                Delay = 101 - (int)Speed.Value
            };

            dijkstra = new Dijkstra(graph, visualizer);
            aStar = new Astar(graph, visualizer);
            jps = new JPS(graph, visualizer);

            PathCanvas.Width = graph.Nodes[0].Count * nodeSize;
            PathCanvas.Height = graph.Nodes.Count * nodeSize;
        }

        private void ClearCanvas()
        {
            PathCanvas.Children.Clear();

            if (graph != null)
            {
                graph.ResetNodes();
            }

            startNode = null;
            endNode = null;
        }

        // Draw the grid on the canvas
        private void DrawGrid()
        {
            // Use the actual map size to draw the grid
            for (int i = 0; i <= graph.Nodes.Count; i++)
            {
                Line horizontalLine = new Line
                {
                    X1 = 0,
                    Y1 = i * nodeSize,
                    X2 = graph.Nodes[0].Count * nodeSize, // Width based on the map size
                    Y2 = i * nodeSize,
                    Stroke = Brushes.Gray,
                    Tag = "GridLine"
                };
                PathCanvas.Children.Add(horizontalLine);
            }

            for (int j = 0; j <= graph.Nodes[0].Count; j++)
            {
                Line verticalLine = new Line
                {
                    X1 = j * nodeSize,
                    X2 = j * nodeSize,
                    Y1 = 0,
                    Y2 = graph.Nodes.Count * nodeSize, // Height based on the map size
                    Stroke = Brushes.Gray,
                    Tag = "GridLine"
                };
                PathCanvas.Children.Add(verticalLine);
            }
        }

        // Draw obstacles on the canvas
        private void DrawObstacles()
        {
            foreach (var row in graph.Nodes)
            {
                foreach (var node in row)
                {
                    if (node.IsObstacle)
                    {
                        DrawNode(node.X, node.Y, Brushes.Black, "Obstacle");
                    }
                }
            }
        }

        // Handle mouse clicks on the canvas
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(PathCanvas);
            int x = (int)(clickPosition.X / nodeSize);
            int y = (int)(clickPosition.Y / nodeSize);

            if (graph.Nodes[y][x].IsObstacle) return;

            if (!IsRunning())
            {
                if (startNode != null && startNode.X == x && startNode.Y == y)
                {
                    isDraggingStartNode = true;
                    ClearAllExceptGridObstaclesAndNodes();
                    PathCanvas.CaptureMouse();
                    return;
                }

                if (endNode != null && endNode.X == x && endNode.Y == y)
                {
                    isDraggingEndNode = true;
                    PathCanvas.CaptureMouse();
                    return;
                }

                if (startNode == null)
                {
                    startNode = graph.Nodes[y][x];
                    DrawNode(x, y, Brushes.Green, "StartNode");
                }
                else if (endNode == null)
                {
                    endNode = graph.Nodes[y][x];
                    DrawNode(x, y, Brushes.Red, "EndNode");
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingStartNode || isDraggingEndNode)
            {
                Point position = e.GetPosition(PathCanvas);
                int x = (int)(position.X / nodeSize);
                int y = (int)(position.Y / nodeSize);

                if (x >= 0 && x < graph.Nodes[0].Count && y >= 0 && y < graph.Nodes.Count && !graph.Nodes[y][x].IsObstacle)
                {
                    if (isDraggingStartNode && (startNode.X != x || startNode.Y != y))
                    {
                        ClearNode(startNode.X, startNode.Y);
                        startNode = null;
                        startNode = graph.Nodes[y][x];
                        DrawNode(x, y, Brushes.Green, "StartNode");
                    }

                    if (isDraggingEndNode && (endNode.X != x || endNode.Y != y))
                    {
                        ClearNode(endNode.X, endNode.Y);
                        endNode = graph.Nodes[y][x];
                        DrawNode(x, y, Brushes.Red, "EndNode");
                    }
                }
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingStartNode || isDraggingEndNode)
            {
                isDraggingStartNode = false;
                isDraggingEndNode = false;
                PathCanvas.ReleaseMouseCapture();
            }
        }

        private void DrawNode(int x, int y, Brush color, string tag)
        {
            Rectangle rect = new Rectangle
            {
                Width = nodeSize,
                Height = nodeSize,
                Fill = color,
                Tag = tag
            };
            Canvas.SetLeft(rect, x * nodeSize);
            Canvas.SetTop(rect, y * nodeSize);
            PathCanvas.Children.Add(rect);
        }

        private void ClearNode(int x, int y)
        {
            foreach (var child in PathCanvas.Children.OfType<Rectangle>().ToList())
            {
                if (Canvas.GetLeft(child) == x * nodeSize && Canvas.GetTop(child) == y * nodeSize)
                {
                    PathCanvas.Children.Remove(child);
                    break;
                }
            }
        }

        // Method to clear everything except the grid and obstacles
        private void ClearAllExceptGridAndObstacles()
        {
            graph.ResetNodes();

            for (int i = PathCanvas.Children.Count - 1; i >= 0; i--)
            {
                UIElement child = PathCanvas.Children[i];
                if (child is FrameworkElement element && element.Tag?.ToString() != "GridLine" && element.Tag?.ToString() != "Obstacle")
                {
                    PathCanvas.Children.RemoveAt(i);
                }
            }
        }

        // Method to clear everything except the grid, obstacles, and start/end nodes
        private void ClearAllExceptGridObstaclesAndNodes()
        {
            graph.ResetNodes();

            for (int i = PathCanvas.Children.Count - 1; i >= 0; i--)
            {
                UIElement child = PathCanvas.Children[i];
                if (child is FrameworkElement element && element.Tag?.ToString() != "GridLine" && element.Tag?.ToString() != "Obstacle" && element.Tag?.ToString() != "StartNode" && element.Tag?.ToString() != "EndNode")
                {
                    PathCanvas.Children.RemoveAt(i);
                }
            }
        }

        private void CompareAlgorithms(object sender, RoutedEventArgs e)
        {
            if (startNode == null || endNode == null) return;
            StopRunning(sender, e);
            ClearAllExceptGridObstaclesAndNodes();

            RunDijkstra_Click(sender, e);
            RunAStar_Click(sender, e);
            RunJPS_Click(sender, e);
        }

        private void RunDijkstra_Click(object sender, RoutedEventArgs e)
        {
            if (startNode == null || endNode == null) return;
            StopRunning(sender, e);
            ClearAllExceptGridObstaclesAndNodes();

            dijkstra = new Dijkstra(graph, visualizer);
            dijkstra.FindShortestPath(startNode, endNode);

            DijkstraTime.Text = $"Time: {this.dijkstra.GetStopwatchTime()} ms";
            DijkstraCost.Text = $"Cost: {dijkstra.GetShortestPathCost()}";
            DijkstraNodes.Text = $"Visited Nodes: {this.dijkstra.GetVisitedNodes()}";
        }

        private void RunAStar_Click(object sender, RoutedEventArgs e)
        {
            if (startNode == null || endNode == null) return;
            StopRunning(sender, e);
            ClearAllExceptGridObstaclesAndNodes();

            aStar = new Astar(graph, visualizer);
            aStar.FindShortestPath(startNode, endNode);

            AStarTime.Text = $"Time: {this.aStar.GetStopwatchTime()} ms";
            AStarCost.Text = $"Cost: {this.aStar.GetShortestPathCost()}";
            AStarNodes.Text = $"Visited Nodes: {this.aStar.GetVisitedNodes()}";
        }

        private void RunJPS_Click(object sender, RoutedEventArgs e)
        {
            if (startNode == null || endNode == null) return;
            StopRunning(sender, e);
            ClearAllExceptGridObstaclesAndNodes();

            jps = new JPS(graph, visualizer);
            jps.FindShortestPath(startNode, endNode);

            JPSTime.Text = $"Time: {this.jps.GetStopwatchTime()} ms";
            JPSCost.Text = $"Cost: {this.jps.GetShortestPathLength()}";
            JPSNodes.Text = $"Visited Nodes: {this.jps.GetVisitedNodes()}";
        }

        private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (visualizer != null)
            {
                visualizer.Delay = 101 - (int)e.NewValue;
            }
        }

        private void VisualizationToggle(object sender, RoutedEventArgs e)
        {
            if (visualizer != null)
            {
                CheckBox checkbox = sender as CheckBox;
                if (checkbox != null)
                {
                    visualizer.VisualizationEnabled = checkbox.IsChecked == true;
                }
            }
        }

        private void StopRunning(object sender, RoutedEventArgs e)
        {
            this.dijkstra.StopRunning();
            this.aStar.StopRunning();
            this.jps.StopRunning();
            ClearAllExceptGridObstaclesAndNodes();
        }

        private bool IsRunning()
        {
            return this.jps.IsRunning() || this.dijkstra.IsRunning() || this.aStar.IsRunning();
        }

        private void ClearDrawnNodes(object sender, RoutedEventArgs e)
        {
            StopRunning(sender, e);
            ClearAllExceptGridAndObstacles();
            startNode = null;
            endNode = null;

            // Reset results
            DijkstraTime.Text = "Time: 0ms";
            DijkstraCost.Text = "Cost: 0";
            DijkstraNodes.Text = "Processed Nodes: 0";

            AStarTime.Text = "Time: 0ms";
            AStarCost.Text = "Cost: 0";
            AStarNodes.Text = "Processed Nodes: 0";

            JPSTime.Text = "Time: 0ms";
            JPSCost.Text = "Cost: 0";
            JPSNodes.Text = "Processed Nodes: 0";
        }

        // This method handles the DragEnter event
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        // This method handles the Drop event
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    string filePath = files[0];
                    ProcessDroppedFile(filePath);
                }
            }
        }

        // Method to process the dropped file
        private void ProcessDroppedFile(string filePath)
        {
            try
            {
                string fileName = System.IO.Path.GetFileName(filePath);

                string targetDirectory = fileLoader.ReturnDirectoryPath();

                string targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);

                if (File.Exists(targetFilePath))
                {
                    string loadedMap = fileLoader.LoadMapByName(fileName);
                    MessageBox.Show($"Map '{fileName}' already exists and has been loaded.", "Map Loaded", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearCanvas();
                    InitializeGraph(loadedMap);
                    DrawGrid();
                    DrawObstacles();
                }
                else
                {
                    // If it doesn't exist, copy the new file to the target directory
                    File.Copy(filePath, targetFilePath, overwrite: false);
                    MessageBox.Show($"File successfully dropped and saved to: {targetFilePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing dropped file: {ex.Message}");
            }
        }
    }
}
