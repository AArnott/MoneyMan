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
	using System.Reflection;

	/// <summary>
	/// A sorted, observable collection.
	/// </summary>
	/// <typeparam name="T">The type of element kept by the collection.</typeparam>
	/// <remarks>
	/// <typeparamref name="T"/> should be immutable such that the sort result will never change,
	/// <em>or</em> it should implement <see cref="INotifyPropertyChanged"/> and raise <see cref="INotifyPropertyChanged.PropertyChanged"/>
	/// whenever a property that may impact sort order has changed.
	/// The <c>sender</c> argument on the <see cref="NotifyCollectionChangedEventHandler"/> must be set to the item in the collection.
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
		/// <param name="comparer">
		/// The comparer to use. When <see langword="null"/> the <see cref="Comparer{T}.Default"/> comparer is used.
		/// If this comparer implements <see cref="IOptimizedComparer{T}"/>, automated re-sorts may be skipped when possible.
		/// </param>
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
		void ICollection<T>.Add(T item) => this.Add(item);

		/// <inheritdoc cref="List{T}.Add(T)"/>
		/// <returns>The index of the item's position in the list.</returns>
		public int Add(T item)
		{
			// Before we resort to a binary search, see if it goes at the end of the list in case our input is already sorted.
			int index = (this.Count == 0 || this.comparer.Compare(item, this.list[^1]) > 0) ? this.Count : this.list.BinarySearch(item, this.comparer);
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

			return index;
		}

		/// <inheritdoc/>
		public bool Contains(T item) => this.IndexOf(item) >= 0;

		/// <inheritdoc/>
		bool ICollection<T>.Remove(T item) => this.Remove(item) >= 0;

		/// <inheritdoc cref="List{T}.Remove(T)"/>
		/// <returns>An index where the removed item had been; if the item was not found, a negative number is returned that is the bitwise complement of the index where it would have been.</returns>
		public int Remove(T item)
		{
			int index = this.IndexOf(item);
			if (index >= 0)
			{
				this.RemoveAt(index);
			}

			return index;
		}

		/// <summary>
		/// Removes the item at a given index.
		/// </summary>
		/// <param name="index">The index of the item to be removed.</param>
		public void RemoveAt(int index)
		{
			T item = this.list[index];
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
		/// Returns the index at which a given item was found in the collection.
		/// </summary>
		/// <param name="item">The item to search for.</param>
		/// <returns>The non-negative index within the list if the item was found; a negative number if it was not found, which is the bitwise complement of the index where it would have been.</returns>
		public int IndexOf(T item)
		{
			int index = this.list.BinarySearch(item, this.comparer);

			if (index >= 0)
			{
				// The binary search found one sort-equivalent match, but it may not be the same object.
				// Several similar objects may be sorted nearby.
				// Make sure we find an exact match.
				T foundItem = this[index];
				bool isClass = typeof(T).GetTypeInfo().IsClass;
				if (!Matches(foundItem))
				{
					// Look earlier in the list for an exact match.
					for (int i = index - 1; i >= 0 && this.comparer.Compare(item, this[i]) == 0; i--)
					{
						if (Matches(this[i]))
						{
							return i;
						}
					}

					// Look later in the list for an exact match.
					for (int i = index + 1; i < this.Count; i++)
					{
						if (Matches(this[i]))
						{
							return i;
						}
					}

					// No match.
					return ~index;
				}

				bool Matches(T candidate) => isClass ? ReferenceEquals(item, candidate) : EqualityComparer<T>.Default.Equals(item, candidate);
			}

			return index;
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
			if (sender is T item && (e.PropertyName is null || this.comparer is not IOptimizedComparer<T> optimized || optimized.IsPropertySignificant(e.PropertyName)))
			{
				// Consider whether this item should be repositioned in the list.
				(int OldIndex, int NewIndex) positions = this.list.UpdateSortPosition(item, this.comparer);
				if (positions.OldIndex != positions.NewIndex && this.CollectionChanged is object)
				{
					this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, positions.NewIndex, positions.OldIndex));
				}
			}
		}
	}
}
