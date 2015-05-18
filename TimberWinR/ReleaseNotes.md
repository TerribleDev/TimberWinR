TimberWinR Release Notes
==================================
A Native Windows to Redis/Elasticsearch Logstash Agent which runs as a service.

Version / Date
### 1.3.26.0 - 2015-05-15
1. Added StatsD outputter
2. Fixed shutdown hang if shutdown was received before service was fully started up.
3. Closed issue [#36](https://github.com/Cimpress-MCP/TimberWinR/issues/36)

### 1.3.25.0 - 2015-04-30
1. Fixed Issue [#49](https://github.com/Cimpress-MCP/TimberWinR/issues/49)
2. Fixed potential non-thread safe when renaming properties
3. Added add_field, rename support to Udp/Tcp Input Listeners
4. Fixed issue with multiple renames (was previously only renaming the first one)
5. Added File outputter for testing.

### 1.3.24.0 - 2015-04-29
1. Fixed potential bug in TailFiles when tailing log files which are partially flushed
   to disk, it now will not process the line until the \r\n has been seen.
2. Added Generator input.

### 1.3.23.0 - 2015-04-23
1. Fixed bug with parsing a single json config file, rather than reading
   JSON files from a directory.
2. Diabled elasticsearch outputter ping by default and parameterized the ping capability.

### 1.3.22.0 - 2015-04-14
1. Fixed minor bug with TailFiles and service re-starts not picking up
   rolled files right away.

### 1.3.21.0 - 2015-04-13
1. Rolled Udp listener support to V4 only, too many issues with dual mode sockets
   and hosts file.  If we want to add this back, I will add a Udpv6 input.

### 1.3.20.0 - 2015-04-03

1. A re-factoring of Logs and TailLogs to be more efficient and detect log rolling correctly,
   this requires http://support.microsoft.com/en-us/kb/172190 which will be detected and
   set by TimberWinR, however, requires a reboot.
2. Fixed issue [#38](https://github.com/Cimpress-MCP/TimberWinR/issues/38) diagnostic output not showing drop flag for Grok filter.
3. Created TimberWinR.TestGenerator for complete testing of TimberWinR
4. Fixed ipv4/ipv6 thread-safe issue with UdpInputListener which might lead to corrupted input data.

### 1.3.19.1 - 2015-03-03

1. Added new Redis parameter _max\_batch\_count_ which increases the _batch\_count_ dynamically over time 
   to handle input flooding.   Default is _batch\_count_ * 10 

### 1.3.19.0 - 2015-02-26

1. Added support for Multiline codecs for Stdin and Logs listeners, closes issue [#23](https://github.com/Cimpress-MCP/TimberWinR/issues/23)
2. Added new TailFiles input type which uses a native implementation (more-efficient) than using LogParser's Log
3. Updated Udp input listner to use UTF8 Encoding rather than ASCII
4. Reduced noisy complaint about missing log files for Logs listener
5. Fixed bug when tailing non-existent log files which resulted in high cpu-usage.
6. Added feature to watch the configuration directory

### 1.3.18.0 - 2014-12-22

1. Fixed bug introduced in 1.3.17.0 which changed the meaning of the delay for Elasticsearch, Redis and Stdout 
intervals to be interpreted as seconds instead of milliseconds.   1.3.17.0 should not be used.
2. Removed ability for installer to downgrade which was leading to leaving previous versions laying around (i.e. reverts 1.3.13.0 change)

### 1.3.17.0 - 2014-12-19

1. Continued work improving shutdown time by using syncHandle.Wait instead of Thread.Sleep

### 1.3.16.0 - 2014-12-19

1. Added logSource property to the Log input to facility the steering of log messages to different indices.

### 1.3.15.0 - 2014-12-12

1. Fixed bug whereby if the Udp or Tcp inputs receive an impropery formatted Json it caused the thread to terminate, and ignore
future messages.

### 1.3.14.0 - 2014-12-11

1. Fixed bug with the Grok filter to match properly the value of the Text field against non-blank entries.

### 1.3.13.0 - 2014-12-02

1. Fixed MSI installer to allow downgrades.

### 1.3.12.0 - 2014-11-25

1. Fixed all remaining memory leaks due to the COM Weak Surrogate which requires an explicit GC.Collect

### 1.3.11.0 - 2014-11-21

1. Re-worked WindowsEvent listener to enable shutting down in a quicker fashion.

### 1.3.10.0 - 2014-11-18

1. Refactored Conditions handler to use non-leaking evaluator.

### 1.3.9.0 - 2014-11-11

1. Merged in pull request #9
2. Updated chocolately uninstall to preserve GUID

### 1.3.8.0 - 2014-11-06

1. Added interval parameter to WindowsEvent input listener
2. Increased default value for interval to 60 seconds for polling WindowsEvents

### 1.3.7.0 - 2014-10-21

1. Added additional information for diagnostics port
2. Completed minor handling of Log rolling detection

### 1.3.6.0 - 2014-10-16

1. Handle rolling of logs whereby the logfile remains the same, but the content resets back to 0 bytes.
