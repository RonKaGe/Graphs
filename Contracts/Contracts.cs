namespace GraphEditor.Core
{
	using System;
	using System.Collections.Generic;

	public interface IVertex
	{
		string Id { get; }
		string? Label { get; }
		IReadOnlyDictionary<string, object>? Data { get; }
	}

	public interface IEdge
	{
		string Id { get; }
		string Source { get; }
		string Target { get; }
		double? Weight { get; }   // для Дейкстры/МСТ
		double? Capacity { get; } // для max-flow
		IReadOnlyDictionary<string, object>? Data { get; }
	}

	public interface IGraphModel
	{
		bool IsDirected { get; }
		bool AllowParallelEdges { get; }
		bool AllowSelfLoops { get; }

		IReadOnlyDictionary<string, IVertex> Vertices { get; }
		IReadOnlyDictionary<string, IEdge> Edges { get; }

		event EventHandler? Changed;

		// операции изменения
		bool AddVertex(string id, string? label = null);
		bool RemoveVertex(string id);
		bool AddEdge(string id, string source, string target, double? weight = null, double? capacity = null);
		bool RemoveEdge(string id);

		// обновления атрибутов
		bool SetVertexLabel(string id, string? label);
		bool SetEdgeWeight(string id, double? weight);
		bool SetEdgeCapacity(string id, double? capacity);

		// запросы
		bool TryGetVertex(string id, out IVertex? v);
		bool TryGetEdge(string id, out IEdge? e);
		IReadOnlyCollection<string> GetNeighbors(string vertexId);

		// матрицы (по требованию)
		double[,] BuildAdjacencyMatrix(bool useWeights = true);
		int[,] BuildIncidenceMatrix();
	}

	public abstract class GraphModelBase : IGraphModel
	{
		public bool IsDirected { get; protected init; } = false;
		public bool AllowParallelEdges { get; protected init; } = true;
		public bool AllowSelfLoops { get; protected init; } = true;

		public IReadOnlyDictionary<string, IVertex> Vertices { get; protected set; } =
			new Dictionary<string, IVertex>();
		public IReadOnlyDictionary<string, IEdge> Edges { get; protected set; } =
			new Dictionary<string, IEdge>();

		public event EventHandler? Changed;

		protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

		public abstract bool AddVertex(string id, string? label = null);
		public abstract bool RemoveVertex(string id);
		public abstract bool AddEdge(string id, string source, string target, double? weight = null, double? capacity = null);
		public abstract bool RemoveEdge(string id);

		public abstract bool SetVertexLabel(string id, string? label);
		public abstract bool SetEdgeWeight(string id, double? weight);
		public abstract bool SetEdgeCapacity(string id, double? capacity);

		public abstract bool TryGetVertex(string id, out IVertex? v);
		public abstract bool TryGetEdge(string id, out IEdge? e);
		public abstract IReadOnlyCollection<string> GetNeighbors(string vertexId);

		public abstract double[,] BuildAdjacencyMatrix(bool useWeights = true);
		public abstract int[,] BuildIncidenceMatrix();
	}

	// Простые DTO для удобства
	public class Vertex : IVertex
	{
		public string Id { get; init; } = string.Empty;
		public string? Label { get; init; }
		public IReadOnlyDictionary<string, object>? Data { get; init; }
	}

	public class Edge : IEdge
	{
		public string Id { get; init; } = string.Empty;
		public string Source { get; init; } = string.Empty;
		public string Target { get; init; } = string.Empty;
		public double? Weight { get; init; }
		public double? Capacity { get; init; }
		public IReadOnlyDictionary<string, object>? Data { get; init; }
	}
}

// Visual: позиции и цвета (для WPF)
namespace GraphEditor.Visual
{
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Media;
	using GraphEditor.Core;

	public interface ILayoutAlgorithm
	{
		// возвращает позиции вершин
		IReadOnlyDictionary<string, Point> ComputePositions(IGraphModel graph);
	}

	public interface IGraphVisualModel
	{
		// позиции и цвета
		IReadOnlyDictionary<string, Point> VertexPositions { get; }
		IReadOnlyDictionary<string, Color> VertexColors { get; }
		IReadOnlyDictionary<string, Color> EdgeColors { get; }

		// установка/получение
		void SetVertexPosition(string vertexId, Point position);
		Point? GetVertexPosition(string vertexId);

		void SetVertexColor(string vertexId, Color color);
		void SetEdgeColor(string edgeId, Color color);

		// автолейаут
		void ApplyLayout(IGraphModel graph, ILayoutAlgorithm algorithm);

		// сброс оформления
		void ResetStyles(Color defaultVertexColor, Color defaultEdgeColor);
	}

	public abstract class GraphVisualBase : IGraphVisualModel
	{
		public IReadOnlyDictionary<string, Point> VertexPositions { get; protected set; } =
			new Dictionary<string, Point>();
		public IReadOnlyDictionary<string, Color> VertexColors { get; protected set; } =
			new Dictionary<string, Color>();
		public IReadOnlyDictionary<string, Color> EdgeColors { get; protected set; } =
			new Dictionary<string, Color>();

		public abstract void SetVertexPosition(string vertexId, Point position);
		public abstract Point? GetVertexPosition(string vertexId);

		public abstract void SetVertexColor(string vertexId, Color color);
		public abstract void SetEdgeColor(string edgeId, Color color);

		public abstract void ApplyLayout(IGraphModel graph, ILayoutAlgorithm algorithm);
		public abstract void ResetStyles(Color defaultVertexColor, Color defaultEdgeColor);
	}
}

namespace GraphEditor.App
{
	using System;

	public interface IAppShell
	{
		void ShowError(string message, Exception? ex = null);
		void ShowWarning(string message);
		void ShowInfo(string message);
	}

	public abstract class AppShellBase : IAppShell
	{
		public abstract void ShowError(string message, Exception? ex = null);
		public abstract void ShowWarning(string message);
		public abstract void ShowInfo(string message);
	}
}
