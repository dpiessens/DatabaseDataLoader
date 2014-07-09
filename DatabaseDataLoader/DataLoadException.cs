using System;
using System.Data.SqlClient;
using System.Linq;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// An exception that occurs when a data load fails.
	/// </summary>
	public class DataLoadException : ApplicationException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataLoadException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		/// <param name="error">The error.</param>
		/// <param name="tableName">Name of the table.</param>
		public DataLoadException(string message, Exception innerException, ErrorType error, string tableName) 
			: base(message, innerException)
		{
			this.Error = error;
			this.TableName = tableName;
		}

		/// <summary>
		/// Enumerates the types of errors.
		/// </summary>
		public enum ErrorType
		{
			/// <summary>
			/// The error is unknown
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// The table is not found
			/// </summary>
			TableNotFound = 1,

			/// <summary>
			/// The record failure has occurred.
			/// </summary>
			RecordFailure = 2,

			/// <summary>
			/// The value conversion
			/// </summary>
			ValueConversion = 3
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		/// <value>The name of the table.</value>
		public string TableName { get; private set; }

		/// <summary>
		/// Gets the error.
		/// </summary>
		/// <value>The error.</value>
		public ErrorType Error { get; private set; }

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>A <see cref="System.String" /> that represents this instance.</returns>
		/// <PermissionSet>
		/// <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*" />
		/// </PermissionSet>
		public override string ToString()
		{
			var sqlError = string.Empty;
			if (InnerException != null && InnerException is SqlException)
			{
				sqlError = Environment.NewLine + "SQL Errors:";
				sqlError = ((SqlException) InnerException).Errors.Cast<object>().Aggregate(sqlError, (current, sqlEx) => current + (Environment.NewLine + sqlEx));
			}

			return string.Format(
				"{0}{1}TableName: {2}{1}{3}", 
				base.ToString(),
 				Environment.NewLine,
				TableName,
				sqlError);
		}
	}
}