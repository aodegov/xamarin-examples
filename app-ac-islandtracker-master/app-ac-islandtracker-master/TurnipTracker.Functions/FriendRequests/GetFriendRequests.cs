using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web.Http;
using TurnipTracker.Shared;
using TurnipTracker.Functions.Helpers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Linq;

namespace TurnipTracker.Functions
{
    public static class GetFriendRequests
    {
        [FunctionName(nameof(GetFriendRequests))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetFriendRequests/{myPublicKey}")] HttpRequest req,
            string myPublicKey,
            [Table("FriendRequest")] CloudTable friendRequestTable,
            [Table("Friend")] CloudTable friendTable,
            [Table("User")] CloudTable userTable,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger {nameof(SubmitFriendRequest)} function processed a request.");


            var privateKey = Utils.ParseToken(req);
            if (privateKey == null)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("You are not authorized to make this request.")
                };

            if (string.IsNullOrWhiteSpace(myPublicKey))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid data to process request")
                };
            }

            try
            {
                var user = await Utils.FindUserEntitySlim(userTable, privateKey, myPublicKey);
                if (user == null)
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Unable to locate your user account.")
                    };
            }
            catch (Exception ex)
            {
                log.LogError("User doesn't exist: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Unable to locate your user account.")
                };
            }


            var requests = new List<PendingFriendRequest>();
            try
            {
                var publicKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", 
                    QueryComparisons.Equal, myPublicKey);

                var rangeQuery = new TableQuery<FriendRequestEntity>().Where(publicKeyFilter);

                var existingFriendRequests = await friendRequestTable.ExecuteQuerySegmentedAsync(rangeQuery, null);

                // spin off tasks for finding all friends
                var tasks = new List<Task<TableQuerySegment<UserEntity>>>();
                foreach (var f in existingFriendRequests)
                {
                    tasks.Add(Utils.FindFriendTask(userTable, f.RequesterPublicKey));
                }

                await Task.WhenAll(tasks);

                for (var i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    var friend = t.Result?.Results?.FirstOrDefault();
                    if (friend == null)
                        if (friend == null)
                        continue;
                    requests.Add(new PendingFriendRequest
                    {
                        CreationDateUTC = existingFriendRequests.ElementAt(i).Timestamp.UtcDateTime,
                        IslandName = friend.IslandName,
                        Name = friend.Name,
                        RequesterPublicKey = friend.PublicKey
                    });
                }

            }
            catch (Exception ex)
            {
                log.LogError($"Error {nameof(GetFriendRequests)} - Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Unable to process friend information.")
                };
            }

            var json = JsonConvert.SerializeObject(requests);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
