﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using Xamarin.Essentials.Interfaces;

namespace GitTrends
{
	public abstract class GitHubAuthenticationViewModel : BaseViewModel
	{
		bool _isAuthenticating = false;

		protected GitHubAuthenticationViewModel(IMainThread mainThread,
													IAnalyticsService analyticsService,
													GitHubUserService gitHubUserService,
													DeepLinkingService deepLinkingService,
													GitHubAuthenticationService gitHubAuthenticationService) : base(analyticsService, mainThread)
		{
			GitHubAuthenticationService.AuthorizeSessionStarted += HandleAuthorizeSessionStarted;
			GitHubAuthenticationService.AuthorizeSessionCompleted += HandleAuthorizeSessionCompleted;

			DemoButtonCommand = new AsyncCommand<string?, object?>(text => ExecuteDemoButtonCommand(text), _ => IsNotAuthenticating);
			ConnectToGitHubButtonCommand = new AsyncCommand<(CancellationToken CancellationToken, Xamarin.Essentials.BrowserLaunchOptions? BrowserLaunchOptions), object?>(tuple => ExecuteConnectToGitHubButtonCommand(gitHubAuthenticationService, deepLinkingService, gitHubUserService, tuple.CancellationToken, tuple.BrowserLaunchOptions), _ => IsNotAuthenticating);

			GitHubUserService = gitHubUserService;
			GitHubAuthenticationService = gitHubAuthenticationService;
		}

		public IAsyncCommand<(CancellationToken CancellationToken, Xamarin.Essentials.BrowserLaunchOptions? BrowserLaunchOptions)> ConnectToGitHubButtonCommand { get; }
		public IAsyncCommand<string?> DemoButtonCommand { get; }

		public bool IsNotAuthenticating => !IsAuthenticating;

		public virtual bool IsDemoButtonVisible => !IsAuthenticating && GitHubUserService.Alias != DemoUserConstants.Alias;

		protected GitHubAuthenticationService GitHubAuthenticationService { get; }
		protected GitHubUserService GitHubUserService { get; }

		public bool IsAuthenticating
		{
			get => _isAuthenticating;
			set => SetProperty(ref _isAuthenticating, value, () =>
			{
				NotifyIsAuthenticatingPropertyChanged();
				MainThread.InvokeOnMainThreadAsync(ConnectToGitHubButtonCommand.RaiseCanExecuteChanged).SafeFireAndForget(ex => Debug.WriteLine(ex));
			});
		}

		protected virtual void NotifyIsAuthenticatingPropertyChanged()
		{
			OnPropertyChanged(nameof(IsNotAuthenticating));
			OnPropertyChanged(nameof(IsDemoButtonVisible));
		}

		protected virtual Task ExecuteDemoButtonCommand(string? buttonText)
		{
			IsAuthenticating = true;
			return Task.CompletedTask;
		}

		protected async virtual Task ExecuteConnectToGitHubButtonCommand(GitHubAuthenticationService gitHubAuthenticationService, DeepLinkingService deepLinkingService, GitHubUserService gitHubUserService, CancellationToken cancellationToken, Xamarin.Essentials.BrowserLaunchOptions? browserLaunchOptions = null)
		{
			IsAuthenticating = true;

			// Yield from the Main Thread to allow IsAuthenticating indicator to appear
			await Task.Yield();

			try
			{
				var loginUrl = gitHubAuthenticationService.GetGitHubLoginUrl();

				if (!string.IsNullOrWhiteSpace(loginUrl))
				{
					await deepLinkingService.OpenBrowser(loginUrl, browserLaunchOptions).ConfigureAwait(false);
				}
				else
				{
					await deepLinkingService.DisplayAlert("Error", "Couldn't connect to GitHub Login. Check your internet connection and try again", "OK").ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				AnalyticsService.Report(e);
			}
			finally
			{
				IsAuthenticating = false;
			}
		}

		void HandleAuthorizeSessionStarted(object sender, EventArgs e) => IsAuthenticating = true;
		void HandleAuthorizeSessionCompleted(object sender, AuthorizeSessionCompletedEventArgs e) => IsAuthenticating = false;
	}
}