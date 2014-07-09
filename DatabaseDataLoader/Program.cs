using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Centare.Tools.DatabaseDataLoader
{
    using System.Reflection;

    /// <summary>
	/// The main entry class for the application.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Defines the entry point of the application.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		public static void Main(string[] args)
		{
            Console.WriteLine("Database Data Loader");
            Console.WriteLine();
		    SetupAssemblyReference();


			var arguments = new ArgumentProcessor(args);

			string connectionString;
			if (!arguments.TryGetArgument("connection", ArgumentProcessor.StringParser, out connectionString))
			{
				Console.WriteLine("Argument 'connection' is missing but is required.");
				Environment.Exit(-1);
			}

			string baseDir;
			if (!arguments.TryGetArgument("baseDir", ArgumentProcessor.StringParser, out baseDir))
			{
				Console.WriteLine("Argument 'baseDir' is missing but is required.");
				Environment.Exit(-1);
			}

			var baseDirInfo = new DirectoryInfo(baseDir);
			if (!baseDirInfo.Exists)
			{
				Console.WriteLine("Argument 'baseDir' ({0}) does not reference an actual directory.", baseDir);
				Environment.Exit(-1);
			}
			else
			{
			    Console.WriteLine("Data files directory: {0}", baseDirInfo.FullName);
			}

			SqlConnectionStringBuilder builder = null;
			try
			{
				builder = new SqlConnectionStringBuilder(connectionString);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Argument 'connection' ({0}) Raw: '{1}' is invalid. Details: {2}", connectionString, args.FirstOrDefault(a => a.StartsWith("-connection", StringComparison.InvariantCultureIgnoreCase)), ex.Message);
				Environment.Exit(-1);
			}

            Console.WriteLine("Connection: {0}", builder);

            var exitCode = LoadData(builder.ConnectionString, baseDirInfo);

		    if (System.Diagnostics.Debugger.IsAttached)
		    {
                Console.Read();
		    }
			 
			Console.WriteLine("Data Load Complete");
			Environment.Exit(exitCode);
		}

        /// <summary>
        /// Sets up the assembly reference to avoid IL merge.
        /// </summary>
	    private static void SetupAssemblyReference()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    var ns = typeof(Program).Namespace ?? string.Empty;
                    var resourceName = args.Name;

                    if (!resourceName.StartsWith(ns))
                    {
                        var asmName = new AssemblyName(args.Name).Name;
                        resourceName = string.Format("{0}.DependentAssemblies.{1}.dll", ns, asmName);
                    }

                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            return null;
                        }

                        var assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        return Assembly.Load(assemblyData);
                    }
                };
        }

	    /// <summary>
		/// Builds the SQL template.
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="metadata">The metadata.</param>
		/// <param name="fieldHeaders">The field headers.</param>
		/// <param name="safeLoad">if set to <c>true</c> this is a safe to load (update) file.</param>
		/// <returns>The constructed SQL command.</returns>
		private static SqlLoadCommand BuildSqlTemplate(string tableName, IReadOnlyDictionary<string, ColumnInfo> metadata, IEnumerable<string> fieldHeaders, bool safeLoad)
		{
			var command = new SqlLoadCommand { TableName = tableName };
			foreach (var fieldHeader in fieldHeaders)
			{
				var header = fieldHeader;
				var columnDefinition = metadata[header];
				command.LoadParameters.Add(new LoadParameter
					{
						ColumnName = columnDefinition.ColumnName,
						DataType = columnDefinition.DataType,
						SqlDataType = columnDefinition.SqlDataType,
						HeaderName = header,
						IsPrimaryKey = columnDefinition.IsPrimary,
						IsAutoIncrement = columnDefinition.IsIdentity,
						MaxLength = columnDefinition.MaxLength,
						Nullable = columnDefinition.Nullable
					});
			}
			
			// Get any identity columns for the where clause.
			var primaryKeyClause = string.Join(" AND ", command.LoadParameters.Where(c => c.IsPrimaryKey).Select(l => string.Format("[{0}] = @{0}", l.ColumnName)));
			command.HasIdentityColumns = command.LoadParameters.Any(l => l.IsAutoIncrement);
			
			var builder = new StringBuilder();
			builder.AppendLine(@"DECLARE @RecordExists AS bit = 0")
				   .AppendLine(@"DECLARE @InsertedRecords AS int = 0")
				   .AppendLine(@"DECLARE @UpdatedRecords AS int = 0")
				   .AppendFormat(@"SELECT @RecordExists = 1 FROM [{0}] WHERE {1}", tableName, primaryKeyClause)
				   .AppendLine()
				   .AppendLine("IF (@RecordExists = 0)")
				   .AppendLine("BEGIN");

			builder.AppendFormat("INSERT INTO [{0}]", tableName)
			       .AppendLine()
				   .AppendFormat("({0})", string.Join(",", command.LoadParameters.Select(l => string.Format("[{0}]", l.ColumnName))))
			       .AppendLine()
			       .AppendLine("VALUES")
			       .AppendFormat("({0})", string.Join(",", command.LoadParameters.Select(l => string.Format("@{0}", l.ColumnName))))
			       .AppendLine()
				   .AppendLine()
				   .AppendLine("SET @InsertedRecords = 1");

			builder.AppendLine("END");

			// Create update statement
			if (safeLoad)
			{
				builder.AppendLine("ELSE")
					   .AppendLine("BEGIN")
					   .AppendFormat("UPDATE [{0}]", tableName)
					   .AppendLine()
					   .AppendFormat("SET {0}", string.Join(", ", command.LoadParameters.Where(c => !c.IsPrimaryKey).Select(l => string.Format("[{0}] = @{0}", l.ColumnName))))
					   .AppendLine()
					   .AppendFormat("WHERE {0}", primaryKeyClause)
					   .AppendLine()
					   .AppendLine("SET @UpdatedRecords = 1")
					   .AppendLine("END");
			}
			
			builder.AppendLine("SELECT @InsertedRecords AS InsertedRecords, @UpdatedRecords AS UpdatedRecords");
			command.Command = builder.ToString();

			return command;
		}

		/// <summary>
		/// Loads the data.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="baseDirectory">The base directory.</param>
		/// <returns>The exit code for the process.</returns>
		private static int LoadData(string connectionString, DirectoryInfo baseDirectory)
		{
			TypeConverterFactory.AddConverter<TimeSpan>(new TimeSpanConverter());

			var exitCode = 0;
			SqlConnection connection = null;
			try
			{
				connection = new SqlConnection(connectionString);
				connection.Open();

				foreach (var subDirectory in baseDirectory.EnumerateDirectories("*.*", SearchOption.TopDirectoryOnly))
				{
					var safeLoadDirectory = string.Equals(subDirectory.Name, "Updateable", StringComparison.InvariantCultureIgnoreCase);
					foreach (var loadFile in subDirectory.EnumerateFiles("*.csv", SearchOption.TopDirectoryOnly))
					{
						try
						{
							var code = LoadFile(connection, loadFile, safeLoadDirectory);
							if (exitCode == 0 && code != 0)
							{
								exitCode = code;
							}
						}
						catch (DataLoadException ex)
						{
							switch (ex.Error)
							{
								case DataLoadException.ErrorType.TableNotFound:
									Console.WriteLine("Table '{0}' does not exist in the database.", ex.TableName);
									exitCode = -4;
									break;
								default:
									Console.WriteLine("Error loading table '{0}'. Details: {1}", ex.TableName, ex.Message);
									exitCode = -3;
									break;
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine(
								"An error occured while loading file '{0}\\{1}'. Details: {2}", 
								subDirectory.Name,
								loadFile.Name,
								ex.Message);

							exitCode = -3;
						}
					}
				}
			}
			finally
			{
				if (connection != null)
				{
					connection.Close();
				}
			}

			return exitCode;
		}

		/// <summary>
		/// Loads the file.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="loadFile">The load file.</param>
		/// <param name="safeLoadDirectory">if set to <c>true</c> the directory is a safe load directory.</param>
		/// <returns>The exit code for the file.</returns>
		private static int LoadFile(SqlConnection connection, FileInfo loadFile, bool safeLoadDirectory)
		{
			Console.WriteLine();
			Console.WriteLine("Loading File: {0}", loadFile.Name);

			var exitCode = 0;
			var tableName = Path.GetFileNameWithoutExtension(loadFile.Name);
			var metadata = GetMetadata(connection, tableName);
			var config = new CsvConfiguration
				{
					IsHeaderCaseSensitive = false,
					SkipEmptyRecords = true,
					HasHeaderRecord = true
				};

			var loadStatistics = new LoadStatistics();
			var initialized = false;
			SqlLoadCommand sqlCommand = null;
			using (var reader = loadFile.OpenText())
			{
				using (var file = new CsvReader(reader, config))
				{
					while (file.Read())
					{
						if (!initialized)
						{
							// Map first row as headers
							var items = file.FieldHeaders.Where(h => !metadata.ContainsKey(h)).ToList();
							if (items.Count > 0)
							{
								Console.WriteLine("File '{0}' contains columns ({1}) that are not in the table.");
								return -2;
							}

							sqlCommand = BuildSqlTemplate(tableName, metadata, file.FieldHeaders, safeLoadDirectory);
							ChangeForeignKeyContraints(connection, tableName, false);

							if (sqlCommand.HasIdentityColumns)
							{
								ChangeIdentityInsert(connection, tableName, false);
							}

							initialized = true;
						}

						loadStatistics.TotalRecords = loadStatistics.TotalRecords + 1;
						try
						{
							LoadRecord(connection, file, sqlCommand, loadStatistics);
						}
						catch (Exception ex)
						{
							Console.WriteLine("Cannot load record {0}. Details: {1}", loadStatistics.TotalRecords, ex);
							loadStatistics.RecordsErrored = loadStatistics.RecordsErrored + 1;
							exitCode = -3;
						}
					}

					if (initialized)
					{
						ChangeForeignKeyContraints(connection, tableName, true);

						if (sqlCommand.HasIdentityColumns)
						{
							ChangeIdentityInsert(connection, tableName, true);
						}
					}
				}
			}

			Console.WriteLine();
			Console.WriteLine(
				"Statistics: {0} Inserted, {1} Updated, {2} Errored, {3} Total",
				loadStatistics.RecordsCreated,
				loadStatistics.RecordsUpdated,
				loadStatistics.RecordsErrored,
				loadStatistics.TotalRecords);

			return exitCode;
		}

		/// <summary>
		/// Loads the record.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="csvReader">The CSV Reader.</param>
		/// <param name="sqlCommand">The SQL command.</param>
		/// <param name="loadStatistics">The load statistics.</param>
		private static void LoadRecord(SqlConnection connection, CsvReader csvReader, SqlLoadCommand sqlCommand, LoadStatistics loadStatistics)
		{
			// Get the table metadata
			var command = connection.CreateCommand();
			command.CommandType = CommandType.Text;
			command.CommandText = sqlCommand.Command;

			foreach (var loadParameter in sqlCommand.LoadParameters)
			{
				var parameter = command.CreateParameter();
				parameter.ParameterName = loadParameter.ColumnName;
				parameter.Direction = ParameterDirection.Input;
				parameter.SqlDbType = loadParameter.SqlDataType;
				parameter.IsNullable = loadParameter.Nullable;

				if (loadParameter.MaxLength > 0)
				{
					parameter.Size = loadParameter.MaxLength;
				}

				parameter.Value = loadParameter.ValueAccessor(csvReader);
				command.Parameters.Add(parameter);
			}

			try
			{
				using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
				{
					if (!reader.Read())
					{
						return;
					}

					loadStatistics.RecordsCreated += reader.GetInt32(0);
					loadStatistics.RecordsUpdated += reader.GetInt32(1);
				}
			}
			catch (SqlException ex)
			{
				var builder = new StringBuilder();
				builder.AppendFormat("Command: {0}", command.CommandText)
				       .AppendLine()
				       .AppendLine("Parameters:")
				       .AppendLine("Name\t\tValue");

				foreach (SqlParameter loadParameter in command.Parameters)
				{
					builder.AppendFormat("@{0}\t\t'{1}'", loadParameter.ParameterName, loadParameter.Value).AppendLine();
				}

				throw new DataLoadException(string.Format("Cannot process load record. Command: {0}", builder), ex, DataLoadException.ErrorType.RecordFailure, sqlCommand.TableName);
			}
			finally
			{
				command.Dispose();
			}
		}

		/// <summary>
		/// Gets the metadata.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <returns>A dictionary of columns.</returns>
		private static Dictionary<string, ColumnInfo> GetMetadata(SqlConnection connection, string tableName)
		{
			var dictionary = new Dictionary<string, ColumnInfo>(StringComparer.InvariantCultureIgnoreCase);
			try
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandType = CommandType.Text;
					command.CommandText = string.Format("SELECT TOP(1) * FROM [{0}]", tableName);

					using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
					{
						var schema = reader.GetSchemaTable();

						if (schema != null && schema.Rows.Count > 0)
						{
							foreach (DataRow row in schema.Rows)
							{
								var columnName = row.Field<string>("ColumnName");
								if (dictionary.ContainsKey(columnName))
								{
									continue;
								}

								var info = new ColumnInfo
								{
									ColumnName = columnName,
									DataType = row.Field<Type>("DataType"),
									SqlDataType = (SqlDbType)row.Field<int>("NonVersionedProviderType"),
									IsIdentity = row.Field<bool>("IsIdentity"),
									MaxLength = row.Field<int>("ColumnSize"),
									Nullable = row.Field<bool>("AllowDBNull")
								};

								dictionary.Add(columnName, info);
							}
						}
					}
				}

				using (var command = connection.CreateCommand())
				{
					command.CommandType = CommandType.Text;
					command.CommandText = SqlCommands.GetPrimaryKeySql;
					var parameter = command.CreateParameter();
					parameter.ParameterName = "TableName";
					parameter.DbType = DbType.String;
					parameter.Direction = ParameterDirection.Input;
					parameter.Value = tableName;
					command.Parameters.Add(parameter);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = reader.GetFieldValue<string>(0);
							ColumnInfo info;
							if (dictionary.TryGetValue(columnName, out info))
							{
								info.IsPrimary = true;
							}
						}
					}
				}
			}
			catch (SqlException ex)
			{
				var errorType = DataLoadException.ErrorType.Unknown;
				if (ex.Number == 208)
				{
					errorType = DataLoadException.ErrorType.TableNotFound;
				}

				throw new DataLoadException("Cannot get metadata for table.", ex, errorType, tableName);
			}

			return dictionary;
		}

		/// <summary>
		/// enables or disables foreign key constraints for the table.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="enable">if set to <c>true</c> enables constraints.</param>
		private static void ChangeForeignKeyContraints(SqlConnection connection, string tableName, bool enable)
		{
			try
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandType = CommandType.Text;
					command.CommandText = string.Format("ALTER TABLE [{0}] {1}CHECK CONSTRAINT ALL", tableName, enable ? string.Empty : "NO");
					command.ExecuteNonQuery();
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Constraints could not be {0} on table '{1}', data failures may occur.", enable ? "enabled" : "disabled", tableName);
			}
		}

		/// <summary>
		/// enables or disables identity insert constraints for the table.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="enable">if set to <c>true</c> enables constraints.</param>
		private static void ChangeIdentityInsert(SqlConnection connection, string tableName, bool enable)
		{
			try
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandType = CommandType.Text;
					command.CommandText = string.Format("SET IDENTITY_INSERT [{0}] {1}", tableName, enable ? "OFF" : "ON");
					command.ExecuteNonQuery();
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Identity Insert could not be {0} on table '{1}', data failures may occur.", enable ? "enabled" : "disabled", tableName);
			}
		}
	}
}