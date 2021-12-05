// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class SortedObservableCollectionTests : TestBase
{
	private SortedObservableCollection<int> collection = new(new DescendingIntComparer());

	public SortedObservableCollectionTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void IsReadOnly_ICollectionOfT() => Assert.False(((ICollection<int>)this.collection).IsReadOnly);

	[Fact]
	public void IsReadOnly_IList() => Assert.False(((IList)this.collection).IsReadOnly);

	[Fact]
	public void IsFixedSize() => Assert.False(((IList)this.collection).IsFixedSize);

	[Fact]
	public void IsSynchronized() => Assert.False(((ICollection)this.collection).IsSynchronized);

	[Fact]
	public void SyncRoot() => Assert.NotNull(((ICollection)this.collection).SyncRoot);

	[Fact]
	public void Add()
	{
		Assert.Equal(0, this.collection.Add(5));
		Assert.Equal(5, Assert.Single(this.collection));

		Assert.Equal(1, this.collection.Add(3));
		Assert.Equal(0, this.collection.Add(7));
	}

	[Fact]
	public void Add_ICollectionOfT()
	{
		ICollection<int> collection = this.collection;
		collection.Add(5);
		Assert.Equal(5, Assert.Single(collection));
	}

	[Fact]
	public void Add_IList()
	{
		IList collection = this.collection;
		collection.Add(5);
		Assert.Equal(5, Assert.Single(collection));
	}

	[Fact]
	public void Insert_IListOfT()
	{
		IList<int> collection = this.collection;
		Assert.Throws<NotSupportedException>(() => collection.Insert(0, 5));
	}

	[Fact]
	public void Insert_IList()
	{
		IList collection = this.collection;
		Assert.Throws<NotSupportedException>(() => collection.Insert(0, 5));
	}

	[Fact]
	public void Indexer()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => this.collection[0]);
		this.collection.Add(3);
		this.collection.Add(5);
		Assert.Equal(5, this.collection[0]);
		Assert.Equal(3, this.collection[1]);
	}

	[Fact]
	public void Indexer_IList()
	{
		IList collection = this.collection;
		collection.Add(5);
		Assert.Equal(5, collection[0]);
		Assert.Throws<NotSupportedException>(() => collection[0] = 3);
	}

	[Fact]
	public void Indexer_IListOfT()
	{
		IList<int> collection = this.collection;
		collection.Add(5);
		Assert.Equal(5, collection[0]);
		Assert.Throws<NotSupportedException>(() => collection[0] = 3);
	}

	[Fact]
	public void CopyTo()
	{
		this.collection.CopyTo(Array.Empty<int>(), 0);
		int[] target = new int[4];
		this.collection.Add(1);
		this.collection.Add(3);
		this.collection.Add(5);
		this.collection.CopyTo(target, 1);
		Assert.Equal(new[] { 0, 5, 3, 1 }, target);
	}

	[Fact]
	public void CopyTo_NonGeneric()
	{
		ICollection collection = this.collection;
		collection.CopyTo(Array.Empty<int>(), 0);
		int[] target = new int[4];
		this.collection.Add(1);
		this.collection.Add(3);
		this.collection.Add(5);
		collection.CopyTo(target, 1);
		Assert.Equal(new[] { 0, 5, 3, 1 }, target);
	}

	[Fact]
	public void Contains()
	{
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection
		Assert.False(this.collection.Contains(1));
		this.collection.Add(1);
		Assert.True(this.collection.Contains(1));
#pragma warning restore xUnit2017 // Do not use Contains() to check if a value exists in a collection
	}

	[Fact]
	public void Contains_IList()
	{
		IList collection = this.collection;
		Assert.False(collection.Contains(1));
		this.collection.Add(1);
		Assert.True(collection.Contains(1));
	}

	[Fact]
	public void Contains_IList_WrongType()
	{
		IList collection = this.collection;
		Assert.False(collection.Contains("wrong type"));
	}

	[Fact]
	public void Contains_IList_NullValue()
	{
		IList collection = this.collection;
		Assert.False(collection.Contains(null));

		collection = new SortedObservableCollection<object?>();
		Assert.False(collection.Contains(null));
		collection.Add(null);
		Assert.True(collection.Contains(null));
	}

	[Fact]
	public void Remove()
	{
		Assert.Equal(~0, this.collection.Remove(1));
		this.collection.Add(3);
		this.collection.Add(5);
		Assert.Equal(1, this.collection.Remove(3));
		Assert.Equal(0, this.collection.Remove(5));
		Assert.Equal(~0, this.collection.Remove(5));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void RemoveWhereMultipleIdentialRefTypesExist(int indexToRemove)
	{
		SortedObservableCollection<ObservableMutableClass> collection = new(new MutableClassComparer());
		ObservableMutableClass[] items = new[]
		{
			new ObservableMutableClass(1),
			new ObservableMutableClass(1),
			new ObservableMutableClass(1),
			new ObservableMutableClass(1),
		};
		foreach (ObservableMutableClass item in items)
		{
			collection.Add(item);
		}

		collection.Remove(items[indexToRemove]);
		for (int i = 0; i < items.Length; i++)
		{
			if (i == indexToRemove)
			{
				Assert.DoesNotContain(items[i], collection);
			}
			else
			{
				Assert.Contains(items[i], collection);
			}
		}

		// Attempt to remove an equivalent value that is not actually in the collection.
		Assert.True(collection.Remove(new ObservableMutableClass(1)) < 0);
	}

	[Fact]
	public void Remove_ICollectionOfT()
	{
		ICollection<int> collection = this.collection;
		Assert.False(collection.Remove(1));
		collection.Add(3);
		collection.Add(5);
		Assert.True(collection.Remove(3));
		Assert.True(collection.Remove(5));
		Assert.False(collection.Remove(5));
	}

	[Fact]
	public void Remove_IList()
	{
		IList collection = this.collection;
		collection.Remove(1);
		collection.Add(3);
		collection.Add(5);
		collection.Remove(3);
		collection.Remove(5);
	}

	[Fact]
	public void RemoveAt()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => this.collection.RemoveAt(0));
		this.collection.Add(3);
		this.collection.Add(5);
		this.collection.RemoveAt(1);
		Assert.Single(this.collection);
		this.collection.RemoveAt(0);
		Assert.Empty(this.collection);
	}

	[Fact]
	public void IndexOf()
	{
		// The concrete public method returns the bitwise complement of the sorted location of where an item *would* be when not found.
		Assert.Equal(~0, this.collection.IndexOf(3));
		this.collection.Add(5);
		this.collection.Add(10);
		Assert.Equal(~0, this.collection.IndexOf(15));
		Assert.Equal(~1, this.collection.IndexOf(7));
		Assert.Equal(~2, this.collection.IndexOf(3));
	}

	[Fact]
	public void IndexOf_IList()
	{
		// This interface is documented as returning exactly -1 when items are not found.
		IList collection = this.collection;
		Assert.Equal(~0, collection.IndexOf(3));
		this.collection.Add(5);
		this.collection.Add(10);
		Assert.Equal(-1, collection.IndexOf(15));
		Assert.Equal(-1, collection.IndexOf(7));
		Assert.Equal(-1, collection.IndexOf(3));
	}

	[Fact]
	public void IndexOf_IListOfT()
	{
		// This interface is documented as returning exactly -1 when items are not found.
		IList<int> collection = this.collection;
		Assert.Equal(~0, collection.IndexOf(3));
		this.collection.Add(5);
		this.collection.Add(10);
		Assert.Equal(-1, collection.IndexOf(15));
		Assert.Equal(-1, collection.IndexOf(7));
		Assert.Equal(-1, collection.IndexOf(3));
	}

	[Fact]
	public void Clear()
	{
		this.collection.Clear();
		this.collection.Add(3);
		this.collection.Clear();
		Assert.Empty(this.collection);
	}

	[Fact]
	public void Add_RaisesPropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => this.collection.Add(5), nameof(this.collection.Count));
	}

	[Fact]
	public void Remove_RaisesPropertyChanged()
	{
		this.collection.Add(3);
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => Assert.Equal(0, this.collection.Remove(3)), nameof(this.collection.Count));
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => Assert.Equal(~0, this.collection.Remove(3)), invertExpectation: true, nameof(this.collection.Count));
	}

	[Fact]
	public void Clear_RaisesPropertyChanged()
	{
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => this.collection.Clear(), invertExpectation: true, nameof(this.collection.Count));
		this.collection.Add(5);
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => this.collection.Clear(), nameof(this.collection.Count));
	}

	[Fact]
	public void Add_RaisesCollectionChanged()
	{
		NotifyCollectionChangedEventArgs args = TestUtilities.AssertCollectionChangedEvent(this.collection, () => this.collection.Add(5));
		Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
		Assert.Null(args.OldItems);
		Assert.Equal(new[] { 5 }, args.NewItems);
		Assert.Equal(0, args.NewStartingIndex);

		args = TestUtilities.AssertCollectionChangedEvent(this.collection, () => this.collection.Add(10));
		Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
		Assert.Null(args.OldItems);
		Assert.Equal(new[] { 10 }, args.NewItems);
		Assert.Equal(0, args.NewStartingIndex);

		args = TestUtilities.AssertCollectionChangedEvent(this.collection, () => this.collection.Add(1));
		Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
		Assert.Null(args.OldItems);
		Assert.Equal(new[] { 1 }, args.NewItems);
		Assert.Equal(2, args.NewStartingIndex);
	}

	[Fact]
	public void Remove_RaisesCollectionChanged()
	{
		TestUtilities.AssertNoCollectionChangedEvent(this.collection, () => this.collection.Remove(3));

		this.collection.Add(3);
		this.collection.Add(5);
		NotifyCollectionChangedEventArgs args = TestUtilities.AssertCollectionChangedEvent(this.collection, () => this.collection.Remove(3));
		Assert.Equal(NotifyCollectionChangedAction.Remove, args.Action);
		Assert.Null(args.NewItems);
		Assert.Equal(new[] { 3 }, args.OldItems);
		Assert.Equal(1, args.OldStartingIndex);
	}

	[Fact]
	public void Clear_RaisesCollectionChanged()
	{
		TestUtilities.AssertNoCollectionChangedEvent(this.collection, () => this.collection.Clear());
		this.collection.Add(5);
		NotifyCollectionChangedEventArgs args = TestUtilities.AssertCollectionChangedEvent(this.collection, () => this.collection.Clear());
		Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
		Assert.Null(args.NewItems);
		Assert.Null(args.OldItems);
	}

	[Fact]
	public void ItemChangesResortCollection()
	{
		SortedObservableCollection<ObservableMutableClass> collection = new(new MutableClassComparer());
		ObservableMutableClass a = new(1);
		ObservableMutableClass b = new(2);
		collection.Add(a);
		collection.Add(b);

		NotifyCollectionChangedEventArgs args = TestUtilities.AssertCollectionChangedEvent(collection, () => a.Value = 3);
		Assert.Equal(NotifyCollectionChangedAction.Move, args.Action);
		Assert.Equal(0, args.OldStartingIndex);
		Assert.Equal(1, args.NewStartingIndex);
		Assert.Same(a, Assert.Single(args.OldItems));
		Assert.Same(a, Assert.Single(args.NewItems));
		Assert.Equal(new[] { b, a }, collection);
	}

	[Fact]
	public void ItemChangesToUnimportantPropertiesDoNotTriggerResort()
	{
		MutableClassComparer comparer = new MutableClassComparer();
		SortedObservableCollection<ObservableMutableClass> collection = new(comparer);
		ObservableMutableClass a = new(1);
		ObservableMutableClass b = new(2);
		collection.Add(a);
		collection.Add(b);

		int oldCount = comparer.InvocationCount;
		a.OtherProperty = 3;
		Assert.Equal(oldCount, comparer.InvocationCount);
	}

	[Fact]
	public void ItemChangesWithNullPropertyName()
	{
		MutableClassComparer comparer = new MutableClassComparer();
		SortedObservableCollection<ObservableMutableClass> collection = new(comparer);
		ObservableMutableClass a = new(1);
		ObservableMutableClass b = new(2);
		collection.Add(a);
		collection.Add(b);

		int oldCount = comparer.InvocationCount;
		a.RaisePropertyChanged(a, null);
		Assert.NotEqual(oldCount, comparer.InvocationCount);
	}

	[Fact]
	public void ItemChangesWithNullSender()
	{
		MutableClassComparer comparer = new MutableClassComparer();
		SortedObservableCollection<ObservableMutableClass> collection = new(comparer);
		ObservableMutableClass a = new(1);
		ObservableMutableClass b = new(2);
		collection.Add(a);
		collection.Add(b);

		int oldCount = comparer.InvocationCount;
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>("sender", () => a.RaisePropertyChanged(null, nameof(a.Value)));
		this.Logger.WriteLine(ex.ToString());
		Assert.Equal(oldCount, comparer.InvocationCount);
	}

	[Fact]
	public void ItemChangesWithNonMemberSender()
	{
		MutableClassComparer comparer = new MutableClassComparer();
		SortedObservableCollection<ObservableMutableClass> collection = new(comparer);
		ObservableMutableClass a = new(1);
		ObservableMutableClass b = new(2);
		collection.Add(a);
		collection.Add(b);

		int oldCount = comparer.InvocationCount;
		a.RaisePropertyChanged(new ObservableMutableClass(1), nameof(ObservableMutableClass.Value));
		Assert.Equal(oldCount, comparer.InvocationCount);
	}

	[Fact]
	public void Remove_ReleasesHandlerReference()
	{
		SortedObservableCollection<ObservableMutableClass> collection = new(new MutableClassComparer());
		ObservableMutableClass a = new(1);
		collection.Add(a);
		Assert.Equal(1, a.HandlersCount);
		collection.Remove(a);
		Assert.Equal(0, a.HandlersCount);
	}

	[Fact]
	public void Clear_ReleasesHandlerReference()
	{
		SortedObservableCollection<ObservableMutableClass> collection = new(new MutableClassComparer());
		ObservableMutableClass a = new(1);
		collection.Add(a);
		Assert.Equal(1, a.HandlersCount);
		collection.Clear();
		Assert.Equal(0, a.HandlersCount);
	}

	[Fact]
	public void GetEnumerator_GenericInterface()
	{
		this.collection.Add(3);
		this.collection.Add(5);
		IEnumerable<int> enumerable = this.collection;
		Assert.Equal(new[] { 5, 3 }, enumerable.ToArray());
	}

	[Fact]
	public void GetEnumerator_NonGenericInterface()
	{
		this.collection.Add(3);
		this.collection.Add(5);
		IEnumerable enumerable = this.collection;
		IEnumerator enumerator = enumerable.GetEnumerator();
		Assert.True(enumerator.MoveNext());
		Assert.Equal(5, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(3, enumerator.Current);
		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void Count()
	{
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
		Assert.Equal(0, this.collection.Count);
		this.collection.Add(3);
		Assert.Equal(1, this.collection.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
	}

	[Fact]
	public void DefaultComparer()
	{
		this.collection = new();
		this.collection.Add(3);
		this.collection.Add(5);
		this.collection.Add(1);
		Assert.Equal(new[] { 1, 3, 5 }, this.collection);
	}

	private class DescendingIntComparer : IComparer<int>
	{
		public int Compare(int x, int y) => -x.CompareTo(y);
	}

	private class ObservableMutableClass : INotifyPropertyChanged
	{
		private int value;
		private int otherProperty;

		internal ObservableMutableClass(int value)
		{
			this.value = value;
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public int Value
		{
			get => this.value;
			set
			{
				this.value = value;
				this.OnPropertyChanged();
			}
		}

		public int OtherProperty
		{
			get => this.otherProperty;
			set
			{
				this.otherProperty = value;
				this.OnPropertyChanged();
			}
		}

		internal int HandlersCount => this.PropertyChanged?.GetInvocationList().Length ?? 0;

		internal void RaisePropertyChanged(object? sender, string? propertyName) => this.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "") => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private class MutableClassComparer : IOptimizedComparer<ObservableMutableClass>
	{
		internal int InvocationCount { get; private set; }

		public int Compare(ObservableMutableClass? x, ObservableMutableClass? y)
		{
			Assumes.False(x is null || y is null);
			this.InvocationCount++;
			return x.Value.CompareTo(y.Value);
		}

		public bool IsPropertySignificant(string propertyName) => propertyName == nameof(ObservableMutableClass.Value);
	}
}
