using System;
using System.Collections.Generic;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>A class that helps process arguments from the command line.</summary>
	internal class ArgumentProcessor
	{
		#region Fields

		/// <summary>
		/// The arguments.
		/// </summary>
		private readonly List<string> _arguments;

		/// <summary>
		/// The processed args.
		/// </summary>
		private readonly Dictionary<string, List<string>> _processedArgs;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ArgumentProcessor"/> class.
		/// </summary>
		/// <param name="args">
		/// The arguments from the command line.
		/// </param>
		public ArgumentProcessor(params string[] args)
		{
			_arguments = new List<string>(args);
			_processedArgs = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

			ParseArguments(args);
		}

		#endregion

		#region Delegates

		/// <summary>
		/// A delegate method that indicates how an argument should be parsed.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the argument being processes.
		/// </typeparam>
		/// <param name="argumentValue">
		/// The argument value.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <returns>
		/// <c>true</c> if the argument parses correctly; otherwise <c>false</c>.
		/// </returns>
		internal delegate bool ArgumentParser<T>(string argumentValue, out T value);

		#endregion

		#region Properties

		/// <summary>
		///     Gets the string parser to use for argument parsing.
		/// </summary>
		/// <value>
		///     The string parser.
		/// </value>
		public static ArgumentParser<string> StringParser
		{
			get { return ParseString; }
		}

		/// <summary>
		///     Gets an integer parser that ensures the value is positive.
		/// </summary>
		/// <value>
		///     The string parser.
		/// </value>
		public static ArgumentParser<int> PositiveInt
		{
			get { return ParsePositiveInt; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the argument by key.
		/// </summary>
		/// <param name="key">
		/// The key.
		/// </param>
		/// <returns>
		/// The argument if exists; otherwise <c>null</c>.
		/// </returns>
		public string GetArgument(string key)
		{
			List<string> value;
			return _processedArgs.TryGetValue(key, out value) ? value[0] : null;
		}

		/// <summary>
		/// Gets the argument by key.
		/// </summary>
		/// <param name="key">
		/// The key.
		/// </param>
		/// <returns>
		/// The argument if exists; otherwise <c>null</c>.
		/// </returns>
		public List<string> GetArgumentList(string key)
		{
			List<string> value;
			return _processedArgs.TryGetValue(key, out value) ? new List<string>(value) : new List<string>(0);
		}

		/// <summary>
		/// Gets the argument by position.
		/// </summary>
		/// <param name="index">
		/// The index.
		/// </param>
		/// <returns>
		/// The argument if exists; otherwise <c>null</c>.
		/// </returns>
		public string GetArgument(int index)
		{
			return (index > -1 && index < _arguments.Count) ? _arguments[index] : null;
		}

		/// <summary>
		/// Indicates that a command line switch exists with the given key.
		/// </summary>
		/// <param name="key">
		/// The key.
		/// </param>
		/// <returns>
		/// The argument if exists; otherwise <c>null</c>.
		/// </returns>
		public bool HasSwitch(string key)
		{
			return _processedArgs.ContainsKey(key);
		}

		/// <summary>
		/// Tries to get and parse the argument by switch key.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the data once processed.
		/// </typeparam>
		/// <param name="key">
		/// The key.
		/// </param>
		/// <param name="parser">
		/// The parser.
		/// </param>
		/// <param name="parsedValue">
		/// The parsed value.
		/// </param>
		/// <param name="defaultValue">
		/// The default value.
		/// </param>
		/// <returns>
		/// <c>true</c> if the argument exists and is processed correctly.
		/// </returns>
		public bool TryGetArgument<T>(string key, ArgumentParser<T> parser, out T parsedValue, T defaultValue = default(T))
		{
			return TryGetArgument(() => GetArgument(key), parser, out parsedValue, defaultValue);
		}

		/// <summary>
		/// Tries to get and parse the argument by position.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the data once processed.
		/// </typeparam>
		/// <param name="index">
		/// The index position of the argument.
		/// </param>
		/// <param name="parser">
		/// The parser.
		/// </param>
		/// <param name="parsedValue">
		/// The parsed value.
		/// </param>
		/// <param name="defaultValue">
		/// The default value.
		/// </param>
		/// <returns>
		/// <c>true</c> if the argument exists and is processed correctly.
		/// </returns>
		public bool TryGetArgument<T>(int index, ArgumentParser<T> parser, out T parsedValue, T defaultValue = default(T))
		{
			return TryGetArgument(() => GetArgument(index), parser, out parsedValue, defaultValue);
		}

		/// <summary>
		/// Parses the string.
		/// </summary>
		/// <param name="argumentValue">
		/// The argument value.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <returns>
		/// true in all cases for a string.
		/// </returns>
		private static bool ParsePositiveInt(string argumentValue, out int value)
		{
			if (int.TryParse(argumentValue, out value) && value > 0)
			{
				return true;
			}

			value = 0;
			return false;
		}

		/// <summary>
		/// Parses the string.
		/// </summary>
		/// <param name="argumentValue">
		/// The argument value.
		/// </param>
		/// <param name="value">
		/// The value.
		/// </param>
		/// <returns>
		/// true in all cases for a string.
		/// </returns>
		private static bool ParseString(string argumentValue, out string value)
		{
			value = argumentValue;
			return true;
		}

		/// <summary>
		/// Tries the get argument.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the argument.
		/// </typeparam>
		/// <param name="argumentFunc">
		/// The function used to get the argument.
		/// </param>
		/// <param name="parser">
		/// The parser.
		/// </param>
		/// <param name="parsedValue">
		/// The parsed value.
		/// </param>
		/// <param name="defaultValue">
		/// The default value.
		/// </param>
		/// <returns>
		/// <c>true</c> if the argument is correct; otherwise <c>false</c>.
		/// </returns>
		private static bool TryGetArgument<T>(Func<string> argumentFunc, ArgumentParser<T> parser, out T parsedValue, 
		                                      T defaultValue)
		{
			var argumentValue = argumentFunc();
			if (!string.IsNullOrEmpty(argumentValue) && parser(argumentValue, out parsedValue))
			{
				return true;
			}

			parsedValue = defaultValue;
			return false;
		}

		/// <summary>
		/// Parses the arguments.
		/// </summary>
		/// <param name="args">
		/// The arguments to parse.
		/// </param>
		private void ParseArguments(IList<string> args)
		{
			for (var cnt = 0; cnt < args.Count; cnt++)
			{
				var arg = args[cnt];
				if (string.IsNullOrWhiteSpace(arg) || !arg.StartsWith("-"))
				{
					continue;
				}

				arg = arg.Substring(1);

				var key = arg;
				var valuePos = arg.IndexOf(':');
				string value = null;
				if (valuePos > -1)
				{
					key = arg.Substring(0, valuePos);
					if (valuePos < arg.Length - 1)
					{
						value = arg.Substring(valuePos + 1);
					}
				}

				_arguments[cnt] = value;
				if (!_processedArgs.ContainsKey(key))
				{
					_processedArgs.Add(key, new List<string>(1));
				}

				_processedArgs[key].Add(value);
			}
		}

		#endregion
	}
}