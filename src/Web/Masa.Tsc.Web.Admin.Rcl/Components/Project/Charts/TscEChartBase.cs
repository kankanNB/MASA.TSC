﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Masa.Tsc.Web.Admin.Rcl.Components;

public partial class TscEChartBase : TscComponentBase
{
    protected bool _isLoading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(_isLoading)
            await OnLoadAsync(default!);
        await base.OnAfterRenderAsync(firstRender);
    }

    public async Task OnLoadAsync(Dictionary<string, object> queryParams)
    {
        await LoadAsync(queryParams);
        _isLoading = false;
        StateHasChanged();
    }

    protected virtual async Task LoadAsync(Dictionary<string, object> queryParams)
    {
        Thread.Sleep(200);
        await Task.CompletedTask;
    }
}
