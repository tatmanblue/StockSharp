#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioLookupResultMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio lookup result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioLookupResultMessage : BaseResultMessage<PortfolioLookupResultMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		public PortfolioLookupResultMessage()
			: base(MessageTypes.PortfolioLookupResult)
		{
		}
	}
}