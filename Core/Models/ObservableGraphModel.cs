using GraphEditor.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace GraphEditor.Core.Models
{
    public class ObservableGraphModel : GraphModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<IVertex> _observableVertices;
        private ObservableCollection<IEdge> _observableEdges;

        // Новые свойства для ObservableCollections
        public new ObservableCollection<IVertex> ObservableVertices => _observableVertices;
        public new ObservableCollection<IEdge> ObservableEdges => _observableEdges;

        // Сохраняем доступ к базовым словарям
        public new IReadOnlyDictionary<string, IVertex> Vertices => base.Vertices;
        public new IReadOnlyDictionary<string, IEdge> Edges => base.Edges;

        // Методы для проверки существования (аналоги ContainsKey)
        public bool ContainsVertex(string id) => base.Vertices.ContainsKey(id);
        public bool ContainsEdge(string id) => base.Edges.ContainsKey(id);

        public ObservableGraphModel(bool isDirected = false, bool allowParallelEdges = true, bool allowSelfLoops = true)
            : base(isDirected, allowParallelEdges, allowSelfLoops)
        {
            _observableVertices = new ObservableCollection<IVertex>();
            _observableEdges = new ObservableCollection<IEdge>();

            UpdateObservableCollections();
            base.Changed += OnGraphChanged;
        }

        private void OnGraphChanged(object? sender, EventArgs e)
        {
            UpdateObservableCollections();
            OnPropertyChanged(nameof(ObservableVertices));
            OnPropertyChanged(nameof(ObservableEdges));
            OnPropertyChanged(nameof(Vertices));
            OnPropertyChanged(nameof(Edges));
        }

        private void UpdateObservableCollections()
        {
            var currentVertices = base.Vertices.Values.ToList();
            SyncCollections(_observableVertices, currentVertices, v => v.Id);

            var currentEdges = base.Edges.Values.ToList();
            SyncCollections(_observableEdges, currentEdges, e => e.Id);
        }

        private void SyncCollections<T>(ObservableCollection<T> collection, List<T> newItems, Func<T, string> idSelector)
        {
            var toRemove = collection.Where(item => !newItems.Any(newItem => idSelector(newItem) == idSelector(item))).ToList();
            foreach (var item in toRemove)
            {
                collection.Remove(item);
            }

            for (int i = 0; i < newItems.Count; i++)
            {
                var newItem = newItems[i];
                var existingItem = collection.FirstOrDefault(item => idSelector(item) == idSelector(newItem));

                if (existingItem == null)
                {
                    collection.Insert(i, newItem);
                }
                else if (!existingItem.Equals(newItem))
                {
                    collection.Remove(existingItem);
                    collection.Insert(i, newItem);
                }
                else if (collection.IndexOf(existingItem) != i)
                {
                    collection.Move(collection.IndexOf(existingItem), i);
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool AddVertex(string id, string? label = null)
        {
            var result = base.AddVertex(id, label);
            if (result)
            {
                OnPropertyChanged(nameof(Vertices));
            }
            return result;
        }

        public override bool RemoveVertex(string id)
        {
            var result = base.RemoveVertex(id);
            if (result)
            {
                OnPropertyChanged(nameof(Vertices));
            }
            return result;
        }

        public override bool AddEdge(string id, string source, string target, double? weight = null, double? capacity = null)
        {
            var result = base.AddEdge(id, source, target, weight, capacity);
            if (result)
            {
                OnPropertyChanged(nameof(Edges));
            }
            return result;
        }

        public override bool RemoveEdge(string id)
        {
            var result = base.RemoveEdge(id);
            if (result)
            {
                OnPropertyChanged(nameof(Edges));
            }
            return result;
        }

        public override bool SetVertexLabel(string id, string? label)
        {
            var result = base.SetVertexLabel(id, label);
            if (result)
            {
                OnPropertyChanged(nameof(Vertices));
            }
            return result;
        }

        public override bool SetEdgeWeight(string id, double? weight)
        {
            var result = base.SetEdgeWeight(id, weight);
            if (result)
            {
                OnPropertyChanged(nameof(Edges));
            }
            return result;
        }

        public override bool SetEdgeCapacity(string id, double? capacity)
        {
            var result = base.SetEdgeCapacity(id, capacity);
            if (result)
            {
                OnPropertyChanged(nameof(Edges));
            }
            return result;
        }
    }
}