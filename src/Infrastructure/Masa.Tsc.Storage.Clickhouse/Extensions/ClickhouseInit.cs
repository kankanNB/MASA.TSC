// Copyright (c) MASA Stack All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Masa.Tsc.Storage.Clickhouse.Extensions;

public static class ClickhouseInit
{
    private static ILogger Logger { get; set; }

    internal static MasaStackClickhouseConnection Connection { get; private set; }

    public static void Init(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var logfactory = serviceProvider.GetService<ILoggerFactory>();
        Logger = logfactory?.CreateLogger("Masa.Contrib.StackSdks.Tsc.Clickhouse")!;
        try
        {
            Connection = serviceProvider.GetRequiredService<MasaStackClickhouseConnection>();
            if (!ExistsTable(Connection, MasaStackClickhouseConnection.TraceSourceTable))
                throw new ArgumentNullException(nameof(MasaStackClickhouseConnection.TraceSourceTable));
            if (!ExistsTable(Connection, MasaStackClickhouseConnection.LogSourceTable))
                throw new ArgumentNullException(nameof(MasaStackClickhouseConnection.LogSourceTable));
            InitLog();
            //InitTrace(MasaStackClickhouseConnection.TraceTable);
            InitTrace(MasaStackClickhouseConnection.TraceSpanTable, "where SpanKind =='SPAN_KIND_SERVER'");
            InitTrace(MasaStackClickhouseConnection.TraceClientTable, "where SpanKind =='SPAN_KIND_CLIENT'");
            InitMappingTable();
            var timezoneStr = GetTimezone(Connection);
            MasaStackClickhouseConnection.TimeZone = TZConvert.GetTimeZoneInfo(timezoneStr);
        }
        finally
        {
            Connection?.Dispose();
        }
    }

    private static void InitLog()
    {
        var viewTable = MasaStackClickhouseConnection.LogTable.Replace(".", ".v_");
        string[] sql = new string[] {
            @$"CREATE TABLE {MasaStackClickhouseConnection.LogTable}
(
    `Timestamp` DateTime64(9) CODEC(Delta(8), ZSTD(1)),
    `TraceId` String CODEC(ZSTD(1)),
    `SpanId` String CODEC(ZSTD(1)),
    `TraceFlags` UInt32 CODEC(ZSTD(1)),
    `SeverityText` LowCardinality(String) CODEC(ZSTD(1)),
    `SeverityNumber` Int32 CODEC(ZSTD(1)),
    `ServiceName` LowCardinality(String) CODEC(ZSTD(1)),
    `Body` String CODEC(ZSTD(1)),
    `ResourceSchemaUrl` String CODEC(ZSTD(1)),
    `Resources` String CODEC(ZSTD(1)),
    `ScopeSchemaUrl` String CODEC(ZSTD(1)),
    `ScopeName` String CODEC(ZSTD(1)),
    `ScopeVersion` String CODEC(ZSTD(1)),
    `Scopes` String CODEC(ZSTD(1)),
    `Logs` String CODEC(ZSTD(1)),
	
	`Resource.service.namespace` String CODEC(ZSTD(1)),	
	`Resource.service.version` String CODEC(ZSTD(1)),	
	`Resource.service.instance.id` String CODEC(ZSTD(1)),	
	
	`Attributes.TaskId`  String CODEC(ZSTD(1)),
    `Attributes.exception.type`  String CODEC(ZSTD(1)),
	`Attributes.exception.message`  String CODEC(ZSTD(1)),   
    `Attributes.http.target`  String CODEC(ZSTD(1)),
    
    ResourceAttributesKeys Array(String) CODEC(ZSTD(1)),
    ResourceAttributesValues Array(String) CODEC(ZSTD(1)),
    LogAttributesKeys Array(String) CODEC(ZSTD(1)),
    LogAttributesValues Array(String) CODEC(ZSTD(1)),    

    INDEX idx_log_id TraceId TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_log_servicename ServiceName TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_log_serviceinstanceid `Resource.service.instance.id` TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_log_severitytext SeverityText TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_log_taskid `Attributes.TaskId` TYPE bloom_filter(0.001) GRANULARITY 1,
    INDEX idx_log_exceptiontype `Attributes.exception.type` TYPE bloom_filter(0.001) GRANULARITY 1,

	INDEX idx_string_body Body TYPE tokenbf_v1(30720, 2, 0) GRANULARITY 1,
	INDEX idx_string_exceptionmessage Attributes.exception.message TYPE tokenbf_v1(30720, 2, 0) GRANULARITY 1
)
ENGINE = MergeTree
PARTITION BY toDate(Timestamp)
ORDER BY (
 Timestamp,
 `Resource.service.namespace`,
 ServiceName
 )
TTL toDateTime(Timestamp) + toIntervalDay(30)
SETTINGS index_granularity = 8192,
 ttl_only_drop_parts = 1;
",
            $@"CREATE MATERIALIZED VIEW {viewTable} TO {MasaStackClickhouseConnection.LogTable}
AS
SELECT
Timestamp,TraceId,SpanId,TraceFlags,SeverityText,SeverityNumber,ServiceName,Body,ResourceSchemaUrl,toJSONString(ResourceAttributes) as Resources,
ScopeSchemaUrl,ScopeName,ScopeVersion,toJSONString(ScopeAttributes) as Scopes,toJSONString(LogAttributes) as Logs,
ResourceAttributes['service.namespace'] as `Resource.service.namespace`,ResourceAttributes['service.version'] as `Resource.service.version`,
ResourceAttributes['service.instance.id'] as `Resource.service.instance.id`,
LogAttributes['TaskId'] as `Attributes.TaskId`,
LogAttributes['exception.type'] as `Attributes.exception.type`,
LogAttributes['exception.message'] as `Attributes.exception.message`,
LogAttributes['RequestPath'] as `Attributes.http.target`,
mapKeys(ResourceAttributes) as ResourceAttributesKeys,mapValues(ResourceAttributes) as ResourceAttributesValues,
mapKeys(LogAttributes) as LogAttributesKeys,mapValues(LogAttributes) as LogAttributesValues
FROM {MasaStackClickhouseConnection.LogSourceTable}
",
        };
        InitTable(MasaStackClickhouseConnection.LogTable, sql[0]);
        InitTable(viewTable, sql[1]);
    }

    private static void InitTrace(string table, string? where = null)
    {
        var viewTable = table.Replace(".", ".v_");
        string[] sql = new string[] {
            @$"CREATE TABLE {table}
(
    `Timestamp` DateTime64(9) CODEC(Delta(8), ZSTD(1)),
    `TraceId` String CODEC(ZSTD(1)),
    `SpanId` String CODEC(ZSTD(1)),
    `ParentSpanId` String CODEC(ZSTD(1)),
    `TraceState` String CODEC(ZSTD(1)),
    `SpanName` LowCardinality(String) CODEC(ZSTD(1)),
    `SpanKind` LowCardinality(String) CODEC(ZSTD(1)),
    `ServiceName` LowCardinality(String) CODEC(ZSTD(1)),
    `Resources` String CODEC(ZSTD(1)),
    `ScopeName` String CODEC(ZSTD(1)),
    `ScopeVersion` String CODEC(ZSTD(1)),
    `Spans` String CODEC(ZSTD(1)),
    `Duration` Int64 CODEC(ZSTD(1)),
    `StatusCode` LowCardinality(String) CODEC(ZSTD(1)),
    `StatusMessage` String CODEC(ZSTD(1)),
    `Events.Timestamp` Array(DateTime64(9)) CODEC(ZSTD(1)),
    `Events.Name` Array(LowCardinality(String)) CODEC(ZSTD(1)),
    `Events.Attributes` Array(Map(LowCardinality(String), String)) CODEC(ZSTD(1)),
    `Links.TraceId` Array(String) CODEC(ZSTD(1)),
    `Links.SpanId` Array(String) CODEC(ZSTD(1)),
    `Links.TraceState` Array(String) CODEC(ZSTD(1)),
    `Links.Attributes` Array(Map(LowCardinality(String), String)) CODEC(ZSTD(1)),
	
	`Resource.service.namespace` String CODEC(ZSTD(1)),	
	`Resource.service.version` String CODEC(ZSTD(1)),	
	`Resource.service.instance.id` String CODEC(ZSTD(1)),
	
	`Attributes.http.status_code` String CODEC(ZSTD(1)),
	`Attributes.http.response_content_body` String CODEC(ZSTD(1)),
	`Attributes.http.request_content_body` String CODEC(ZSTD(1)),
	`Attributes.http.target` String CODEC(ZSTD(1)),
    `Attributes.http.method` String CODEC(ZSTD(1)),
    `Attributes.exception.type` String CODEC(ZSTD(1)),
	`Attributes.exception.message` String CODEC(ZSTD(1)),

    `ResourceAttributesKeys` Array(String) CODEC(ZSTD(1)),
    `ResourceAttributesValues` Array(String) CODEC(ZSTD(1)),
    `SpanAttributesKeys` Array(String) CODEC(ZSTD(1)),
    `SpanAttributesValues` Array(String) CODEC(ZSTD(1)),

    INDEX idx_trace_id TraceId TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_trace_servicename ServiceName TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_trace_servicenamespace Resource.service.namespace TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_trace_serviceinstanceid Resource.service.instance.id TYPE bloom_filter(0.001) GRANULARITY 1,
	INDEX idx_trace_statuscode Attributes.http.status_code TYPE bloom_filter(0.001) GRANULARITY 1,
    INDEX idx_trace_exceptiontype Attributes.exception.type TYPE bloom_filter(0.001) GRANULARITY 1,	
	
	INDEX idx_string_requestbody Attributes.http.request_content_body TYPE tokenbf_v1(30720, 2, 0) GRANULARITY 1,
	INDEX idx_string_responsebody Attributes.http.response_content_body TYPE tokenbf_v1(30720, 2, 0) GRANULARITY 1,
	INDEX idx_string_exceptionmessage Attributes.exception.message TYPE tokenbf_v1(30720, 2, 0) GRANULARITY 1
)
ENGINE = MergeTree
PARTITION BY toDate(Timestamp)
ORDER BY (
 Timestamp,
 Resource.service.namespace,
 ServiceName
 )
--TTL toDateTime(Timestamp) + toIntervalDay(30)
SETTINGS index_granularity = 8192,
 ttl_only_drop_parts = 1;
",
            $@"CREATE MATERIALIZED VIEW {viewTable} TO {table}
AS
SELECT
    Timestamp,TraceId,SpanId,ParentSpanId,TraceState,SpanName,SpanKind,ServiceName,toJSONString(ResourceAttributes) AS Resources,
    ScopeName,ScopeVersion,toJSONString(SpanAttributes) AS Spans,
    Duration,StatusCode,StatusMessage,Events.Timestamp,Events.Name,Events.Attributes,
    Links.TraceId,Links.SpanId,Links.TraceState,Links.Attributes,
    
    ResourceAttributes['service.namespace'] as `Resource.service.namespace`,ResourceAttributes['service.version'] as `Resource.service.version`,
    ResourceAttributes['service.instance.id'] as `Resource.service.instance.id`,
    
    SpanAttributes['http.status_code'] as `Attributes.http.status_code`,
    SpanAttributes['http.response_content_body'] as `Attributes.http.response_content_body`,
    SpanAttributes['http.request_content_body'] as `Attributes.http.request_content_body`,
    SpanAttributes['http.target'] as `Attributes.http.target`,
    SpanAttributes['http.method'] as `Attributes.http.method`,
    SpanAttributes['exception.type'] as `Attributes.exception.type`,   
    SpanAttributes['exception.message'] as `Attributes.exception.message`,   

    mapKeys(ResourceAttributes) AS ResourceAttributesKeys,
    mapValues(ResourceAttributes) AS ResourceAttributesValues,
    mapKeys(SpanAttributes) AS SpanAttributesKeys,
    mapValues(SpanAttributes) AS SpanAttributesValues
FROM {MasaStackClickhouseConnection.TraceSourceTable}
{where}
" };
        InitTable(table, sql[0]);
        InitTable(viewTable, sql[1]);
    }

    private static void InitMappingTable()
    {
        var mappingTable = "otel_mapping_";
        var now= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sql = new string[]{
$@"
CREATE TABLE {MasaStackClickhouseConnection.MappingTable}
(
    Timestamp DateTime64(9) CODEC(Delta(8), ZSTD(1)),
    `Name` String CODEC(ZSTD(1)),
    `Type` String CODEC(ZSTD(1))
)
ENGINE = ReplacingMergeTree(Timestamp)
PRIMARY KEY (Timestamp,`Type`,`Name`)
ORDER BY (Timestamp,`Type`,`Name`)
--TTL toDateTime(Timestamp) + toIntervalDay(30)
SETTINGS index_granularity = 8192;",
@$"CREATE MATERIALIZED VIEW {MasaStackClickhouseConnection.MappingTable.Replace(mappingTable,"v_otel_traces_attribute_mapping")} to {MasaStackClickhouseConnection.MappingTable}
as
select 
 DISTINCT now() as Timestamp, Names as `Name`,'trace_attributes' AS `Type` 
 from
(
SELECT arraySort(mapKeys(SpanAttributes)) AS Names    
FROM {MasaStackClickhouseConnection.TraceSourceTable}
) t
Array join Names",
$@"CREATE MATERIALIZED VIEW  {MasaStackClickhouseConnection.MappingTable.Replace(mappingTable,"v_otel_traces_resource_mapping")} to {MasaStackClickhouseConnection.MappingTable}
as
select 
 DISTINCT now() as Timestamp, Names as `Name`,'trace_resource' AS `Type` 
 from
(
SELECT arraySort(mapKeys(ResourceAttributes)) AS Names    
FROM {MasaStackClickhouseConnection.TraceSourceTable}
) t
Array join Names",
$@"CREATE MATERIALIZED VIEW {MasaStackClickhouseConnection.MappingTable.Replace(mappingTable,"v_otel_logs_attribute_mapping")} to {MasaStackClickhouseConnection.MappingTable}
as
select 
 DISTINCT now() as Timestamp, Names as `Name`,'log_attributes' AS `Type` 
 from
(
SELECT arraySort(mapKeys(LogAttributes)) AS Names    
FROM {MasaStackClickhouseConnection.LogSourceTable}
) t
Array join Names",
$@"CREATE MATERIALIZED VIEW {MasaStackClickhouseConnection.MappingTable.Replace(mappingTable,"v_otel_logs_resource_mapping")} to {MasaStackClickhouseConnection.MappingTable}
as
select 
 DISTINCT now() as Timestamp, Names as `Name`,'log_resource' AS `Type` 
 from
(
SELECT arraySort(mapKeys(ResourceAttributes)) AS Names    
FROM {MasaStackClickhouseConnection.LogSourceTable}
) t
Array join Names",
$@"insert into {MasaStackClickhouseConnection.MappingTable}
values 
('{now}','Timestamp','log_basic'),
('{now}','TraceId','log_basic'),
('{now}','SpanId','log_basic'),
('{now}','TraceFlag','log_basic'),
('{now}','SeverityText','log_basic'),
('{now}','SeverityNumber','log_basic'),
('{now}','Body','log_basic'),

('{now}','Timestamp','trace_basic'),
('{now}','TraceId','trace_basic'),
('{now}','SpanId','trace_basic'),
('{now}','ParentSpanId','trace_basic'),
('{now}','TraceState','trace_basic'),
('{now}','SpanKind','trace_basic'),
('{now}','Duration','trace_basic');
" };
        InitTable(MasaStackClickhouseConnection.MappingTable, sql);
    }

    private static void InitTable(string tableName, params string[] sqls)
    {
        var database = Connection.ConnectionSettings.Database!;
        if (!string.IsNullOrEmpty(database))
            tableName = tableName.Substring(database.Length + 1);

        if (Convert.ToInt32(Connection.ExecuteScalar($"select count() from system.tables where database ='{database}' and name in ['{tableName}']")) > 0)
            return;
        if (sqls == null || sqls.Length == 0)
            return;
        foreach (var sql in sqls)
        {
            ExecuteSql(Connection, sql);
        }
    }

    internal static bool ExistsTable(MasaStackClickhouseConnection connection, string tableName)
    {
        var database = connection.ConnectionSettings.Database!;
        if (!string.IsNullOrEmpty(database))
            tableName = tableName.Substring(database.Length + 1);
        return Convert.ToInt32(connection.ExecuteScalar($"select count() from system.tables where database ='{database}' and name in ['{tableName}']")) > 0;
    }

    public static void InitTable(MasaStackClickhouseConnection connection, string tableName, params string[] sqls)
    {
        if (ExistsTable(connection, tableName))
            return;
        if (sqls == null || sqls.Length == 0)
            return;
        foreach (var sql in sqls)
        {
            ExecuteSql(connection, sql);
        }
    }

    internal static void ExecuteSql(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        if (connection.State != ConnectionState.Open)
            connection.Open();
        cmd.CommandText = sql;
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Init table sql error:{RawSql}", sql);
        }
    }

    private static string GetTimezone(MasaStackClickhouseConnection connection)
    {
        using var cmd = connection.CreateCommand();
        if (connection.State != ConnectionState.Open)
            connection.Open();
        var sql = "select timezone()";
        cmd.CommandText = sql;
        try
        {
            return cmd.ExecuteScalar()?.ToString()!;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "ExecuteSql {RawSql} error", sql);
            throw;
        }
    }
}
