// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MoneyMan.ViewModels;

internal class ObservableBox<T> : ObservableBase<T>
{
	private readonly List<IObserver<T>> observers = new();
	private T value;

	internal ObservableBox(T initialValue) => this.value = initialValue;

	public T Value
	{
		get => this.value;
		set
		{
			if (!EqualityComparer<T>.Default.Equals(this.value, value))
			{
				this.value = value;
				this.RepeatValue();
			}
		}
	}

	public void RepeatValue()
	{
		T value = this.Value;
		foreach (IObserver<T> observer in this.observers)
		{
			observer.OnNext(value);
		}
	}

	protected override IDisposable SubscribeCore(IObserver<T> observer)
	{
		observer.OnNext(this.Value);
		this.observers.Add(observer);
		return new Subscription(this, observer);
	}

	private class Subscription : IDisposable
	{
		private readonly ObservableBox<T> owner;
		private readonly IObserver<T> observer;

		internal Subscription(ObservableBox<T> owner, IObserver<T> observer)
		{
			this.owner = owner;
			this.observer = observer;
		}

		public void Dispose() => this.owner.observers.Remove(this.observer);
	}
}
