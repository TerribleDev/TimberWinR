# Filters
The following filters are provided.


| Filter            | Description                                                             
| :---------------- |:----------------------------------------------------------------------- 
| *[grok][1]*            |Similar to the logstash grok filter                                                  
| *date*            |Format of the "Data" binary field.                                       
| *mutate*          |Behavior when event messages or event category names cannot be resolved.                             
Example Input:
```json
{
    "TimberWinR": {
        "Inputs": {
            "WindowsEvents": [
                {
                    "source": "System,Application",
                    "binaryFormat": "PRINT",
                    "resolveSIDS": true
                }
            ]
		}
	}
}
```


  [1]: http://logstash.net/docs/1.4.2/filters/grok