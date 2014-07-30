# Input: IISW3CLogs

The IISW3C input format parses IIS log files in the W3C Extended Log File Format. 

IIS web sites logging in the W3C Extended format can be configured to log only a specific subset of the available fields.
Log files in this format begin with some informative headers ("directives"), the most important of which is the "#Fields" directive, describing which fields are logged at which position in a log row.
After the directives, the log entries follow. Each log entry is a space-separated list of field values.

If the logging configuration of an IIS virtual site is updated, the structure of the fields in the file that is currently logged to might change according to the new configuration. In this case, a new "#Fields" directive is logged describing the new fields structure, and the IISW3C input format keeps track of the structure change and parses the new log entries accordingly. 



## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *iCodepage*       | property:integer |Codepage of the text file.                                              | 0 is the system codepage, -1 is UNICODE.                         | 0  |
| *recurse*         | property:integer |Max subdirectory recursion level.                                       | 0 disables subdirectory recursion; -1 enables unlimited recursion. | 0 |
| *minDateMod*      | property:datetime |Minimum file last modified date, in local time coordinates             | When this parameter is specified, the IISW3C input format processes only log files that have been modified after the specified date.  |  |
| *dQuotes*       | property:boolean |Specifies that string values in the log are double-quoted.                | Log processors might generate W3C logs whose string values are enclosed in double-quotes.                      | false |
| *dirTime*       | property:boolean |Use the value of the "#Date" directive for the "date" and/or "time" field values when these fields are not logged.               | When a log file is configured to not log the "date" and/or "time" fields, specifying "true" for this parameters causes the IISW3C input format to generate "date" and "time" values using the value of the last seen "#Date" directive.  | false |
| *consolidateLogs*  | property:boolean |Return entries from all the input log files ordering by date and time. | When a location refers to log files from multiple IIS virtual sites, specifying true for this parameter causes the IISW3C input format to parse all the input log files in parallel, returning entries ordered by the values of the "date" and "time" fields in the log files; the input records returned will thus appear as if a single IISW3C log file was being parsed. Enabling this feature is equivalent to executing a query with an "ORDER BY date, time" clause on all the log files. However, the implementation of this feature leverages the pre-existing chronological order of entries in each log file, and it does not require the extensive memory resources otherwise required by the ORDER BY query clause.   | false |

Example Input:
```json
{
    "TimberWinR": {
        "Inputs": {
            "IISW3CLogs": [
                {                   
                   "location": "C:\\inetpub\\logs\\LogFiles\\W3SVC1\\*"
                }
            ]
		}
	}
}
```


## Fields
After a successful parse of an event, the following fields are added:
| Name | Type | Description |
| ---- |:-----| :-----------------------------------------------------------------------|
|LogFilename| STRING | Full path of the log file containing this entry |
|LogRow | INTEGER |  Line in the log file containing this entry  |
|date | TIMESTAMP |  The date on which the request was served (Universal Time Coordinates (UTC) time)  |
|time | TIMESTAMP |  The time at which the request was served (Universal Time Coordinates (UTC) time)  |
|c-ip| STRING | The IP address of the client that made the request  |
|cs-username| STRING | The name of the authenticated user that made the request, or NULL if the request was from an anonymous user  |
|s-sitename| STRING | The IIS service name and site instance number that served the request  |
|s-computername| STRING | The name of the server that served the request  |
|s-ip| STRING | The IP address of the server that served the request  |
|s-port | INTEGER |  The server port number that received the request  |
|cs-method| STRING | The HTTP request verb or FTP operation  |
|cs-uri-stem| STRING | The HTTP request uri-stem or FTP operation target  |
|cs-uri-query| STRING | The HTTP request uri-query, or NULL if the requested URI did not include a uri-query  |
|sc-status | INTEGER |  The response HTTP or FTP status code  |
|sc-substatus | INTEGER |  The response HTTP sub-status code (this field is logged by IIS version 6.0 and later only)  |
|sc-win32-status | INTEGER |  The Windows status code associated with the response HTTP or FTP status code  |
|sc-bytes | INTEGER |  The number of bytes in the response sent by the server  |
|cs-bytes | INTEGER |  The number of bytes in the request sent by the client  |
|time-taken | INTEGER |  The number of milliseconds elapsed since the moment the server received the request to the moment the server sent the last response chunk to the client  |
|cs-version| STRING | The HTTP version of the client request  |
|cs-host| STRING | The client request Host header  |
|cs(User-Agent)| STRING | The client request User-Agent header  |
|cs(Cookie)| STRING | The client request Cookie header  |
|cs(Referer)| STRING | The client request Referer header  |
|s-event| STRING | The type of log event (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-process-type| STRING | The type of process that triggered the log event (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-user-time | REAL | The total accumulated User Mode processor time, in percentage, that the site used during the current interval (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-kernel-time | REAL | The total accumulated Kernel Mode processor time, in percentage, that the site used during the current interval (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-page-faults | INTEGER |  The total number of memory references that resulted in memory page faults during the current interval (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-total-procs | INTEGER |  The total number of applications created during the current interval (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-active-procs | INTEGER |  The total number of applications running when the log event was triggered (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |
|s-stopped-procs | INTEGER |  The total number of applications stopped due to process throttling during the current interval (this field is logged by IIS version 5.0 only when the "Process Accounting Logging" feature is enabled)  |

