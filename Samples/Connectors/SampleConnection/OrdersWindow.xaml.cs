﻿namespace SampleConnection
{
	using StockSharp.BusinessEntities;

	public partial class OrdersWindow
	{
		public OrdersWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector => MainWindow.Instance.MainPanel.Connector;

		private void OrderGrid_OnOrderCanceling(Order order)
		{
			Connector.CancelOrder(order);
		}
	}
}