﻿// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

global using Masa.BuildingBlocks.BasicAbility.Auth;
global using Masa.BuildingBlocks.BasicAbility.Auth.Contracts.Model;
global using Masa.BuildingBlocks.BasicAbility.Pm;
global using Masa.BuildingBlocks.BasicAbility.Pm.Enum;
global using Masa.BuildingBlocks.Data.UoW;
global using Masa.BuildingBlocks.Ddd.Domain.Entities;
global using Masa.BuildingBlocks.Ddd.Domain.Entities.Full;
global using Masa.BuildingBlocks.Ddd.Domain.Repositories;
global using Masa.BuildingBlocks.Dispatcher.Events;
global using Masa.BuildingBlocks.Identity.IdentityModel;
global using Masa.BuildingBlocks.ReadWriteSpliting.Cqrs.Commands;
global using Masa.BuildingBlocks.ReadWriteSpliting.Cqrs.Queries;
global using Masa.Contrib.BasicAbility.Dcc;
global using Masa.Contrib.BasicAbility.Tsc;
global using Masa.Contrib.Data.Contracts.EF;
global using Masa.Contrib.Data.EntityFrameworkCore;
global using Masa.Contrib.Data.UoW.EF;
global using Masa.Contrib.Ddd.Domain.Repository.EF;
global using Masa.Contrib.Dispatcher.Events;
global using Masa.Contrib.Dispatcher.IntegrationEvents.Dapr;
global using Masa.Contrib.Dispatcher.IntegrationEvents.EventLogs.EF;
global using Masa.Contrib.Service.MinimalAPIs;
global using Masa.Tsc.Contracts.Admin;
global using Masa.Tsc.Contracts.Admin.Charts;
global using Masa.Tsc.Contracts.Admin.Enums;
global using Masa.Tsc.Contracts.Admin.Infrastructure.Dtos;
global using Masa.Tsc.Services.Observability.Elastic;
global using Masa.Tsc.Service.Admin;
global using Masa.Tsc.Service.Admin.Application.Instruments.Commands;
global using Masa.Tsc.Service.Admin.Application.Instruments.Queries;
global using Masa.Tsc.Service.Admin.Application.Logs;
global using Masa.Tsc.Service.Admin.Application.Metrics;
global using Masa.Tsc.Service.Admin.Application.Projects;
global using Masa.Tsc.Service.Admin.Application.Setting;
global using Masa.Tsc.Service.Admin.Application.Teams;
global using Masa.Tsc.Service.Admin.Application.Traces;
global using Masa.Tsc.Service.Admin.Domain.Aggregates;
global using Masa.Tsc.Service.Admin.Domain.Repositories;
global using Masa.Tsc.Service.Admin.Extenision;
global using Masa.Tsc.Service.Infrastructure;
global using Masa.Utils.Caller.Core;
global using Masa.Utils.Caller.HttpClient;
global using Masa.Utils.Data.Prometheus;
global using Masa.Utils.Data.Prometheus.Enums;
global using Masa.Utils.Data.Prometheus.Model;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.OpenApi.Models;
global using OpenTelemetry.Logs;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;
global using OpenTelemetry.Trace;
global using System.Reflection;
global using System.Text.Json;
