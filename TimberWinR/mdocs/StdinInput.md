# Input: Stdin

The Stdin Input will read from the console (Console.ReadLine) and build a simple message for testing.

## Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter         |     Type       |  Description                                                             | Details               |  Default |
| :---------------- |:---------------| :----------------------------------------------------------------------- | :---------------------------  | :-- |
| [codec](https://github.com/Cimpress-MCP/TimberWinR/blob/master/TimberWinR/mdocs/Codec.md)  | object | Codec to use  |


```json
{
    "TimberWinR": {
        "Inputs": {
            "Stdin": [
                {
                    "_comment": "Read from Console"                  
                }
            ]
		}
	}
}
```
## Fields

A field: "type": "Win32-Stdin" is automatically appended, and the entire JSON is passed on vertabim

| Name | Type | Description |
| ---- |:-----| :-----------------------------------------------------------------------|
| type | STRING |Win32-Stdin |
| message | STRING | The message typed in  |
