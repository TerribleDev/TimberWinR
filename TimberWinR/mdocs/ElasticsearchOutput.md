# Output: Elasticsearch

The Elasticsearch output passes on data directly to Elasticsearch.

## Parameters
The following parameters are allowed when configuring the Redis output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *threads*     | string   | Location of log files(s) to monitor                         | Number of worker theads to send messages | 1 |
| *interval*    | integer  | Interval in milliseconds to sleep during batch sends        | Interval       | 5000 |
| *index*       | string   | The index name to use                                       | index used/created | logstash-yyyy.dd.mm |
| *host*        | [string] | The hostname(s) of your Elasticsearch server(s) | IP or DNS name |  |
| *port*        | integer  | Redis port number                                           | This port must be open  | 6379  |

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
                        "tstlexiceapp006.vistaprint.svc"
                    ]
                }
            ]
		}
	}
}
```