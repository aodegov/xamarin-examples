﻿using System;

namespace GitTrends.Shared
{
	public record Issue(string Title, string Body, DateTimeOffset CreatedAt, string State, DateTimeOffset? ClosedAt = null);
}