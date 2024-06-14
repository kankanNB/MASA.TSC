﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Masa.Tsc.Web.Admin.Rcl.Components.Apm;

public partial class ApmTraceView
{
    [Inject]
    IJSRuntime JSRuntime { get; set; }

    [Parameter]
    public object Value { get; set; }

    [Parameter]
    public bool Show { get; set; }

    [Parameter]
    public bool Dialog { get; set; } = true;

    [Parameter]
    public string LinkUrl { get; set; }

    [Parameter]
    public EventCallback<bool> ShowChanged { get; set; }

    [Parameter]
    public EventCallback<bool> LoadingCompelete { get; set; }

    [Parameter]
    public StringNumber? Height { get; set; }

    private async Task CloseAsync()
    {
        Show = false;
        if (ShowChanged.HasDelegate)
            await ShowChanged.InvokeAsync(Show);
        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        if ((!Dialog || Show) && Value != null)
        {
            var newKey = JsonSerializer.Serialize(Value);
            if (!string.Equals(md5Key, newKey))
            {
                if (Value is Dictionary<string, object> dic)
                    _dic = dic;
                else
                    _dic = Value.ToDictionary();
                md5Key = newKey;
            }
        }
        base.OnParametersSet();
    }
    private IDictionary<string, object>? _dic = null;
    private string md5Key;
    private string search = string.Empty;

    private void OnSeach(string value)
    {
        search = value;
    }

    private async Task OpenLogAsync()
    {
        await JSRuntime.InvokeVoidAsync("open", LinkUrl, "_blank");
    }
}