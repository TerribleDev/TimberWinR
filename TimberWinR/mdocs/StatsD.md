# Output: StatsD

The StatsD output passes on data directly to StatsD. (https://github.com/etsy/statsd)

## Parameters
The following parameters are allowed when configuring the StatsD output.

| Parameter     |   Type   |  Description                                                | Details               |  Default |
| :-------------|:---------|:------------------------------------------------------------| :---------------------------  | :-- |
| *count*                         | string | Array of (metric_name, gauge name) pairs counted | Must come in pairs  |  |
| *decrement*                     | string | Array of metrics to be decremented |  |  |
| *flush_size*                    | integer  | Maximum number of messages before flushing           |   | 50000  |
| *gauge*                         | string | Array of (metric_name, gauge name) pairs gauged | Must come in pairs  |  |
| *host*                          | string | Hostname or IP of StatsD server | localhost |  |
| *idle_flush_time*               | integer  | Maximum number of seconds elapsed before triggering a flush           |   | 10  |
| *increment*                     | string | Array of metrics to be incremented |  |  |
| *interval*                      | integer  | Interval in milliseconds to sleep between sends     | Interval       | 5000 |
| *max_queue_size*                | integer  | Maximum StatsD queue depth       |  | 50000 |
| *namespace*                     | string | Namespace for stats | timberwinr |  |
| *port*                          | integer  | StatsD port number                                   | This port must be open  | 8125  |
| *queue_overflow_discard_oldest* | bool  | If true, discard oldest messages when max_queue_size reached otherwise discard newest |  | true |
| *sample_rate*                   | integer  | StatsD sample rate           |   | 1  |
| *sender*                        | string | Sender name | FQDN |  |
| *threads*                       | string    | Number of Threads processing messages          | | 1 |
| *timing*                        | string | Array of (metric_name, timing_name) pairs timed | Must come in pairs  |  |
| *type*                          | string |Type to which this filter applies, if empty, applies to all types.

### Example Usage
Example Input: Tail an apache log file, and record counts for bytes and increments for response codes.

sample-apache.log (snip)
```
180.76.5.25 - - [13/May/2015:17:02:26 -0700] "GET /frameset.htm HTTP/1.1" 404 89 "-" "Mozilla/5.0 (compatible; Baiduspider/2.0; +http://www.baidu.com/search/spider.html)" "www.redlug.com"
208.115.113.94 - - [13/May/2015:17:03:55 -0700] "GET /robots.txt HTTP/1.1" 200 37 "-" "Mozilla/5.0 (compatible; DotBot/1.1; http://www.opensiteexplorer.org/dotbot, help@moz.com)" "redlug.com"
208.115.113.94 - - [13/May/2015:17:03:55 -0700] "GET /robots.txt HTTP/1.1" 200 37 "-" "Mozilla/5.0 (compatible; DotBot/1.1; http://www.opensiteexplorer.org/dotbot, help@moz.com)" "www.redlug.com"
```

Note: [COMBINEDAPACHELOG](https://github.com/elastic/logstash/blob/v1.4.2/patterns/grok-patterns) is a standard
Grok Pattern.

TimberWinR configuration

```json
{
    "TimberWinR": {
        "Inputs": {
            "TailFiles": [
                {
                    "interval": 5,
                    "logSource": "apache log files",
                    "location": "..\\sample-apache.log",
                    "recurse": -1
                }
            ]
        },
        "Filters": [
            {
                "grok": {
                    "type": "Win32-TailLog",
                    "match": [
                        "Text",
                        "%{COMBINEDAPACHELOG}"
                    ]
                }
            }
        ],
        "Outputs": {            
            "StatsD": [
                {
                    "type": "Win32-TailLog",
                    "port": 8125,
                    "host": "stats.mycompany.svc",
                    "increment":  ["apache.response.%{response}"],
                    "count":  ["apache.bytes", "%{bytes}"]
                }
            ]
        }
    }
}

```

Assuming your FQDN is something like mymachine.mycompany.com, you should see the following in Graphite:

```
stats.counters.timberwinr.mymachine.mycompany.com.apache.bytes.count
stats.counters.timberwinr.mymachine.mycompany.com.apache.bytes.rate
stats.counters.timberwinr.mymachine.mycompany.com.apache.response.200.count
stats.counters.timberwinr.mymachine.mycompany.com.apache.response.200.rate
stats.counters.timberwinr.mymachine.mycompany.com.apache.response.404.count
stats.counters.timberwinr.mymachine.mycompany.com.apache.response.404.rate
...
...
```