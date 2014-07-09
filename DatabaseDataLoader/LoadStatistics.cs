namespace Centare.Tools.DatabaseDataLoader
{
	/// <summary>
	/// A set of statistics for loading.
	/// </summary>
	public class LoadStatistics
	{
		/// <summary>
		/// Gets or sets the records created.
		/// </summary>
		/// <value>The records created.</value>
		public int RecordsCreated { get; set; }

		/// <summary>
		/// Gets or sets the records updated.
		/// </summary>
		/// <value>The records updated.</value>
		public int RecordsUpdated { get; set; }

		/// <summary>
		/// Gets or sets the records errored.
		/// </summary>
		/// <value>The records errored.</value>
		public int RecordsErrored { get; set; }

		/// <summary>
		/// Gets or sets the total records.
		/// </summary>
		/// <value>The total records.</value>
		public int TotalRecords { get; set; }
	}
}