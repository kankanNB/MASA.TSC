﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using BlazorComponent;

namespace Masa.Tsc.Web.Admin.Rcl.Components;

public partial class ServiceResponseAvgTime : TscEChartBase
{
    [Parameter]
    public StringNumber Width { get; set; } = "100%";

    [Parameter]
    public StringNumber Height { get; set; } = "100%";

    [Parameter]
    public string Title { get; set; }

    private double total { get; set; } = 0;

    private string unit { get; set; } = "ms";

    internal override async Task LoadAsync(ProjectAppSearchModel query)
    {
        var step = (long)Math.Floor((query.End!.Value - query.Start!.Value).TotalSeconds);
        var metric = $"round(sum by(service_name) (increase(http_server_duration_sum{{service_name=\"{query.AppId}\"}}[{step}s]))/sum by(service_name) (increase(http_server_duration_count{{service_name=\"{query.AppId}\"}}[{step}s])),1)";
        total = 0;
        var result = await ApiCaller.MetricService.GetQueryAsync(metric, query.End!.Value);
        if (result != null && result.Result != null && result.Result.Any() && result.ResultType == Utils.Data.Prometheus.Enums.ResultTypes.Vector)
        {
            var obj = ((QueryResultInstantVectorResponse)result.Result[0])!.Value![1];
            total = obj is double.NaN || string.Equals(obj, "NaN") ? 0 : Convert.ToDouble(obj);

            if (total - 1000 > 0)
            {
                unit = "s";
                total = total / 1000.0;
            }
            else
            {
                unit = "ms";
            }
        }
    }
}