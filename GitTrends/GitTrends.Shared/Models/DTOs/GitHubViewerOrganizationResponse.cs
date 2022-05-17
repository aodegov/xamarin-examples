﻿using System.Collections.Generic;

namespace GitTrends.Shared
{
	public record GitHubViewerOrganizationResponse(ViewerOrganizationsResponse Viewer);

	public record ViewerOrganizationsResponse(OrganizationNamesResponse Organizations);

	public record OrganizationNamesResponse(IReadOnlyList<OrganizationRepositoryName> Nodes, PageInfo PageInfo);

	public record OrganizationRepositoryName(string Login);
}