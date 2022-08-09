﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.
using Nest;

namespace Masa.Tsc.Service.Admin.Application.Logs;

public class QueryHandler
{
    private readonly IElasticClient _elasticClient;
    private readonly IServiceProvider _provider;
    private readonly ILogger _logger;
    private readonly ICallerProvider _caller;

    public QueryHandler(IServiceProvider provider, IElasticClient elasticClient, ILogger<QueryHandler> logger)
    {
        _provider = provider;
        _elasticClient = elasticClient;
        _logger = logger;
        _caller = _provider.GetRequiredService<ICallerFactory>().CreateClient(ElasticConst.ES_HTTP_CLIENT_NAME);
    }

    #region agg query
    [EventHandler]
    public async Task GetAggregationAsync(LogAggQuery query)
    {
        await _elasticClient.SearchAsync<object, LogAggQuery>(ElasticConst.LogIndex, query, condition: Filter,
            aggregate: (agg, q) => IElasticClientExtenstion.Aggregation(agg, q.FieldMaps),
            result: (rep, q) => q.Result = IElasticClientExtenstion.AggResult(rep, q.FieldMaps)!,
            logger: _logger);
    }

    private QueryContainer Filter(QueryContainerDescriptor<object> container, LogAggQuery query)
    {
        var list = new List<Func<QueryContainerDescriptor<object>, QueryContainer>>();
        if (!string.IsNullOrEmpty(query.Query))
        {
            list.Add(q => q.Raw(query.Query));
        }
        if (query.Start > DateTime.MinValue && query.End > DateTime.MinValue && query.Start < query.End)
        {
            list.Add(q => q.DateRange(f => f.GreaterThanOrEquals(query.Start).LessThanOrEquals(query.End).Field(ElasticConst.LogTimestamp)));
        }

        if (list.Any())
            container.Bool(b => b.Must(list));

        return container;
    }
    #endregion

    [EventHandler]
    public async Task GetLatestDataAsync(LatestLogQuery query)
    {
        await _elasticClient.SearchAsync<object, LatestLogQuery>(ElasticConst.LogIndex, query, condition: LastLogFilter,
           result: (result, q) =>
           {
               query.Result = result.Documents.FirstOrDefault() ?? default(JsonElement);
           },
           pageration: () => ValueTuple.Create(true, 1, 1),
           sort: (sort, q) =>
           {
               if (q.IsDesc)
                   return sort.Descending(ElasticConst.LogTimestamp);
               else
                   return sort.Ascending(ElasticConst.LogTimestamp);
           },
           logger: _logger);
    }

    private QueryContainer LastLogFilter(QueryContainerDescriptor<object> container, LatestLogQuery query)
    {
        var list = new List<Func<QueryContainerDescriptor<object>, QueryContainer>>();
        if (!string.IsNullOrEmpty(query.Query))
        {
            list.Add(q => q.Raw(query.Query));
        }
        if (query.Start > DateTime.MinValue && query.End > DateTime.MinValue && query.Start < query.End)
        {
            list.Add(q => q.DateRange(f => f.GreaterThanOrEquals(query.Start).LessThanOrEquals(query.End).Field(ElasticConst.LogTimestamp)));
        }

        if (list.Any())
            container.Bool(b => b.Must(list));

        return container;
    }

    [EventHandler]
    public async Task GetMappingAsync(LogFieldQuery query)
    {
        if (query == null)
            return;
        var result = await _caller.GetMappingAsync(ElasticConst.LogIndex);
        if (result != null && result.Any())
        {
            query.Result = result.Select(m => new Contracts.Admin.MappingResponse
            {
                DataType = m.DataType,
                IsKeyword = m.IsKeyword,
                MaxLenth = m.MaxLenth,
                Name = m.Name,
            });
        }
        else
        {
            query.Result = default!;
        }
    }

    [EventHandler]
    public async Task GetPageListAsync(LogsQuery query)
    {
        await _elasticClient.SearchAsync<object, LogsQuery>(ElasticConst.LogIndex, query, condition: PageFilter,
           result: (result, q) => query.Result = new PaginationDto<object>(result.Total, result.Documents?.ToList() ?? default!),
           pageration: () => ValueTuple.Create(true, query.Page, query.Size),
           sort: (sort, q) =>sort.Field(ElasticConst.LogTimestamp, query.Sort == "asc" ? SortOrder.Ascending : SortOrder.Descending),
           logger: _logger);
    }

    private QueryContainer PageFilter(QueryContainerDescriptor<object> container, LogsQuery query)
    {
        var list = new List<Func<QueryContainerDescriptor<object>, QueryContainer>>();
        if (!string.IsNullOrEmpty(query.Query))
        {
            list.Add(q => q.QueryString(t => t.Query(query.Query).DefaultOperator(Operator.And)));
        }
        if (query.Start > DateTime.MinValue && query.End > DateTime.MinValue && query.Start < query.End)
        {
            list.Add(q => q.DateRange(f => f.GreaterThanOrEquals(query.Start).LessThanOrEquals(query.End).Field(ElasticConst.LogTimestamp)));
        }

        if (list.Any())
            container.Bool(b => b.Must(list));

        return container;
    }
}
