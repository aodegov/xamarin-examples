﻿using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TurnipTracker.Shared
{
    /// <summary>
    /// This is information that will be pulled down for friends list
    /// </summary>
    public class UserEntity : TableEntity
    {
        public UserEntity()
        {

        }
        public UserEntity(string publicKey, string privateKey)
        {
            PartitionKey = publicKey;
            RowKey = privateKey;
        }

        // Index column
        // PartitionKey
        public string PublicKey => PartitionKey;

        // Primary Key
        // RowKey
        public string PrivateKey => RowKey;
        public string Name { get; set; }
        public string IslandName { get; set; }

        //could be tiny int?
        public int Fruit { get; set; }
        public string TimeZone { get; set; }
        public string Status { get; set; }

        //could be small int
        public int AMPrice { get; set; }
        public int PMPrice { get; set; }
        public int MaxPrediction { get; set; }
        public int MinPrediction { get; set; }

        public int BuyPrice { get; set; }

        //could be smallint
        public int TurnipUpdateDayOfYear { get; set; }
        public int TurnipUpdateYear { get; set; }
        public DateTime TurnipUpdateTimeUTC { get; set; }
        public string FriendCode { get; set; }
        public string DodoCode { get; set; }
        public int GateStatus { get; set; }
        public DateTime? GateClosesAtUTC { get; set; }
    }
}
