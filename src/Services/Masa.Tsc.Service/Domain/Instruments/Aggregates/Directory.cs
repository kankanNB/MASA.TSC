﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Masa.Tsc.Service.Admin.Domain.Instruments.Aggregates;

public class Directory : AggregateRoot<Guid>
{
    public string Name { get; set; }

    public int Sort { get; set; }

    public Guid? ParentId { get; set; }
}
