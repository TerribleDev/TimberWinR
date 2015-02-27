# Output: Redis

The Redis output passes on data to Redis to be consumed by the Logtash indexer.

## Parameters
The following parameters are allowed when configuring the Redis output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *threads*     | string   | Location of log files(s) to monitor                         | Number of worker theads to send messages | 1 |
| *batch_count* | integer  | Sent as a single message                         | Number of messages to aggregate | 10 |
| *interval*    | integer  | Interval in milliseconds to sleep during batch sends        | Interval      | 5000 |
| *index*       | string   | The name of the redis list                                  | logstash index name | logstash |
| *host*        | [string] | The hostname(s) of your Redis server(s) | IP or DNS name |  |
| *port*        | integer  | Redis port number                                           | This port must be open  | 6379  |
| *max_queue_size* | integer  | Maximum redis queue depth       |  | 50000 |
| *queue_overflow_discard_oldest* | bool  | If true, discard oldest messages when max_queue_size reached otherwise discard newest |  | true |

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
                        "tstlexiceapp006.mycompany.svc"
                    ]
                }
            ]
		}
	}
}
```