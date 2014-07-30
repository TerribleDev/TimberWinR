# Date Filter
The date filter is used for parsing dates from fields, and then using that date or timestamp as the logstash timestamp for the event.
For example, syslog events usually have timestamps like this:

```
"Apr 17 09:32:01"
```
You would use the date format "MMM dd HH:mm:ss" to parse this.

The date filter is especially important for sorting events and for backfilling old data. If you don't 
get the date correct in your event, then searching for them later will likely sort out of order.

In the absence of this filter, TimberWinR will choose a timestamp based on the first time it sees 
the event (at input time), if the timestamp is not already set in the event. For example, with 
file input, the timestamp is set to the time of each read.

## Date Parameters
The following parameters and operations are allowed when using the Date filter.

| Operation       |     Type        | Description    | Default                                                        
| :---------------|:----------------|:-----------------------------------------------------------------------|
| *add_field*       | array  |If the filter is successful, add an arbitrary field to this event.  Tag names can be dynamic and include parts of the event using the %{field} syntax.  |  |
| *condition*     | string |C# expression | |
| *convertToUTC*  | boolean  |Converts time to UTC | false |
| *match*         | [string] |Required field and pattern must match before any subsequent date operations are executed. | |
| *locale*     | string  |  Specify a locale to be used for date parsing  | en-US |
| *target*     | string  |  Store the matching timestamp into the given target field. If not provided, default to updating the @timestamp field of the event.  | @timestamp |

## Parameter Details
### match 
The date formats allowed are anything allowed by [C# DateTime Format](http://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx). You can see the docs for this format here:
Given this configuration
```json
  "Filters": [     
    {
	   "date":  {
          "condition": "[type] == \"Win32-FileLog\"",
          "match": [
              "timestamp",
              "MMM  d HH:mm:sss",
              "MMM dd HH:mm:ss"                       
          ],
          "add_field":  [
              "UtcTimestamp"
          ],                
          "convertToUTC":  true
      }
    }     
  ]
```

### condition "C# expression"
If present, the condition must evaluate to true in order for the remaining operations to be performed.  If there is no condition specified
then the operation(s) will be executed in order.
```json
  "Filters": [     
    {
		"grok": {      			
		    "condition": "[type] == \"Win32-EventLog\""
			"add_field": [
				"ComputerName", "%{Host}"				              
			]
		}                
    }     
  ]
```
The above example will add a field ComputerName set to the value of Host only for Win32-EventLog types.

### add_field ["fieldName", "fieldValue", ...]
The fields must be in pairs with fieldName first and value second.
```json
  "Filters": [     
    {
	   "date":  {
          "condition": "[type] == \"Win32-FileLog\"",
          "match": [
              "timestamp",
              "MMM  d HH:mm:sss",
              "MMM dd HH:mm:ss"                       
          ],
          "add_field":  [
              "UtcTimestamp"
          ]
      }
    }     
  ]
```

### convertToUTC "true|false"
If true and the filter matches, the time parsed will be converted to UTC
```json
  "Filters": [     
    {
	   "date":  {
          "condition": "[type] == \"Win32-FileLog\"",
          "match": [
              "timestamp",
              "MMM  d HH:mm:sss",
              "MMM dd HH:mm:ss"                       
          ],
          "add_field":  [
              "UtcTimestamp"
          ],                
          "convertToUTC":  true
      }
    }     
  ]
```
