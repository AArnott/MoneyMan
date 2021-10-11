// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
	public void IsReadOnly() => Assert.False(((ICollection<int>)this.collection).IsReadOnly);

	[Fact]
	public void IsSynchronized() => Assert.False(((ICollection)this.collection).IsSynchronized);

	[Fact]
	public void SyncRoot() => Assert.NotNull(((ICollection)this.collection).SyncRoot);

	[Fact]
	public void Add()
	{
		this.collection.Add(5);
		Assert.Equal(5, Assert.Single(this.collection));
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
	public void Remove()
	{
		Assert.False(this.collection.Remove(1));
		this.collection.Add(3);
		this.collection.Add(5);
		Assert.True(this.collection.Remove(3));
		Assert.True(this.collection.Remove(5));
		Assert.False(this.collection.Remove(5));
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
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => Assert.True(this.collection.Remove(3)), nameof(this.collection.Count));
		TestUtilities.AssertPropertyChangedEvent(this.collection, () => Assert.False(this.collection.Remove(3)), invertExpectation: true, nameof(this.collection.Count));
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
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Value)));
			}
		}

		internal int HandlersCount => this.PropertyChanged?.GetInvocationList().Length ?? 0;
	}

	private class MutableClassComparer : IComparer<ObservableMutableClass>
	{
		public int Compare(ObservableMutableClass? x, ObservableMutableClass? y)
		{
			Assumes.False(x is null || y is null);
			return x.Value.CompareTo(y.Value);
		}
	}
}
