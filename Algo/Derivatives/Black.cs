#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: Black.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The Greeks values calculating model by the Black formula.
	/// </summary>
	public class Black : BlackScholes
	{
		// http://riskencyclopedia.com/articles/black_1976/

		/// <summary>
		/// Initializes a new instance of the <see cref="Black"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public Black(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
			: base(option, securityProvider, dataProvider, exchangeInfoProvider)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Black"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public Black(Security option, Security underlyingAsset, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
			: base(option, underlyingAsset, dataProvider, exchangeInfoProvider)
		{
		}

		/// <inheritdoc />
		public override decimal Dividend
		{
			set
			{
				if (value != 0)
					throw new ArgumentOutOfRangeException(LocalizedStrings.Str701Params.Put(UnderlyingAsset));

				base.Dividend = value;
			}
		}

		private decimal? GetExpRate(DateTimeOffset currentTime)
		{
			var timeLine = GetExpirationTimeLine(currentTime);

			if (timeLine == null)
				return null;

			return (decimal)DerivativesHelper.ExpRate(RiskFree, timeLine.Value);
		}

		/// <inheritdoc />
		public override decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Premium(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		public override decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Delta(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		public override decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Gamma(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		public override decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Vega(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		public override decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Theta(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		public override decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Rho(currentTime, deviation, assetPrice);
		}

		/// <inheritdoc />
		protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
		{
			return DerivativesHelper.D1(assetPrice, GetStrike(), 0, 0, deviation, timeToExp);
		}
	}
}