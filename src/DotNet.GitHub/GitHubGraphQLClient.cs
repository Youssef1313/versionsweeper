﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DotNet.GitHub;

public sealed class GitHubGraphQLClient
{
    const string IssueQuery = """
        query($search_value: String!) {
          search(type: ISSUE, query: $search_value, first: 10) {
            nodes {
              ... on Issue {
                title
                number
                url
                body
                state
                createdAt
                updatedAt
                closedAt
              }
            }
          }
        }
        """;

    static readonly Uri s_graphQLUri = new("https://api.github.com/graphql");

    readonly HttpClient _httpClient;
    readonly ILogger<GitHubGraphQLClient> _logger;

    public GitHubGraphQLClient(HttpClient httpClient, ILogger<GitHubGraphQLClient> logger) =>
        (_httpClient, _logger) = (httpClient, logger);

    public async Task<(bool IsError, ExistingIssue? Issue)> GetIssueAsync(
        string owner, string name, string token, string title)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new("Token", token);
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new(GitHubProduct.Header.Name, GitHubProduct.Header.Version));

            GraphQLRequest graphQLRequest = new()
            {
                Query = IssueQuery,
                Variables =
                    {
                        ["search_value"] = $"repo:{owner}/{name} type:issue '{title}' in:title"
                    }
            };

            using var request = new StringContent(graphQLRequest.ToString());
            request.Headers.ContentType = new(MediaTypeNames.Application.Json);
            request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

            using HttpResponseMessage response = await _httpClient.PostAsync(s_graphQLUri, request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            GraphQLResult<ExistingIssue>? result = json.FromJson<GraphQLResult<ExistingIssue>>(
                GitHubJsonSerializerContext.Default.GraphQLResultExistingIssue);

            return (false, result?.Data?.Search?.Nodes
                ?.Where(i => i.State == ItemState.Open)
                ?.OrderByDescending(i => i.CreatedAt.GetValueOrDefault())
                ?.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ {Message}", ex.Message);

            return (true, default);
        }
    }
}
