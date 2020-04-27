﻿#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: TradeTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	class TradeTable : Table<ExecutionMessage>
	{
		public TradeTable(decimal? priceStep, decimal? volumeStep)
			: base("Trade", CreateColumns(priceStep, volumeStep))
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns(decimal? priceStep, decimal? volumeStep)
		{
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeId))
			{
				//IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32),
			};
			yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			{
				//IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(SecurityId.BoardCode))
			{
				//IsPrimaryKey = true,
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(ExecutionMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(ExecutionMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradePrice)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = priceStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.TradeVolume)) { DbType = typeof(decimal), ValueRestriction = new DecimalRestriction { Scale = volumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.OriginSide)) { DbType = typeof(int?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.OpenInterest)) { DbType = typeof(decimal?), ValueRestriction = new DecimalRestriction { Scale = volumeStep?.GetCachedDecimals() ?? 1 } };
			yield return new ColumnDescription(nameof(ExecutionMessage.IsUpTick)) { DbType = typeof(bool?) };
			yield return new ColumnDescription(nameof(ExecutionMessage.Currency)) { DbType = typeof(int?) };
		}

		protected override IDictionary<string, object> ConvertToParameters(ExecutionMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(ExecutionMessage.TradeId), value.TradeId == null ? value.TradeStringId : value.TradeId.To<string>() },
				{ nameof(SecurityId.SecurityCode), value.SecurityId.SecurityCode },
				{ nameof(SecurityId.BoardCode), value.SecurityId.BoardCode },
				{ nameof(ExecutionMessage.ServerTime), value.ServerTime },
				{ nameof(ExecutionMessage.LocalTime), value.LocalTime },
				{ nameof(ExecutionMessage.TradePrice), value.TradePrice },
				{ nameof(ExecutionMessage.TradeVolume), value.TradeVolume },
				{ nameof(ExecutionMessage.OriginSide), (int?)value.OriginSide },
				{ nameof(ExecutionMessage.OpenInterest), value.OpenInterest },
				{ nameof(ExecutionMessage.IsUpTick), value.IsUpTick },
				{ nameof(ExecutionMessage.Currency), (int?)value.Currency },
			};
			return result;
		}
	}
}