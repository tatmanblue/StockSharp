﻿namespace StockSharp.Configuration
{
	using System;
	using System.IO;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Localization;

	/// <summary>
	/// System paths.
	/// </summary>
	public static class Paths
	{
		static Paths()
		{
			var appSettings = ConfigManager.AppSettings;

			var companyPath = appSettings.Get("companyPath");
			CompanyPath = companyPath.IsEmpty() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp") : companyPath.ToFullPathIfNeed();

			var settingsPath = appSettings.Get("settingsPath");
			AppDataPath = settingsPath.IsEmpty() ? Path.Combine(CompanyPath, Title) : settingsPath.ToFullPathIfNeed();

			PlatformConfigurationFile = Path.Combine(AppDataPath, "platform_config.xml");
			ProxyConfigurationFile = Path.Combine(AppDataPath, "proxy_config.xml");
			SecurityNativeIdDir = Path.Combine(AppDataPath, "NativeId");
			SecurityMappingDir = Path.Combine(AppDataPath, "Symbol mapping");
			SecurityExtendedInfo = Path.Combine(AppDataPath, "Extended info");
			StorageDir = Path.Combine(AppDataPath, "Storage");
			SnapshotsDir = Path.Combine(AppDataPath, "Snapshots");
			InstallerDir = Path.Combine(CompanyPath, "Installer");
		}

		/// <summary>
		/// App title.
		/// </summary>
		public static string Title => TypeHelper.ApplicationName.Remove("S#.", true);

		/// <summary>
		/// The path to directory with all applications.
		/// </summary>
		public static readonly string CompanyPath;

		/// <summary>
		/// The path to the settings directory.
		/// </summary>
		public static readonly string AppDataPath;

		/// <summary>
		/// The path to the configuration file of platform definition.
		/// </summary>
		public static readonly string PlatformConfigurationFile;

		/// <summary>
		/// The path to the configuration file of proxy settings.
		/// </summary>
		public static readonly string ProxyConfigurationFile;

		/// <summary>
		/// The path to the directory with native security identifiers.
		/// </summary>
		public static readonly string SecurityNativeIdDir;

		/// <summary>
		/// The path to the directory with securities id mapping.
		/// </summary>
		public static readonly string SecurityMappingDir;

		/// <summary>
		/// The path to the directory with securities extended info.
		/// </summary>
		public static readonly string SecurityExtendedInfo;

		/// <summary>
		/// The path to the directory with market data.
		/// </summary>
		public static readonly string StorageDir;

		/// <summary>
		/// The path to the directory with snapshots of market data.
		/// </summary>
		public static readonly string SnapshotsDir;

		/// <summary>
		/// The path to the installer directory.
		/// </summary>
		public static readonly string InstallerDir;

		/// <summary>
		/// Get website url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetWebSiteUrl()
		{
			return $"https://stocksharp.{LocalizedStrings.Domain}";
		}

		/// <summary>
		/// Get user url.
		/// </summary>
		/// <param name="userId">User id.</param>
		/// <returns>Localized url.</returns>
		public static string GetUserUrl(long userId)
		{
			return $"{GetWebSiteUrl()}/users/{userId}/";
		}

		/// <summary>
		/// Get strategy url.
		/// </summary>
		/// <param name="robotId">The strategy identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetRobotLink(long robotId)
		{
			return $"{GetWebSiteUrl()}/robot/{robotId}/";
		}

		/// <summary>
		/// Get file url.
		/// </summary>
		/// <param name="fileId">File ID.</param>
		/// <returns>Localized url.</returns>
		public static string GetFileLink(object fileId)
		{
			return $"{GetWebSiteUrl()}/file/{fileId}/";
		}

		/// <summary>
		/// To create localized url.
		/// </summary>
		/// <param name="docUrl">Help topic.</param>
		/// <returns>Localized url.</returns>
		public static string GetDocUrl(string docUrl)
		{
			return $"https://doc.stocksharp.{LocalizedStrings.Domain}/html/{docUrl}";
		}

		/// <summary>
		/// Get open account url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetOpenAccountUrl()
		{
			return $"{GetWebSiteUrl()}/broker/openaccount/";
		}

		/// <summary>
		/// Get sign up url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetSignUpUrl()
		{
			return $"{GetWebSiteUrl()}/register/";
		}

		/// <summary>
		/// Get forgot password url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetForgotUrl()
		{
			return $"{GetWebSiteUrl()}/forgot/";
		}
	}
}