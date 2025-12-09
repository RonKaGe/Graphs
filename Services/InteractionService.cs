using GraphEditor.Core;
using GraphEditor.Visual;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphEditor.Services
{
    public enum WorkMode { Select, Drag, AddVertex, AddEdge }

    public class InteractionService
    {
        private readonly Canvas _canvas;
        private readonly IGraphModel _graphModel;
        private readonly GraphVisualModel _visualModel;
        private WorkMode _currentMode = WorkMode.Select;

        // Для перетаскивания
        private bool _isDragging = false;
        private string _draggedVertexId;
        private Point _dragStartPoint;

        // Для создания ребра
        private string _edgeStartVertexId;
        private Line _tempEdgeLine;

        // Для выделения
        private string _selectedElementId;
        public string SelectedElementId => _selectedElementId;

        // События
        public event Action<string> StatusChanged;
        public event Action<Point> VertexAddRequested;
        public event Action<string, string> EdgeAddRequested;
        public event Action<string> ElementSelected;
        public event Action SelectionCleared;
        public event Action VisualChanged;
        public event Action<string> DeleteRequested;
        public event Action<string, Point> VertexRightClicked;    // vertexId, position
        public event Action<string, Point> EdgeRightClicked;      // edgeId, position

        public InteractionService(Canvas canvas, IGraphModel graphModel, GraphVisualModel visualModel)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _graphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            _visualModel = visualModel ?? throw new ArgumentNullException(nameof(visualModel));

        }

        public void SetMode(WorkMode mode)
        {
            _currentMode = mode;

            // Сбрасываем состояние создания ребра
            if (mode != WorkMode.AddEdge)
            {
                _edgeStartVertexId = null;
                ClearTempEdge();
            }

            UpdateStatusForMode();
        }

        public WorkMode GetCurrentMode() => _currentMode;

        // === Обработка мыши ===

        public void HandleMouseDown(Point position, MouseButton button, ModifierKeys modifiers)
        {
            if (button == MouseButton.Left)
            {
                switch (_currentMode)
                {
                    case WorkMode.Select:
                        HandleSelectClick(position, modifiers);
                        break;
                    case WorkMode.Drag:
                        StartDragging(position);
                        break;
                    case WorkMode.AddVertex:
                        VertexAddRequested?.Invoke(position);
                        break;
                    case WorkMode.AddEdge:
                        HandleAddEdgeClick(position);
                        break;
                }
            }
            else if (button == MouseButton.Right)
            {
                HandleRightClick(position);
            }
        }

        public void HandleMouseMove(Point position)
        {
            // Обновляем временную линию при создании ребра
            if (_currentMode == WorkMode.AddEdge && _edgeStartVertexId != null && _tempEdgeLine != null)
            {
                var startPos = _visualModel.GetVertexPosition(_edgeStartVertexId) ?? new Point(0, 0);
                _tempEdgeLine.X2 = position.X;
                _tempEdgeLine.Y2 = position.Y;
            }

            // Обработка перетаскивания
            if (_currentMode == WorkMode.Drag && _isDragging && !string.IsNullOrEmpty(_draggedVertexId))
            {
                var delta = position - _dragStartPoint;

                var currentPos = _visualModel.GetVertexPosition(_draggedVertexId) ?? new Point(0, 0);
                var newPosition = new Point(currentPos.X + delta.X, currentPos.Y + delta.Y);
                _visualModel.SetVertexPosition(_draggedVertexId, newPosition);

                _dragStartPoint = position;
                VisualChanged?.Invoke();
            }
        }

        public void HandleMouseUp()
        {
            if (_isDragging)
            {
                StopDragging();
            }
        }

        public void HandleKeyDown(Key key, ModifierKeys modifiers)
        {
            if (key == Key.Delete)
            {
                if (!string.IsNullOrEmpty(_selectedElementId))
                {
                    DeleteRequested?.Invoke(_selectedElementId);
                }
            }
            else if (key == Key.Escape)
            {
                ClearSelection();
                SetMode(WorkMode.Select);
            }
        }

        // === Вспомогательные методы ===

        private void HandleSelectClick(Point position, ModifierKeys modifiers)
        {
            var elementId = FindElementAtPoint(position);

            if (!string.IsNullOrEmpty(elementId))
            {
                if (modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+click - переключаем выделение
                    ToggleElementSelection(elementId);
                }
                else
                {
                    // Обычный клик - выделяем только этот элемент
                    ClearSelection();
                    SelectElement(elementId);
                }
            }
            else
            {
                // Клик на пустом месте - снимаем выделение
                ClearSelection();
            }
        }

        private void HandleAddEdgeClick(Point position)
        {
            var vertexId = FindVertexAtPoint(position);

            if (!string.IsNullOrEmpty(vertexId))
            {
                if (_edgeStartVertexId == null)
                {
                    // Первый клик - выбираем начальную вершину
                    _edgeStartVertexId = vertexId;
                    StatusChanged?.Invoke($"Edge from {vertexId} - select target vertex");

                    // Показываем временную линию
                    StartTempEdge(vertexId, position);
                }
                else if (_edgeStartVertexId != vertexId)
                {
                    // Второй клик на другой вершине - создаём ребро
                    EdgeAddRequested?.Invoke(_edgeStartVertexId, vertexId);

                    _edgeStartVertexId = null;
                    ClearTempEdge();
                    SetMode(WorkMode.AddEdge);
                }
                else
                {
                    // Клик на той же вершине - отмена
                    StatusChanged?.Invoke("Edge creation cancelled");
                    _edgeStartVertexId = null;
                    ClearTempEdge();
                }
            }
            else
            {
                // Клик не на вершине
                if (_edgeStartVertexId != null)
                {
                    _edgeStartVertexId = null;
                    ClearTempEdge();
                }
            }
        }

        private void HandleRightClick(Point position)
        {
            var elementId = FindElementAtPoint(position);
            if (!string.IsNullOrEmpty(elementId))
            {
                if (_graphModel.Vertices.ContainsKey(elementId))
                {
                    VertexRightClicked?.Invoke(elementId, position);
                }
                else if (_graphModel.Edges.ContainsKey(elementId))
                {
                    EdgeRightClicked?.Invoke(elementId, position);
                }

                ElementSelected?.Invoke(elementId);
            }
        }

        private string FindElementAtPoint(Point position)
        {
            var vertexId = FindVertexAtPoint(position);
            if (!string.IsNullOrEmpty(vertexId))
                return vertexId;

            return FindEdgeAtPoint(position);
        }

        private string FindVertexAtPoint(Point position)
        {
            foreach (var vertex in _graphModel.Vertices.Values)
            {
                var vertexPos = _visualModel.GetVertexPosition(vertex.Id);
                if (vertexPos.HasValue)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(position.X - vertexPos.Value.X, 2) +
                        Math.Pow(position.Y - vertexPos.Value.Y, 2));

                    if (distance <= 30)
                        return vertex.Id;
                }
            }
            return null;
        }

        private string FindEdgeAtPoint(Point position)
        {
            foreach (var edge in _graphModel.Edges.Values)
            {
                var sourcePos = _visualModel.GetVertexPosition(edge.Source) ?? new Point(0, 0);
                var targetPos = _visualModel.GetVertexPosition(edge.Target) ?? new Point(0, 0);

                var distance = DistanceToLine(position, sourcePos, targetPos);
                if (distance < 8)
                    return edge.Id;
            }
            return null;
        }

        private double DistanceToLine(Point p, Point lineStart, Point lineEnd)
        {
            var lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
            if (lineLength == 0) return Math.Sqrt(Math.Pow(p.X - lineStart.X, 2) + Math.Pow(p.Y - lineStart.Y, 2));

            var t = Math.Max(0, Math.Min(1, ((p.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                                            (p.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / (lineLength * lineLength)));

            var projection = new Point(
                lineStart.X + t * (lineEnd.X - lineStart.X),
                lineStart.Y + t * (lineEnd.Y - lineStart.Y)
            );

            return Math.Sqrt(Math.Pow(p.X - projection.X, 2) + Math.Pow(p.Y - projection.Y, 2));
        }

        private void SelectElement(string elementId)
        {
            _selectedElementId = elementId;

            if (_graphModel.Vertices.ContainsKey(elementId))
            {
                _visualModel.SetVertexColor(elementId, Colors.Red);
            }
            else if (_graphModel.Edges.ContainsKey(elementId))
            {
                _visualModel.SetEdgeColor(elementId, Colors.Red);
            }

            ElementSelected?.Invoke(elementId);
            VisualChanged?.Invoke();
            StatusChanged?.Invoke($"Selected: {elementId}");
        }

        private void ToggleElementSelection(string elementId)
        {
            if (_selectedElementId == elementId)
            {
                ClearSelection();
            }
            else
            {
                SelectElement(elementId);
            }
        }

        private void StartDragging(Point position)
        {
            var vertexId = FindVertexAtPoint(position);
            if (!string.IsNullOrEmpty(vertexId))
            {
                _isDragging = true;
                _draggedVertexId = vertexId;
                _dragStartPoint = position;
                _canvas.CaptureMouse();
                StatusChanged?.Invoke($"Dragging vertex {vertexId}");
            }
        }

        private void StopDragging()
        {
            _isDragging = false;
            _draggedVertexId = null;
            _canvas.ReleaseMouseCapture();
            StatusChanged?.Invoke("Drag completed");
        }

        private void ClearSelection()
        {
            if (!string.IsNullOrEmpty(_selectedElementId))
            {
                if (_graphModel.Vertices.ContainsKey(_selectedElementId))
                    _visualModel.SetVertexColor(_selectedElementId, Colors.LightBlue);
                else if (_graphModel.Edges.ContainsKey(_selectedElementId))
                    _visualModel.SetEdgeColor(_selectedElementId, Colors.Black);

                _selectedElementId = null;
                SelectionCleared?.Invoke();
                VisualChanged?.Invoke();
            }
        }

        private void UpdateStatusForMode()
        {
            switch (_currentMode)
            {
                case WorkMode.Select:
                    StatusChanged?.Invoke("Mode: Select");
                    break;
                case WorkMode.Drag:
                    StatusChanged?.Invoke("Mode: Drag");
                    break;
                case WorkMode.AddVertex:
                    StatusChanged?.Invoke("Mode: Add Vertex");
                    break;
                case WorkMode.AddEdge:
                    StatusChanged?.Invoke("Mode: Add Edge");
                    break;
            }
        }

        private void StartTempEdge(string vertexId, Point position)
        {
            var startPos = _visualModel.GetVertexPosition(vertexId) ?? new Point(0, 0);

            _tempEdgeLine = new Line
            {
                X1 = startPos.X,
                Y1 = startPos.Y,
                X2 = position.X,
                Y2 = position.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Opacity = 0.7
            };

            _canvas.Children.Add(_tempEdgeLine);
        }

        private void ClearTempEdge()
        {
            if (_tempEdgeLine != null)
            {
                _canvas.Children.Remove(_tempEdgeLine);
                _tempEdgeLine = null;
            }
        }
    }
}