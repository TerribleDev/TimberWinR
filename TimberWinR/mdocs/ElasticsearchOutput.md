# Output: Elasticsearch

The Elasticsearch output passes on data directly to Elasticsearch.

## Parameters
The following parameters are allowed when configuring the Redis output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *flush_size*                    | integer  | Maximum number of messages before flushing           |   | 50000  |
| *host*                          | [string] | Array of hostname(s) of your Elasticsearch server(s) | IP or DNS name |  |
| *idle_flush_time*               | integer  | Maximum number of seconds elapsed before triggering a flush           |   | 10  |
| *index*                         | [string]    | The index name to use                                       | index used/created | logstash-yyyy.dd.mm |
| *interval*                      | integer  | Interval in milliseconds to sleep during batch sends        | Interval       | 5000 |
| *max_queue_size*                | integer  | Maximum Elasticsearch queue depth       |  | 5000 |
| *port*                          | integer  | Elasticsearch port number                                   | This port must be open  | 9200  |
| *queue_overflow_discard_oldest* | bool  | If true, discard oldest messages when max_queue_size reached otherwise discard newest |  | true |
| *threads*                       | [string]    | Number of Threads                         | Number of worker threads processing messages | 1 |

### Index parameter
If you want to output your data everyday to a new index, use following index format: "index-%{yyyy.MM.dd}". Here date format could be any forwat which you need.

Example Input:
```json
{
    "TimberWinR": {
        "Outputs": {
            "Elasticsearch": [
               { 
                    "threads":  1,   
                    "interval": 5000,                             
                    "host": [
                        "tstlexiceapp006.mycompany.svc"
                    ]
                }
            ]
		}
	}
}
```