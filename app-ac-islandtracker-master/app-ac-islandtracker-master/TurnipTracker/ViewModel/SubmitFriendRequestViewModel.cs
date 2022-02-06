﻿using System;
using System.Threading.Tasks;
using Microsoft.AppCenter.Crashes;
using MvvmHelpers;
using MvvmHelpers.Commands;
using TurnipTracker.Services;
using Xamarin.Forms;

namespace TurnipTracker.ViewModel
{
    [QueryProperty("Name", "name")]
    [QueryProperty("Id", "id")]
    public class SubmitFriendRequestViewModel : ViewModelBase
    {
        public AsyncCommand RequestFriendCommand { get; }
        public SubmitFriendRequestViewModel()
        {
            RequestFriendCommand = new AsyncCommand(RequestFriend);
        }

        bool submitted;
        public bool Submitted
        {
            get => submitted;
            set => SetProperty(ref submitted, value);
        }

        bool needsVerification = true;
        public bool NeedsVerification
        {
            get => needsVerification;
            set => SetProperty(ref needsVerification, value);
        }

        bool showClose;
        public bool ShowClose
        {
            get => showClose;
            set => SetProperty(ref showClose, value);
        }

        bool error;
        public bool Error
        {
            get => error;
            set => SetProperty(ref error, value);
        }

        string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, Uri.UnescapeDataString(value ?? string.Empty));
        }

        string id;
        public string Id
        {
            get => id;
            set => SetProperty(ref id, Uri.UnescapeDataString(value ?? string.Empty));
        }


        async Task RequestFriend()
        {
            if (IsBusy)
                return;

            if (!SettingsService.HasRegistered)
            {
                await App.Current.MainPage.DisplayAlert("Register First", "Please create your profile before sbmitting friend request.", "OK");
                return;
            }

            NeedsVerification = false;


            

            IsBusy = true;
            try
            {
                await DataService.SubmitFriendRequestAsync(Id);

                Submitted = true;
                ShowClose = true;
            }
            catch (HttpResponseException hrex) when (!string.IsNullOrWhiteSpace(hrex.Message))
            {
                await DisplayAlert("Uh oh, turbulence", hrex.Message);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Uh oh, turbulence", "Looks like something went wrong. Check internet and try again.");
                Crashes.TrackError(ex);
            }
            finally
            {
                ShowClose = true;
                IsBusy = false;
            }


        }
    }
}
