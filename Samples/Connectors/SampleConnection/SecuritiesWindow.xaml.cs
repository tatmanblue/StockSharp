namespace SampleConnection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Subscription, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Subscription, QuotesWindow>();
		private readonly SynchronizedList<ChartWindow> _chartWindows = new SynchronizedList<ChartWindow>();
		private bool _initialized;
		private bool _appClosing;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		private static Connector Connector => MainWindow.Instance.Connector;

		private void SecuritiesWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateTimeFrames(Connector.Adapter.GetTimeFrames());
		}

		public void UpdateTimeFrames(IEnumerable<TimeSpan> timeFrames)
		{
			if (timeFrames == null)
				throw new ArgumentNullException(nameof(timeFrames));

			timeFrames = timeFrames.ToArray();

			if (!timeFrames.Any())
				return;

			CandlesPeriods.ItemsSource = timeFrames;
			CandlesPeriods.SelectedIndex = 0;
		}

		protected override void OnClosed(EventArgs e)
		{
			_appClosing = true;
			_quotesWindows.SyncDo(d => d.Values.ForEach(w => w.Close()));

			_chartWindows.SyncDo(c => c.ToArray().ForEach(w =>
			{
				w.SeriesInactive = true;
				w.Close();
			}));

			var connector = Connector;

			if (connector != null)
			{
				if (_initialized)
					connector.MarketDepthReceived -= TraderOnMarketDepthChanged;
			}

			base.OnClosed(e);
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var connector = Connector;

			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
			}.Init(connector);

			if (newOrder.ShowModal(this))
				connector.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = Ticks.IsEnabled = HistTicks.IsEnabled = OrderLog.IsEnabled
				= NewOrder.IsEnabled = Depth.IsEnabled = DepthAdvanced.IsEnabled = security != null;

			TryEnableCandles();
		}

		private void DepthAdvancedClick(object sender, RoutedEventArgs e)
		{
			var settingsWnd = new DepthSettingsWindow();

			if (!settingsWnd.ShowModal(this))
				return;

			SubscribeDepths(settingsWnd.Settings);
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			SubscribeDepths(null);
		}

		private void SubscribeDepths(DepthSettings settings)
		{
			var connector = Connector;

			if (!_initialized)
			{
				connector.MarketDepthReceived += TraderOnMarketDepthChanged;
				_initialized = true;
			}

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				// subscribe on order book flow
				var subscription = connector.SubscribeMarketDepth(security, settings?.From, settings?.To, buildMode: settings?.BuildMode ?? MarketDataBuildModes.LoadAndBuild, maxDepth: settings?.MaxDepth, buildFrom: settings?.BuildFrom);

				// create order book window
				var window = new QuotesWindow
				{
					Title = security.Id + " " + LocalizedStrings.MarketDepth
				};
				window.Closed += (s, e) =>
				{
					if (_appClosing)
						return;

					if (subscription.State.IsActive())
						connector.UnSubscribe(subscription);
				};

				window.DepthCtrl.UpdateDepth(connector.GetMarketDepth(security));
				window.Show();

				_quotesWindows.Add(subscription, window);
			}
		}

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			var connector = Connector;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (connector.RegisteredSecurities.Contains(security))
					connector.UnSubscribeLevel1(security);
				else
					connector.SubscribeLevel1(security);
			}
		}

		private void TicksClick(object sender, RoutedEventArgs e)
		{
			var connector = Connector;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (connector.RegisteredTrades.Contains(security))
					connector.UnSubscribeTrades(security);
				else
					connector.SubscribeTrades(security);
			}
		}

		private void HistTicksClick(object sender, RoutedEventArgs e)
		{
			var connector = Connector;

			var wnd = new DatesWindow();

			if (!wnd.ShowModal(this))
				return;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				connector.SubscribeTrades(security, wnd.From, wnd.To);
			}
		}

		private void OrderLogClick(object sender, RoutedEventArgs e)
		{
			var connector = Connector;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (connector.RegisteredOrderLogs.Contains(security))
					connector.UnSubscribeOrderLog(security);
				else
					connector.SubscribeOrderLog(security);
			}
		}

		private void TraderOnMarketDepthChanged(Subscription subscription, MarketDepth depth)
		{
			if (_quotesWindows.TryGetValue(subscription, out var wnd))
				wnd.DepthCtrl.UpdateDepth(depth);
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow
			{
				ShowAllOption = Connector.Adapter.IsSupportSecuritiesLookupAll(),
				Criteria = new Security { Code = "IS" }
			};

			if (!wnd.ShowModal(this))
				return;

			Connector.LookupSecurities(wnd.Criteria);
		}

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			var tf = (TimeSpan)CandlesPeriods.SelectedItem;

			var range = TimeSpan.FromTicks(tf.Ticks * 10000);

			if (range.TotalYears() > 5)
				range = TimeSpan.FromTicks(TimeHelper.TicksPerYear * 5);

			var wnd = new DatesWindow { From = DateTime.Today - range };

			if (!wnd.ShowModal(this))
				return;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var chartWnd = new ChartWindow(new CandleSeries(typeof(TimeFrameCandle), security, tf)
				{
					From = wnd.From,
					To = wnd.To
				});

				_chartWindows.Add(chartWnd);
				chartWnd.Closed += (s, e1) => _chartWindows.Remove(chartWnd);

				chartWnd.Show();
			}
		}

		private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableCandles();
		}

		private void TryEnableCandles()
		{
			Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
		}
	}
}