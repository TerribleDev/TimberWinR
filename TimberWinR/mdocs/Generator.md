# Input: Generator

The Generator input can be used to Generate log files for test purposes.

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| *type*            | string  |Message type                                |  |  Win32-InputGen   |
| *message*         | string  |Message format to send                                  |  |  Hello, World!   |
| *count*           | integer |Number of messages to generate                                       | 0 - Infinite, otherwise that number | 0 |
| *rate*            | integer |Sleep time between generated messages                                    | Milliseconds | 10 |
| [codec](https://github.com/Cimpress-MCP/TimberWinR/blob/master/TimberWinR/mdocs/Codec.md)  | object | Codec to use  |

Example: Generate 100000 "Hello Win32-InputGen" messages

```json
{
    "TimberWinR": {
        "Inputs": {
            "Generator": [
                {
                   "message":  "Hello %{type}",
                   "count": 100000
                }
            ]
		}
	}
}
```
## Fields
After a successful parse of an event, the following fields are added:

| Name | Type | Description |
| ---- |:-----| :-----------|
| Message | STRING | Text line content  |
