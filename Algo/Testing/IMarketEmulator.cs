#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: IMarketEmulator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing paper trading.
	/// </summary>
	public interface IMarketEmulator : IMessageAdapter, ILogSource
	{
		/// <summary>
		/// Emulator settings.
		/// </summary>
		MarketEmulatorSettings Settings { get; }

		/// <summary>
		/// The number of processed messages.
		/// </summary>
		long ProcessedMessageCount { get; }
	}
}