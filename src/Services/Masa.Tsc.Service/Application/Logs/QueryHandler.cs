﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Nest;

namespace Masa.Tsc.Service.Admin.Application.Logs
{
    public class QueryHandler
    {
        private IElasticClient _elasticClient;
        private IServiceProvider _provider;
        private ILogger _logger;
        private ICallerProvider _caller;

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
            await _elasticClient.GetAggregationAsync<object, LogAggQuery>(ElasticConst.LogIndex, query, Filter, Aggregation, AggResult);
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

        private IAggregationContainer Aggregation(AggregationContainerDescriptor<object> aggContainer, LogAggQuery query)
        {
            if (query.FieldMaps == null || !query.FieldMaps.Any())
                return aggContainer;
            foreach (var item in query.FieldMaps)
            {
                switch (item.AggType)
                {
                    case LogAggTypes.Count:
                        {
                            aggContainer.ValueCount(item.Alias, agg => agg.Field(item.Name));
                        }
                        break;
                    case LogAggTypes.Sum:
                        {
                            aggContainer.Sum(item.Alias, agg => agg.Field(item.Name));
                        }
                        break;
                    case LogAggTypes.Avg:
                        {
                            aggContainer.Average(item.Alias, agg => agg.Field(item.Name));
                        }
                        break;
                }
            }
            return aggContainer;
        }

        private void AggResult(ISearchResponse<object> response, LogAggQuery query)
        {
            if (!response.IsValid)
            {
                if (response.TryGetServerErrorReason(out string msg))
                    throw new UserFriendlyException(msg);
                else
                    _logger.LogError($"Aggregation query error: {0}", response);
            }

            if (response.Aggregations == null || !response.Aggregations.Any())
                return;

            var result = new Dictionary<string, string>();
            foreach (var item in response.Aggregations)
            {
                if (item.Value is ValueAggregate value && value != null)
                {
                    string tem = null;
                    if (!string.IsNullOrEmpty(value.ValueAsString))
                        tem = value.ValueAsString;
                    else if (value.Value.HasValue)
                        tem = value.Value.Value.ToString();

                    if (string.IsNullOrEmpty(tem))
                        continue;

                    result.Add(item.Key, tem);
                }
            }
            query.Result = result;
        }
        #endregion

        [EventHandler]
        public async Task GetLatestDataAsync(LatestLogQuery query)
        {
            var response = await _elasticClient.SearchAsync<object>(q => q.Index(ElasticConst.LogIndex).Query(q => Filter(q, new LogAggQuery { Start = query.Start, End = query.End, Query = query.Query })).Sort(s =>
            {
                if (query.IsDesc)
                    return s.Descending(ElasticConst.LogTimestamp);
                else
                    return s.Ascending(ElasticConst.LogTimestamp);
            }).Size(1));
            if (response.IsValid)
            {
                if (response.Documents.Any())
                    query.Result = response.Documents.First();
            }
            else
            {
                _logger.LogError("GetLatestDataAsync Error {0}", response);
            }
        }

        [EventHandler]
        public async Task GetMappingAsync(LogFieldQuery query)
        {
            if (query == null)
                return;
            var result = await _caller.GetMappingAsync(ElasticConst.LogIndex);
            if (result != null)
                query.Result = result;
        }
    }
}
