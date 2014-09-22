# Input: W3CLogs

The W3C input format parses IIS log files in the W3C Extended Log File Format, and handles custom fields unlike the IISW3C input.

IIS web sites logging in the W3C Extended format can be configured to log only a specific subset of the available fields.
Log files in this format begin with some informative headers ("directives"), the most important of which is the "#Fields" directive, describing which fields are logged at which position in a log row.
After the directives, the log entries follow. Each log entry is a space-separated list of field values.

If the logging configuration of an IIS virtual site is updated, the structure of the fields in the file that is currently logged to might change according to the new configuration. In this case, a new "#Fields" directive is logged describing the new fields structure, and the IISW3C input format keeps track of the structure change and parses the new log entries accordingly. 



## Parameters
The following parameters are allowed when configuring IISW3CLogs input.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *location*        | string  |Location of log files(s) to monitor                                     | Path to text file(s) including wildcards, may be separated by commas |     |
| *iCodepage*       | integer |Codepage of the text file.                                              | 0 is the system codepage, -1 is UNICODE.                         | 0  |
| *dtLines*  | integer |Number of lines examined to determine field types at run time. | This parameter specifies the number of initial log lines that the W3C input format examines to determine the data type of the input record fields. If the value is zero, all fields will be assumed to be of the STRING data type. | false |
| *dQuotes*       | boolean |Specifies that string values in the log are double-quoted.                | Log processors might generate W3C logs whose string values are enclosed in double-quotes.                      | false |
| *separator*       | string |Use the value of the "#Date" directive for the "date" and/or "time" field values when these fields are not logged.               | When a log file is configured to not log the "date" and/or "time" fields, specifying "true" for this parameters causes the IISW3C input format to generate "date" and "time" values using the value of the last seen "#Date" directive.  | false |

Example Input:
```json
{
    "TimberWinR": {
        "Inputs": {
            "W3CLogs": [
                {                   
                   "location": "C:\\inetpub\\logs\\LogFiles\\W3SVC1\\*"
                }
            ]
		}
	}
}
```


## Fields
After a successful parse of an event, the following fields are added [(if configured to be logged)](http://technet.microsoft.com/en-us/library/cc754702(v=ws.10).aspx)

| Name | Type | Description |
| ---- |:-----| :-----------------------------------------------------------------------|
|LogFilename| STRING | Full path of the log file containing this entry |
|LogRow | INTEGER |  Line in the log file containing this entry  |

Custom fields to follow..
