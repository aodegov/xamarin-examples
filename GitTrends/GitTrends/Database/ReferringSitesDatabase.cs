﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitTrends.Shared;
using SQLite;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace GitTrends
{
	public class ReferringSitesDatabase : BaseDatabase
	{
		public ReferringSitesDatabase(IFileSystem fileSystem, IAnalyticsService analyticsService) : base(fileSystem, analyticsService, CachedDataConstants.DatabaseReferringSitesLifeSpan)
		{

		}

		public override async Task<int> DeleteAllData()
		{
			var databaseConnection = await GetDatabaseConnection<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);
			return await databaseConnection.DeleteAllAsync<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);
		}

		public async Task DeleteExpiredData()
		{
			var databaseConnection = await GetDatabaseConnection<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);

			var referringSites = await databaseConnection.Table<MobileReferringSitesDatabaseModel>().ToListAsync();
			var expiredReferringSites = referringSites.Where(x => IsExpired(x.DownloadedAt)).ToList();

			foreach (var expiredReferringSite in expiredReferringSites)
				await databaseConnection.DeleteAsync(expiredReferringSite).ConfigureAwait(false);
		}

		public async Task<MobileReferringSiteModel?> GetReferringSite(string repositoryUrl, Uri? referrerUri)
		{
			var databaseConnection = await GetDatabaseConnection<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);
			var referringSitesDatabaseModelList = await databaseConnection.Table<MobileReferringSitesDatabaseModel>().Where(x => x.RepositoryUrl == repositoryUrl && x.ReferrerUri == referrerUri).ToListAsync().ConfigureAwait(false);

			var newestReferringSiteModel = referringSitesDatabaseModelList.OrderByDescending(x => x.DownloadedAt).FirstOrDefault();

			try
			{
				return newestReferringSiteModel is null ? null : MobileReferringSitesDatabaseModel.ToReferringSitesModel(newestReferringSiteModel);
			}
			catch (UriFormatException)
			{
				await databaseConnection.DeleteAsync(newestReferringSiteModel).ConfigureAwait(false);
				return null;
			}
		}

		public async Task<List<MobileReferringSiteModel>> GetReferringSites(string repositoryUrl)
		{
			var databaseConnection = await GetDatabaseConnection<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);

			var referringSitesDatabaseModelList = await databaseConnection.Table<MobileReferringSitesDatabaseModel>().Where(x => x.RepositoryUrl == repositoryUrl).ToListAsync().ConfigureAwait(false);

			return referringSitesDatabaseModelList.Select(x => MobileReferringSitesDatabaseModel.ToReferringSitesModel(x)).ToList();
		}

		public async Task<int> SaveReferringSite(MobileReferringSiteModel referringSiteModel, string repositoryUrl)
		{
			var databaseConnection = await GetDatabaseConnection<MobileReferringSitesDatabaseModel>().ConfigureAwait(false);
			var referringSitesDatabaseModel = MobileReferringSitesDatabaseModel.ToReferringSitesDatabaseModel(referringSiteModel, repositoryUrl);

			return await databaseConnection.InsertOrReplaceAsync(referringSitesDatabaseModel).ConfigureAwait(false);
		}

		record MobileReferringSitesDatabaseModel : IMobileReferringSiteModel
		{
			//PrimaryKey must be nullable https://github.com/praeclarum/sqlite-net/issues/327
			[PrimaryKey]
			public int? Id { get; init; }

			[Indexed]
			public string RepositoryUrl { get; init; } = string.Empty;

			public string FavIconImageUrl { get; init; } = string.Empty;

			public DateTimeOffset DownloadedAt { get; init; }

			public string Referrer { get; init; } = string.Empty;

			public bool IsReferrerUriValid { get; init; }

			public Uri? ReferrerUri { get; init; }

			public long TotalCount { get; init; }

			public long TotalUniqueCount { get; init; }

			public static MobileReferringSiteModel ToReferringSitesModel(MobileReferringSitesDatabaseModel referringSitesDatabaseModel)
			{
				var referringSiteModel = new ReferringSiteModel(referringSitesDatabaseModel.TotalCount, referringSitesDatabaseModel.TotalUniqueCount, referringSitesDatabaseModel.Referrer, referringSitesDatabaseModel.DownloadedAt);

				try
				{
					return new MobileReferringSiteModel(referringSiteModel, string.IsNullOrWhiteSpace(referringSitesDatabaseModel.FavIconImageUrl) ? null : ImageSource.FromUri(new Uri(referringSitesDatabaseModel.FavIconImageUrl)));
				}
				catch (UriFormatException)
				{
					return new MobileReferringSiteModel(referringSiteModel, ImageSource.FromFile(referringSitesDatabaseModel.FavIconImageUrl));
				}
			}

			public static MobileReferringSitesDatabaseModel ToReferringSitesDatabaseModel(MobileReferringSiteModel referringSiteModel, string repositoryUrl)
			{
				return new MobileReferringSitesDatabaseModel
				{
					FavIconImageUrl = referringSiteModel.FavIconImageUrl,
					DownloadedAt = referringSiteModel.DownloadedAt,
					RepositoryUrl = repositoryUrl,
					Referrer = referringSiteModel.Referrer,
					TotalUniqueCount = referringSiteModel.TotalUniqueCount,
					IsReferrerUriValid = referringSiteModel.IsReferrerUriValid,
					ReferrerUri = referringSiteModel.ReferrerUri,
					TotalCount = referringSiteModel.TotalCount
				};
			}
		}
	}
}