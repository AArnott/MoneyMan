// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Diagnostics;

	/// <summary>
	/// A sorted, observable collection.
	/// </summary>
	/// <typeparam name="T">The type of element kept by the collection.</typeparam>
	/// <remarks>
	/// <typeparamref name="T"/> should be immutable such that the sort result will never change,
	/// <em>or</em> it should implement <see cref="INotifyPropertyChanged"/> and raise <see cref="INotifyPropertyChanged.PropertyChanged"/>
	/// whenever a property that may impact sort order has changed.
	/// The collection will automatically resort a changed item within the collection anytime this property has changed.
	/// </remarks>
	[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
	public class SortedObservableCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
	{
		private static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
		private static readonly PropertyChangedEventArgs CountPropertyChangedArgs = new(nameof(Count));

		private readonly IComparer<T> comparer;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly List<T> list = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedObservableCollection{T}"/> class.
		/// </summary>
		/// <param name="comparer">The comparer to use. When <see langword="null"/> the <see cref="Comparer{T}.Default"/> comparer is used.</param>
		public SortedObservableCollection(IComparer<T>? comparer = null)
		{
			this.comparer = comparer ?? Comparer<T>.Default;
		}

		/// <inheritdoc/>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count => this.list.Count;

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<T>.IsReadOnly => false;

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized => false;

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object ICollection.SyncRoot => this;

		/// <inheritdoc/>
		public T this[int index] => this.list[index];

		/// <inheritdoc/>
		void ICollection.CopyTo(Array array, int index) => ((ICollection)this.list).CopyTo(array, index);

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public void Add(T item)
		{
			int index = this.list.BinarySearch(item, this.comparer);
			if (index < 0)
			{
				index = ~index;
			}

			this.list.Insert(index, item);
			if (item is INotifyPropertyChanged observableItem)
			{
				observableItem.PropertyChanged += this.Item_PropertyChanged;
			}

			this.OnPropertyChanged(nameof(this.Count));
			if (this.CollectionChanged is object)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			}
		}

		/// <inheritdoc/>
		public bool Contains(T item) => this.IndexOf(item) >= 0;

		/// <inheritdoc/>
		public bool Remove(T item)
		{
			int index = this.IndexOf(item);
			if (index >= 0)
			{
				if (item is INotifyPropertyChanged observableItem)
				{
					observableItem.PropertyChanged -= this.Item_PropertyChanged;
				}

				this.list.RemoveAt(index);
				this.OnPropertyChanged(nameof(this.Count));
				if (this.CollectionChanged is object)
				{
					this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
				}

				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public void Clear()
		{
			if (this.list.Count > 0)
			{
				foreach (T item in this.list)
				{
					if (item is INotifyPropertyChanged observableItem)
					{
						observableItem.PropertyChanged -= this.Item_PropertyChanged;
					}
				}

				this.list.Clear();
				this.OnPropertyChanged(nameof(this.Count));
				this.OnCollectionChanged(ResetCollectionChanged);
			}
		}

		/// <summary>
		/// Returns an enumerator that enumerates through the list.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public List<T>.Enumerator GetEnumerator() => this.list.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">The name of the property that changed.</param>
		protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, propertyName == nameof(this.Count) ? CountPropertyChangedArgs : new PropertyChangedEventArgs(propertyName));

		/// <summary>
		/// Raises the <see cref="CollectionChanged"/> event.
		/// </summary>
		/// <param name="args">The arguments to pass to the handlers.</param>
		protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => this.CollectionChanged?.Invoke(this, args);

		private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is T item)
			{
				// Consider whether this item should be repositioned in the list.
				(int OldIndex, int NewIndex) positions = this.list.UpdateSortPosition(item, this.comparer);
				if (positions.OldIndex != positions.NewIndex && this.CollectionChanged is object)
				{
					this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, positions.NewIndex, positions.OldIndex));
				}
			}
		}

		private int IndexOf(T item)
		{
			// PERF: we could use binary search to find this more quickly, but be aware it may find any one of several in a row that sort as equal.
			return this.list.IndexOf(item);
		}
	}
}
