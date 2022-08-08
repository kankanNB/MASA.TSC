﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Masa.Tsc.Contracts.Admin.Infrastructure.Dtos;

public class Pagination<T> : FromUri<T>
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}