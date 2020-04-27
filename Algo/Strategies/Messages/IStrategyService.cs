#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IStrategyService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;

	using StockSharp.Algo.Strategies.Messages;

	/// <summary>
	/// The interface describing the strategy store service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/strategyservice.svc")]
	public interface IStrategyService
	{
		/// <summary>
		/// To add the strategy to the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="isEnglish">Create strategy in English store.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy identifier.</returns>
		[OperationContract]
		[Obsolete]
		long CreateStrategy(Guid sessionId, bool isEnglish, StrategyData strategy);

		/// <summary>
		/// To add the strategy to the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="isEnglish">Create strategy in English store.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy identifier.</returns>
		[OperationContract]
		long CreateStrategy2(Guid sessionId, bool isEnglish, StrategyInfoMessage strategy);

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		[Obsolete]
		byte UpdateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UpdateStrategy2(Guid sessionId, StrategyInfoMessage strategy);

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte DeleteStrategy(Guid sessionId, long strategyId);

		/// <summary>
		/// To get all strategies identifiers.
		/// </summary>
		/// <param name="lastCheckTime">Last time of calling the method.</param>
		/// <param name="isEnglish">Request strategies in English store.</param>
		/// <returns>Strategies identifiers and revisions.</returns>
		[OperationContract]
		IEnumerable<Tuple<long, int>> GetStrategies(DateTime lastCheckTime, bool isEnglish);

		/// <summary>
		/// To get the name and description of strategies.
		/// </summary>
		/// <param name="strategyIds">Strategies identifiers.</param>
		/// <returns>Information about strategies.</returns>
		[OperationContract]
		[Obsolete]
		IEnumerable<StrategyData> GetDescription(long[] strategyIds);

		/// <summary>
		/// To get the name and description of strategies.
		/// </summary>
		/// <param name="strategyIds">Strategies identifiers.</param>
		/// <returns>Information about strategies.</returns>
		[OperationContract]
		IEnumerable<StrategyInfoMessage> GetDescription2(long[] strategyIds);

		/// <summary>
		/// To get active subscriptions signed by <see cref="Subscribe"/>.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="lastCheckTime">Last time of calling the method.</param>
		/// <returns>Active subscriptions.</returns>
		[OperationContract]
		[Obsolete]
		IEnumerable<StrategySubscription> GetSubscriptions(Guid sessionId, DateTime lastCheckTime);

		/// <summary>
		/// To get active subscriptions signed by <see cref="Subscribe"/>.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="lastCheckTime">Last time of calling the method.</param>
		/// <returns>Active subscriptions.</returns>
		[OperationContract]
		IEnumerable<StrategySubscriptionInfoMessage> GetSubscriptions2(Guid sessionId, DateTime lastCheckTime);

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <param name="isAutoRenew">Is auto renewable subscription.</param>
		/// <returns>The strategy subscription.</returns>
		[OperationContract]
		[Obsolete]
		StrategySubscription Subscribe(Guid sessionId, long strategyId, bool isAutoRenew);

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <param name="isAutoRenew">Is auto renewable subscription.</param>
		/// <returns>The strategy subscription.</returns>
		[OperationContract]
		StrategySubscriptionInfoMessage Subscribe2(Guid sessionId, long strategyId, bool isAutoRenew);

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="subscriptionId">The subscription identifier.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UnSubscribe(Guid sessionId, long subscriptionId);

		/// <summary>
		/// To find backtesting session.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="from">Minimum creation date.</param>
		/// <param name="to">Maximum creation date.</param>
		/// <returns>Found sessions.</returns>
		[OperationContract]
		[Obsolete]
		StrategyBacktest[] GetBacktests(Guid sessionId, DateTime from, DateTime to);

		/// <summary>
		/// To get an approximate of money to spend for the specified backtesting configuration.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="backtest">Backtesting session.</param>
		/// <returns>An approximate of money.</returns>
		[OperationContract]
		[Obsolete]
		decimal GetApproximateAmount(Guid sessionId, StrategyBacktest backtest);

		/// <summary>
		/// To start backtesing.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="backtest">Backtesting session.</param>
		/// <returns>The backtesting session identifier.</returns>
		[OperationContract]
		[Obsolete]
		long StartBacktest(Guid sessionId, StrategyBacktest backtest);

		/// <summary>
		/// To stop the backtesing.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="backtestId">The backtesting session identifier.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		[Obsolete]
		byte StopBacktest(Guid sessionId, long backtestId);

		/// <summary>
		/// To get the count of completed iterations.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="backtestId">The backtesting session identifier.</param>
		/// <returns>The count of completed iterations.</returns>
		[OperationContract]
		[Obsolete]
		int GetCompletedIterationCount(Guid sessionId, long backtestId);

		/// <summary>
		/// To get the identifier of formatted file.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="backtestId">The backtesting session identifier.</param>
		/// <returns>Identifier of formatted file.</returns>
		[OperationContract]
		[Obsolete]
		long? GetBacktestResult(Guid sessionId, long backtestId);
	}
}