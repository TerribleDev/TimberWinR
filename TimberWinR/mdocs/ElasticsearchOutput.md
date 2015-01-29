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
| *port*        | integer  | Redis port number                                           | This port must be open  | 9200  |

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