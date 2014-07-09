using System.Collections.Generic;

namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// A class definition for the SQL loading components.
	/// </summary>
	public class SqlLoadCommand
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SqlLoadCommand"/> class.
		/// </summary>
		public SqlLoadCommand()
		{
			this.LoadParameters = new List<LoadParameter>();
		}

		/// <summary>
		/// Gets or sets the command.
		/// </summary>
		/// <value>The command.</value>
		public string Command { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance has identity columns.
		/// </summary>
		/// <value><c>true</c> if this instance has identity columns; otherwise, <c>false</c>.</value>
		public bool HasIdentityColumns { get; set; }

		/// <summary>
		/// Gets or sets the load parameters.
		/// </summary>
		/// <value>The load parameters.</value>
		public List<LoadParameter> LoadParameters { get; set; }

		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		/// <value>The name of the table.</value>
		public string TableName { get; set; }
	}
}