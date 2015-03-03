TimberWinR Release Notes
==================================
A Native Windows to Redis/Elasticsearch Logstash Agent which runs as a service.

Version History
### 1.3.20.0 - 03/03/2015
1. Added new Redis parameter _max\_batch\_count_ which increases the _batch\_count_ dynamically over time 
   to handle input flooding.   Default is _batch\_count_ * 10 

### 1.3.19.0 - 02/26/2015

1. Added support for Multiline codecs for Stdin and Logs listeners, closes issue [#23](https://github.com/Cimpress-MCP/TimberWinR/issues/23)
2. Added new TailFiles input type which uses a native implementation (more-efficient) than using LogParser's Log
3. Updated Udp input listner to use UTF8 Encoding rather than ASCII
4. Reduced noisy complaint about missing log files for Logs listener
5. Fixed bug when tailing non-existent log files which resulted in high cpu-usage.
6. Added feature to watch the configuration directory

### 1.3.18.0 - 12/22/2014

1. Fixed bug introduced in 1.3.17.0 which changed the meaning of the delay for Elasticsearch, Redis and Stdout 
intervals to be interpreted as seconds instead of milliseconds.   1.3.17.0 should not be used.
2. Removed ability for installer to downgrade which was leading to leaving previous versions laying around (i.e. reverts 1.3.13.0 change)

### 1.3.17.0 - 12/19/2014

1. Continued work improving shutdown time by using syncHandle.Wait instead of Thread.Sleep

### 1.3.16.0 - 12/19/2014

1. Added logSource property to the Log input to facility the steering of log messages to different indices.

### 1.3.15.0 - 12/12/2014

1. Fixed bug whereby if the Udp or Tcp inputs receive an impropery formatted Json it caused the thread to terminate, and ignore
future messages.

### 1.3.14.0 - 12/11/2014

1. Fixed bug with the Grok filter to match properly the value of the Text field against non-blank entries.

### 1.3.13.0 - 12/02/2014

1. Fixed MSI installer to allow downgrades.

### 1.3.12.0 - 11/25/2014

1. Fixed all remaining memory leaks due to the COM Weak Surrogate which requires an explicit GC.Collect

### 1.3.11.0 - 11/21/2014

1. Re-worked WindowsEvent listener to enable shutting down in a quicker fashion.

### 1.3.10.0 - 11/18/2014

1. Refactored Conditions handler to use non-leaking evaluator.

### 1.3.9.0 - 11/11/2014

1. Merged in pull request #9
2. Updated chocolately uninstall to preserve GUID

### 1.3.8.0 - 11/06/2014
1. Added interval parameter to WindowsEvent input listener
2. Increased default value for interval to 60 seconds for polling WindowsEvents

### 1.3.7.0 - 10/21/2014
1. Added additional information for diagnostics port
2. Completed minor handling of Log rolling detection

### 1.3.6.0 - 10/16/2014
1. Handle rolling of logs whereby the logfile remains the same, but the content resets back to 0 bytes.




