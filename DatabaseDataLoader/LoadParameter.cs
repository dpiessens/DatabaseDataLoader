using System;
using System.Data;
using CsvHelper;
using CsvHelper.TypeConversion;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// An individual parameter for loading data.
	/// </summary>
	public class LoadParameter
	{
		private readonly Lazy<Func<CsvReader, object>> valueAccessor;
	    
		/// <summary>
		/// Initializes a new instance of the <see cref="LoadParameter"/> class.
		/// </summary>
		public LoadParameter()
		{
			this.valueAccessor = new Lazy<Func<CsvReader, object>>(this.CreateValueAccessor);
		}

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
		/// Gets or sets the name of the header.
		/// </summary>
		/// <value>The name of the header.</value>
		public string HeaderName { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a primary key.
		/// </summary>
		/// <value><c>true</c> if this instance is primary key; otherwise, <c>false</c>.</value>
		public bool IsPrimaryKey { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ColumnInfo"/> allows null values.
		/// </summary>
		/// <value><c>true</c> if it allows null values; otherwise, <c>false</c>.</value>
		public bool Nullable { get; set; }

		/// <summary>
		/// Gets or sets the length of the max.
		/// </summary>
		/// <value>The length of the max.</value>
		public int MaxLength { get; set; }

		/// <summary>
		/// Gets the value accessor.
		/// </summary>
		/// <value>The value accessor.</value>
		public Func<CsvReader, object> ValueAccessor
		{
			get { return this.valueAccessor.Value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is auto increment.
		/// </summary>
		/// <value><c>true</c> if this instance is auto increment; otherwise, <c>false</c>.</value>
		public bool IsAutoIncrement { get; set; }

		/// <summary>
		/// Gets or sets the type of the SQL data.
		/// </summary>
		/// <value>The type of the SQL data.</value>
		public SqlDbType SqlDataType { get; set; }

		/// <summary>
		/// Creates the value accessor.
		/// </summary>
		/// <returns>A function to get the value.</returns>
		private Func<CsvReader, object> CreateValueAccessor()
		{
			var converter = TypeConverterFactory.GetConverter(this.DataType);
			return r => GetFieldValue(r, converter);
		}

		/// <summary>
		/// Gets the field value.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="converter">The converter.</param>
		/// <returns>An object result field value.</returns>
		private object GetFieldValue(CsvReader reader, ITypeConverter converter)
		{
			try
			{
				if (this.DataType == typeof(string))
				{
					var stringValue = reader.GetField(this.HeaderName);
					if (this.MaxLength > 0 && !string.IsNullOrEmpty(stringValue) && stringValue.Length > this.MaxLength)
					{
						return stringValue.Substring(0, this.MaxLength);
					}

					return stringValue;
				}

				if (this.Nullable)
				{
					var stringValue = reader.GetField(this.HeaderName);
					if (string.IsNullOrWhiteSpace(stringValue) ||
						string.Equals("NULL", stringValue.Trim(), StringComparison.InvariantCultureIgnoreCase))
					{
						return DBNull.Value;
					}

                    return converter.ConvertFromString(TypeConverterOptionsFactory.GetOptions(this.DataType), stringValue);
				}
				
				return reader.GetField(this.DataType, this.HeaderName, converter);
			}
			catch (Exception ex)
			{
				throw new DataLoadException(
					string.Format("Cannot convert column '{0}' value '{1}' to data type {2}", this.ColumnName, reader.GetField(this.HeaderName), this.DataType),
					ex,
					DataLoadException.ErrorType.ValueConversion,
					string.Empty);
			}
		}
	}
}