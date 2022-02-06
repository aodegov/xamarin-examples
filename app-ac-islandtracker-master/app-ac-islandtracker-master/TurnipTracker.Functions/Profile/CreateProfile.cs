using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TurnipTracker.Shared;
using Microsoft.WindowsAzure.Storage.Table;
using TurnipTracker.Functions.Helpers;
using System.Web.Http;
using TurnipTracker.Functions.Model;

namespace TurnipTracker.Functions
{
    public static class CreateProfile
    {
        [FunctionName(nameof(CreateProfile))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Table("User")] CloudTable cloudTable,
            [Table("ProStatus")] CloudTable proTable,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger {nameof(CreateProfile)} function processed a request.");


            var privateKey = Utils.ParseToken(req);
            if (privateKey == null)
                return new UnauthorizedResult();

            User user = null;

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                user = JsonConvert.DeserializeObject<User>(requestBody);
            }
            catch(Exception ex)
            {
                log.LogWarning("Unable to deserialize user: " + ex.Message);
            }

            if (user == null || 
                string.IsNullOrWhiteSpace(user.PublicKey) ||
                string.IsNullOrWhiteSpace(user.Name) ||
                string.IsNullOrWhiteSpace(user.IslandName) ||
                user.TimeZone == null)
            {
                return new BadRequestErrorMessageResult("Invalid data to process request");
            }
            var encryptedFriendCode = string.Empty;
            try
            { 
                encryptedFriendCode = Cipher.Encrypt(user.FriendCode, Utils.FriendCodePassword, user.PublicKey);
            }
            catch (Exception ex)
            {
                log.LogError("Unable to decrypt friendcode: " + ex.Message);
            }
            var userEntity = new UserEntity(user.PublicKey, privateKey)
            {
                Name = user.Name,
                IslandName = user.IslandName,
                Fruit = user.Fruit,
                TimeZone = user.TimeZone,
                Status = user.Status ?? string.Empty,
                TurnipUpdateTimeUTC = DateTime.UtcNow,
                FriendCode = encryptedFriendCode,
                GateClosesAtUTC = DateTime.UtcNow
            };

            try
            {
#if DEBUG
                userEntity.ETag = "*";
#endif
                // Create the InsertOrReplace table operation
                var insertOrMergeOperation = TableOperation.InsertOrMerge(userEntity);

                // Execute the operation.
                var result = await cloudTable.ExecuteAsync(insertOrMergeOperation);
                var insertedCustomer = result.Result as UserEntity;


                bool.TryParse(Environment.GetEnvironmentVariable("AUTO_PRO"), out var autoPro);
                if (autoPro)
                {
                    try
                    {

                        var proEntity = new ProStatusEntity(user.PublicKey, privateKey)
                        {
                            Receipt = "pre-pro"
                        };

                        // Create the InsertOrReplace table operation
                        var insertOrMergeOperationPro = TableOperation.InsertOrMerge(proEntity);

                        // Execute the operation.
                        var resultPro = await proTable.ExecuteAsync(insertOrMergeOperationPro);
                        var insertedPro = resultPro.Result as ProStatusEntity;
                    }
                    catch(Exception expro)
                    {
                        log.LogError($"Error {nameof(CreateProfile)} - Error: " + expro.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                log.LogError($"Error {nameof(CreateProfile)} - Error: " + ex.Message);
                return new InternalServerErrorResult();
            }

            return new OkObjectResult("User Created");
        }
    }
}
