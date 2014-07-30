# Filters
The following filters are provided.


| Filter            | Description                                                             
| :---------------- |:----------------------------------------------------------------------- 
| *[grok][4]*       |Similar to the [logstash grok][1] filter                                                  
| *[date][5]*       |Similar to the [logstash date][2] filter                                   
| *[mutate][6]*     |Similar to the [logstash mutate][3] filter                          
Example Input:
```json
 "Filters": [          
    {
        "grok": {
            "condition": "[type] == \"Win32-Eventlog\"",
            "match": [
                "Message",
                ""
            ],                   
            "remove_field": [
                "ComputerName"                   
            ]
        }
    },
    {
        "grok": {
            "match": [
                "message",
                "%{SYSLOGLINE}"
            ],           
            "add_field": [               
                "Hello", "from %{logsource}"
            ]
        }
    },
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
    },
    {
        "mutate": {      
            "_comment": "Custom Rules",        
            "rename": [
                "ComputerName", "Host",
                "host", "Host",
                "message","Message",
                "type","Type",
                "SID", "Username"                 
            ]
        }                
    }           
]
```
  [1]: http://logstash.net/docs/1.4.2/filters/grok
  [2]: http://logstash.net/docs/1.4.2/filters/date
  [3]: http://logstash.net/docs/1.4.2/filters/mutate
  [4]: https://github.com/efontana/TimberWinR/blob/master/mdocs/GrokFilter.md
  [5]: https://github.com/efontana/TimberWinR/blob/master/mdocs/DateFilter.md
  [6]: https://github.com/efontana/TimberWinR/blob/master/mdocs/MutateFilter.md