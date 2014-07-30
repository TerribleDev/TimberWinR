# Output: Redis

The Redis output passes on data to Redis to be consumed by the Logtash indexer.

## Parameters
The following parameters are allowed when configuring the Redis output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *threads*     | string   | Location of log files(s) to monitor                         | Number of worker theads to send messages | 1 |
| *interval*    | integer  | Interval in milliseconds to sleep during batch sends        | Interval      | 5000 |
| *batch_count* | integer  | The number of events to send in a single transaction        |  | 10 |
| *index*       | string   | The name of the redis list                                  | logstash index name | logstash |
| *host*        | [string] | The hostname(s) of your Redis server(s) | IP or DNS name |  |
| *port*        | integer  | Redis port number                                           | This port must be open  | 6379  |

Example Input:
```json
{
    "TimberWinR": {
        "Outputs": {
            "Redis": [
               { 
                    "threads":  1,   
                    "interval": 5000, 
                    "batch_count":  500,              
                    "host": [
                        "tstlexiceapp006.vistaprint.svc"
                    ]
                }
            ]
		}
	}
}
```