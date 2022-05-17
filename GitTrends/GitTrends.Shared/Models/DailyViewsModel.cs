﻿using System;
using Newtonsoft.Json;

namespace GitTrends.Shared
{
	public record DailyViewsModel(DateTimeOffset Timestamp, long Count, long Uniques) : BaseDailyModel(Timestamp, Count, Uniques), IDailyViewsModel
	{
		[JsonIgnore]
		public long TotalViews => TotalCount;

		[JsonIgnore]
		public long TotalUniqueViews => TotalUniqueCount;
	}
}