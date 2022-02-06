﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AppCenter.Crashes;
using MvvmHelpers;
using MvvmHelpers.Commands;
using TurnipTracker.Services;
using TurnipTracker.Shared;
using Xamarin.Essentials;
using Xamarin.Forms;
using TurnipTracker.Helpers;
using Microsoft.AppCenter.Analytics;
using TurnipTracker.Model;

namespace TurnipTracker.ViewModel
{
    public class FriendsViewModel : ViewModelBase
    {
        public ObservableRangeCollection<FriendStatus> Friends { get; }
        public ObservableRangeCollection<FriendGroup> FriendsGrouped { get; }
        public bool ShowNoFriends => FriendsGrouped.Count == 0;
        public bool HasFriends => !ShowNoFriends;

        public AsyncCommand<string> ViewFriendCodeCommand { get; }

        public FriendsViewModel()
        {
            ViewFriendCodeCommand = new AsyncCommand<string>(ViewFriendCode);
            Friends = new ObservableRangeCollection<FriendStatus>();
            FriendsGrouped = new ObservableRangeCollection<FriendGroup>();
            RegisterFriendClipboardCommand = new AsyncCommand(RegisterFriendClipboard);
            RegisterFriendCommand = new AsyncCommand<string>(RegisterFriend);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            SendFriendRequestCommand = new AsyncCommand<Xamarin.Forms.View>(SendFriendRequest);
            RemoveFriendCommand = new AsyncCommand<FriendStatus>(RemoveFriend);
            GoToFriendRequestCommand = new AsyncCommand(GoToFriendRequest);
            AddFriendManuallyCommand = new AsyncCommand(AddFriendManually);
            var cache = DataService.GetCache<IEnumerable<FriendStatus>>(DataService.FriendKey);
            if (cache != null)
            {
                Friends.ReplaceRange(cache.OrderByDescending(s => s.TurnipUpdateTimeUTC));
                UpdateFriendsGroups();
            }

            
        }

        async Task ViewFriendCode(string code)
        {
            await DisplayAlert("Friend Code", code);
            Analytics.TrackEvent("ViewFriendCode");
        }

        string requestCount = string.Empty;
        public string RequestCount
        {
            get => requestCount;
            set => SetProperty(ref requestCount, value);
        }

        public bool ForceRefresh { get; set; }

        public string LastUpdate
        {
            get
            {
                var time = SettingsService.LastFriendsUpdate;
                if (time == DateTime.MinValue)
                    return string.Empty;
                var local = time.ToLocalTime();
                return $"Last friend sync: {local.ToShortDateString()} at {local.ToShortTimeString()}";
            }
        }


        public AsyncCommand AddFriendManuallyCommand { get; set; }

        async Task AddFriendManually()
        {
            var clip = string.Empty;
            if (Clipboard.HasText)
            {
                var val = await Clipboard.GetTextAsync();
                if(val != null & val.StartsWith("acislandtracker://"))
                {
                    clip = val;
                    await Clipboard.SetTextAsync(string.Empty);
                }
            }
            var link = await App.Current.MainPage.DisplayPromptAsync("Enter friend's invite link", "Paste in your friend's invite link that they sent you.", "Submit", "Cancel", "acislandtracker://", initialValue: clip);

            if (link != null && link.StartsWith("acislandtracker://"))
            {
                try
                {
                    var uri = new Uri(link);

                    if (Clipboard.HasText)
                    {
                        var val = await Clipboard.GetTextAsync();
                        if (val != null & val.StartsWith("acislandtracker://"))
                        {
                            await Clipboard.SetTextAsync(string.Empty);
                        }
                    }

                    await Shell.Current.GoToAsync($"//{uri.Host}/{uri.PathAndQuery}");
                    Analytics.TrackEvent("AddFriendManually");
                }
                catch
                {

                }
            }
        }


        public AsyncCommand<Xamarin.Forms.View> SendFriendRequestCommand { get; set; }

        async Task SendFriendRequest(Xamarin.Forms.View element)
        {
            if(!SettingsService.HasRegistered)
            {
                await DisplayAlert("Register First", "Please create a profile before sending friend requests.");
                return;
            }

            if (SettingsService.FirstFriendRequest)
            {
                await DisplayAlert("How to send a request",
                    "Looks like this is your first time sending a friend request! Exciting! Simply share the friend invite link with your friends and have them click the link to open Island Tracker or copy it to their clipboard and go to their friends tab. Once they request you as a friend you will see the request in the 'Request' section above that you can either approve or deny.");
                SettingsService.FirstFriendRequest = false;
            }

            var name = DataService.GetProfile().Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Update Profile", "Please update profile before sending a friend request.");
                return;
            }

            if (!(await CheckConnectivity("Check connectivity", "Unable to update profile, please check internet and try again")))
                return;

            var key = await SettingsService.GetPublicKey();
            var message = $"acislandtracker://friends/invite?id={key}&name={Uri.EscapeDataString(name)}";

            //#if DEBUG
            //            await Launcher.OpenAsync(message);
            //#else
            var bounds = element.GetAbsoluteBounds();
            await Share.RequestAsync(new ShareTextRequest
            {
                Title = "Island Tracker Invite",
                Text = message,
                PresentationSourceBounds = bounds.ToSystemRectangle()
            });
            //#endif

            Analytics.TrackEvent("SendFriendRequest");
        }

        public AsyncCommand GoToFriendRequestCommand { get; }

        async Task GoToFriendRequest()
        {
            if (!SettingsService.HasRegistered)
            {
                await App.Current.MainPage.DisplayAlert("Register First", "Create a profile before checking friend requests.", "OK");
                return;
            }

            ForceRefresh = true;

            await GoToAsync("friendrequests");
        }

        public AsyncCommand<string> RegisterFriendCommand { get; }
        public AsyncCommand RegisterFriendClipboardCommand { get; }

        public async Task<bool> RegisterFriendClipboard()
        {
            if (!Clipboard.HasText)
                return false;

            var clip = await Clipboard.GetTextAsync();
            if(await RegisterFriend(clip))
            {
                Analytics.TrackEvent("RegisterFriend", new Dictionary<string, string>
                {
                    ["type"] = "clipboard"
                });
                await Clipboard.SetTextAsync(string.Empty);
                return true;
            }

            return false;
        }

        bool parsing;
        async Task<bool> RegisterFriend(string uriString)
        {
            if (parsing)
                return false;

            parsing = true;
            try
            {
                if (string.IsNullOrWhiteSpace(uriString))
                    return false;

                var created = Uri.TryCreate(uriString, UriKind.Absolute, out var uri);

                if (!created)
                    return false;

                if (uri != null && uri.Host == "friends" && uri.Scheme == "acislandtracker")
                {

                    var key = await SettingsService.GetPublicKey();
                    if (uriString.Contains(key))
                        return false;

                    // we are already on the friends page, so no need to use the host.
                    await GoToAsync($"//{uri.Host}/{uri.PathAndQuery}", uri.Host);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
            finally
            {
                parsing = false;
            }

            return false;
        }

        public AsyncCommand RefreshCommand { get; set; }
        async Task RefreshAsync()
        {
            if (IsBusy)
                return;

            if (!SettingsService.HasRegistered)
            {
                await DisplayAlert("Register First", "Please create a profile before syncing friends.");
                return;
            }

            IsBusy = true;
            var fake = Friends.Count == 0;
            try
            {
                if (fake)
                {
                    FriendsGrouped.Add(new FriendGroup("Updating...", new List<FriendStatus>
                    {
                        new FriendStatus()
                    }));
                    OnPropertyChanged(nameof(ShowNoFriends));
                }

                var friendsTask = DataService.GetFriendsAsync(ForceRefresh);
                var countTask = DataService.GetFriendRequestCountAsync(ForceRefresh);
                await Task.WhenAll(friendsTask, countTask);
                if (friendsTask.IsFaulted && friendsTask.Exception != null)
                    throw friendsTask.Exception;

                var statuses = friendsTask.Result;
                ForceRefresh = false;
                // await Task.Delay(5000);
                Friends.ReplaceRange(statuses.OrderByDescending(s => s.TurnipUpdateTimeUTC));

                UpdateFriendsGroups();

                SettingsService.LastFriendsUpdate = DateTime.UtcNow;
                OnPropertyChanged(nameof(LastUpdate));

                if (!countTask.IsFaulted)
                    RequestCount = countTask.Result.Count == 0 ? string.Empty : countTask.Result.Count.ToString();
                else
                    RequestCount = string.Empty;


                SettingsService.FriendRequestCount = RequestCount;

            }
            catch (HttpResponseException hrex) when (!string.IsNullOrWhiteSpace(hrex.Message))
            {
                if (fake)
                    FriendsGrouped.Clear();
                await DisplayAlert("Uh oh, turbulence", hrex.Message);
            }
            catch (Exception ex)
            {
                if (fake)
                    FriendsGrouped.Clear();

                await DisplayAlert("Uh oh, turbulence", "Looks like something went wrong. Check internet and try again.");

                Crashes.TrackError(ex);
            }
            finally
            {
                IsBusy = false;
            }
            OnPropertyChanged(nameof(ShowNoFriends));
            OnPropertyChanged(nameof(HasFriends));
        }

        int sortOption = 0;
        public int SortOption
        {
            get => sortOption;
            set
            {
                if (SetProperty(ref sortOption, value))
                    UpdateFriendsGroups();
            }
        }

        void UpdateFriendsGroups()
        {
            if (sortOption == 0)
            {
                var today = Friends.Where(f => f.TurnipUpdateTimeUTC.ToLocalTime().DayOfYear == DateTime.Now.DayOfYear).ToList();
                var older = Friends.Where(f => f.TurnipUpdateTimeUTC.ToLocalTime().DayOfYear != DateTime.Now.DayOfYear).ToList();

                FriendsGrouped.Clear();
                if (today.Count > 0)
                    FriendsGrouped.Add(new FriendGroup("Updated Today", today));
                if (older.Count > 0)
                    FriendsGrouped.Add(new FriendGroup("Older Updates", older));
            }
            else
            {
                var open = Friends.Where(f => f.GateStatus != 0 && f.GateClosesAtUTC.HasValue && f.GateClosesAtUTC.Value > DateTime.UtcNow).OrderBy(f => f.GateClosesAtUTC.Value).ToList();
                var closed = Friends.Where(f => f.GateStatus == 0 || !f.GateClosesAtUTC.HasValue || f.GateClosesAtUTC.Value <= DateTime.UtcNow).ToList();

                FriendsGrouped.Clear();
                if (open.Count > 0)
                    FriendsGrouped.Add(new FriendGroup("Open", open));
                if (closed.Count > 0)
                    FriendsGrouped.Add(new FriendGroup("Closed", closed));

            }
        }

        public AsyncCommand<FriendStatus> RemoveFriendCommand { get; set; }
        async Task RemoveFriend(FriendStatus friendStatus)
        {
            if (IsBusy)
                return;

            if (!(await CheckConnectivity("Check connectivity", "Unable to update profile, please check internet and try again")))
                return;

            if (!await DisplayAlert("Remove friend?", $"Are you sure you want to remove {friendStatus.Name}?", "Yes, remove", "Cancel"))
                return;

            Analytics.TrackEvent("RemoveFriend");

            IsBusy = true;
            try
            {

                await DataService.RemoveFriendAsync(friendStatus.PublicKey);
                Friends.Remove(friendStatus);
                ForceRefresh = true;
                UpdateFriendsGroups();
                OnPropertyChanged(nameof(ShowNoFriends));
                OnPropertyChanged(nameof(HasFriends));
                DataService.ClearCache(DataService.FriendKey);
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
                IsBusy = false;
            }
        }
    }
}
