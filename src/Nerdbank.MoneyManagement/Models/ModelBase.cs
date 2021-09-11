// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement
{
	using SQLite;

	public abstract class ModelBase
	{
		/// <summary>
		/// Gets or sets the primary key of this database entity.
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		/// <summary>
		/// Saves this entity to the given <see cref="MoneyFile"/>.
		/// </summary>
		/// <param name="moneyFile">The file to write to.</param>
		public void Save(MoneyFile moneyFile)
		{
			if (this.Id == 0)
			{
				moneyFile.Insert(this);
			}
			else
			{
				moneyFile.InsertOrReplace(this);
			}
		}
	}
}
