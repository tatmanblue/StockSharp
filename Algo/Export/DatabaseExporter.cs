#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: DatabaseExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Data;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.Algo.Export.Database;
	using StockSharp.Algo.Export.Database.DbProviders;

	/// <summary>
	/// The export into database.
	/// </summary>
	public class DatabaseExporter : BaseExporter
	{
		private readonly DatabaseConnectionPair _connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseExporter"/>.
		/// </summary>
		/// <param name="priceStep">Minimum price step.</param>
		/// <param name="volumeStep">Minimum volume step.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="connection">The connection to DB.</param>
		public DatabaseExporter(decimal? priceStep, decimal? volumeStep, DataType dataType, Func<int, bool> isCancelled, DatabaseConnectionPair connection)
			: base(dataType, isCancelled, connection.ToString())
		{
			PriceStep = priceStep;
			VolumeStep = volumeStep;
			_connection = connection;
			CheckUnique = true;
		}

		/// <summary>
		/// Minimum price step.
		/// </summary>
		public decimal? PriceStep { get; }

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		public decimal? VolumeStep { get; }

		private int _batchSize = 50;

		/// <summary>
		/// The size of transmitted data package. The default is 50 elements.
		/// </summary>
		public int BatchSize
		{
			get => _batchSize;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_batchSize = value;
			}
		}

		/// <summary>
		/// To check uniqueness of data in the database. It effects performance. The default is enabled.
		/// </summary>
		public bool CheckUnique { get; set; }

		/// <inheritdoc />
		protected override void ExportOrderLog(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages, () => new OrderLogTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void ExportTicks(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages, () => new TradeTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void ExportTransactions(IEnumerable<ExecutionMessage> messages)
		{
			Do(messages, () => new TransactionTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages.ToTimeQuotes(), () => new MarketDepthQuoteTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages, () => new Level1Table(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			// TODO
			Do(messages, () => new CandleTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages, () => new NewsTable());
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages, () => new SecurityTable());
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<PositionChangeMessage> messages)
		{
			Do(messages, () => new PositionChangeTable(PriceStep, VolumeStep));
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<IndicatorValue> values)
		{
			Do(values, () => new IndicatorValueTable());
		}

		private void Do<TValue, TTable>(IEnumerable<TValue> values, Func<TTable> getTable)
			where TTable : Table<TValue>
		{
			if (getTable == null)
				throw new ArgumentNullException(nameof(getTable));

			using (var provider = BaseDbProvider.Create(_connection))
			{
				provider.CheckUnique = CheckUnique;

				var table = getTable();

				provider.CreateIfNotExists(table);

				foreach (var batch in values.Batch(BatchSize).Select(b => b.ToArray()))
				{
					if (!CanProcess(batch.Length))
						break;

					provider.InsertBatch(table, table.ConvertToParameters(batch));
				}
			}
		}
	}
}