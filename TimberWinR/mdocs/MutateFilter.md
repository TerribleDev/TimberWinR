# Mutate Filter
The mutate filter allows you to perform general mutations on fields. You can rename, remove, replace and modify fields in your events.  This filter will automatically be applied to all inputs before sending to the outputs.   If you want to make a
filter conditional, use the ***condition*** property to specify a legal C# expression.

## Mutate Operations
The following operations are allowed when mutating a field.

| Operation   |     Type        | Description                                                            
| :-----------|:----------------|:-----------------------------------------------------------------------|
| *condition* | property:string |C# Expression
| *remove*    | property:array  |Remove one or more fields                                       
| *rename*    | property:array  |Rename one or more fields                                       
| *replace*   | property:array  |Replace a field with a new value.  The new value can include %{foo} strings to help you build a new value from other parts of the event.                                   
| *split*     | property:array  |Separator between values of the "Strings" field.   

## Details
### condition "C# expression"
If present, the condition must evaluate to true in order for the remaining operations to be performed.  If there is no condition specified
then the operation(s) will be executed in order.
```json
  "Filters": [     
	{
		"mutate": {      			
			"condition": "\"[type]\" == \"Win32-EventLog\"",
			"rename": [
				"ComputerName", "Host"				              
			]
		}                
	}     
  ]
```
The above example will rename ComputerName to Host only for Win32-EventLog types.

### remove ["name", ...]
Removes field.
```json
  "Filters": [     
    {
		"mutate": {      			
			"remove": [
				"ComputerName", "Username"
			]
		}                
    }     
  ]
```
### rename ["oldname", "newname", ...]
The fields must be in pairs with oldname first and newname second.
```json
  "Filters": [     
    {
		"mutate": {      			
			"rename": [
				"ComputerName", "Host",
				"host", "Host",
				"message","Message",
				"type","Type",
				"SID", "Username"                 
			]
		}                
    }     
  ]
```
### replace ["field", "newvalue", ...]
Replaces field with newvalue.   The replacements must be described in pairs.
```json
  "Filters": [     
    {
		"mutate": {      			
			"replace": [
				"message", "%{source_host}: My new message"
			]
		}                
    }     
  ]
```
### split                      
Split a field into an array of values.   The first arguments is the fieldName and the second is the separator.
```json
  "Filters": [     
    {
		"mutate": {      			
			"split": [
				"InsertionStrings", "|"
			]
		}                
    }     
  ]
```


