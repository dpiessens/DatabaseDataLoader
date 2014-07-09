
using System;
using System.Globalization;
using CsvHelper.TypeConversion;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// A data converter for time span values.
	/// </summary>
	public class TimeSpanConverter : DefaultTypeConverter
	{
        /// <summary>
        /// Converts the string to an object.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="text">The string to convert to an object.</param>
        /// <returns>The object created from the string.</returns>
        public override object ConvertFromString(TypeConverterOptions options, string text)
		{
            return string.IsNullOrEmpty(text) ? base.ConvertFromString(options, text) : DateTime.Parse(text).TimeOfDay;
		}

	    /// <summary>
		/// Determines whether this instance [can convert from] the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// <c>true</c> if this instance [can convert from] the specified type; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanConvertFrom(Type type)
		{
			return type == typeof(TimeSpan);
		}
	}
}