using System;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// Represents the column information for the table.
	/// </summary>
	public class ColumnInfo
	{
		/// <summary>
		/// Gets or sets the name of the column.
		/// </summary>
		/// <value>The name of the column.</value>
		public string ColumnName { get; set; }

		/// <summary>
		/// Gets or sets the type of the data.
		/// </summary>
		/// <value>The type of the data.</value>
		public Type DataType { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a primary key.
		/// </summary>
		/// <value><c>true</c> if this instance is primary; otherwise, <c>false</c>.</value>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is auto increment.
		/// </summary>
		/// <value><c>true</c> if this instance is auto increment; otherwise, <c>false</c>.</value>
		public bool IsIdentity { get; set; }

		/// <summary>
		/// Gets or sets the length of the max.
		/// </summary>
		/// <value>The length of the max.</value>
		public int MaxLength { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ColumnInfo"/> allows null values.
		/// </summary>
		/// <value><c>true</c> if it allows null values; otherwise, <c>false</c>.</value>
		public bool Nullable { get; set; }

		/// <summary>
		/// Gets or sets the type of the SQL data.
		/// </summary>
		/// <value>The type of the SQL data.</value>
		public System.Data.SqlDbType SqlDataType { get; set; }
	}
}