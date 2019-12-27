#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: INewsProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using StockSharp.Messages;

	/// <summary>
	/// The interface for access to provider of information about news.
	/// </summary>
	public interface INewsProvider
	{
		/// <summary>
		/// Request news <see cref="News.Story"/> body. After receiving the event <see cref="IMarketDataProvider.NewsChanged"/> will be triggered.
		/// </summary>
		/// <param name="news">News.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void RequestNewsStory(News news, IMessageAdapter adapter = null);
	}
}