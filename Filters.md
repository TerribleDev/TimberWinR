# Filters
The following filters are provided.


| Filter            | Description                                                             
| :---------------- |:----------------------------------------------------------------------- 
| *[grok][4]*            |Similar to the [logstash grok][1] filter                                                  
| *[date][5]*             |Similar to the [logstash date][2] filter                                   
| *[mutate][6]*          |Similar to the [logstash mutate][3] filter                          
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
  [2]: http://logstash.net/docs/1.4.2/filters/date
  [3]: http://logstash.net/docs/1.4.2/filters/mutate
  [4]: https://github.com/efontana/TimberWinR/blob/master/mdocs/GrokFilter.md
  [5]: https://github.com/efontana/TimberWinR/blob/master/mdocs/DateFilter.md
  [6]: https://github.com/efontana/TimberWinR/blob/master/mdocs/MutateFilter.md