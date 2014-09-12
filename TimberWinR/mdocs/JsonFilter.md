# Json Filter
The Json filter allows you to parse a single line of Json into its corresponding fields.  This is
particularly useful when parsing log files.

## Json Operations
The following operations are allowed when mutating a field.

| Operation       |     Type        | Description                                                            
| :---------------|:----------------|:-----------------------------------------------------------------------|
| *type*          | property:string |Type to which this filter applies, if empty, applies to all types.
| *condition*     | property:string |C# expression, if the expression is true, continue, otherwise, ignore
| *source*        | property:string |Required field indicates which field contains the Json to be parsed
| *target*        | property:string |If suppled, the parsed json will be contained underneath a propery named *target*
| *add_field*     | property:array  |If the filter is successful, add an arbitrary field to this event.  Field names can be dynamic and include parts of the event using the %{field} syntax.  This property must be specified in pairs.                                    
| *remove_field*  | property:array  |If the filter is successful, remove arbitrary fields from this event.  Field names can be dynamic and include parts of the event using the %{field} syntax.                                
| *add_tag*       | property:array  |If the filter is successful, add an arbitrary tag to this event.  Tag names can be dynamic and include parts of the event using the %{field} syntax.                                  
| *remove_tag*    | property:array  |If the filter is successful, remove arbitrary tags from this event.  Field names can be dynamic and include parts of the event using the %{field} syntax.                          

## Operation Details
### source 
The match field is required, the first argument is the field to inspect, and compare to the expression specified by the second
argument.  In the below example, the message is spected to be something like this from a fictional sample log:

Given this input configuration:

Lets assume that a newline such as the following is appended to foo.jlog:
```
   {"Email":"james@example.com","Active":true,"CreatedDate":"2013-01-20T00:00:00Z","Roles":["User","Admin"]}
```

```json
   "Inputs": {
            "Logs": [
                {
                    "location": "C:\\Logs1\\foo.jlog",
                    "recurse": -1
                }
            ]
        },
        "Filters":[  
            {  
                "json":{  
                    "type":  "Win32-FileLog",                   
                    "target": "stuff",
                    "source": "Text"              
                }
            }]
        }       
```

In the above example, the file foo.jlog is being tailed, and when a newline is appended, it is assumed
to be Json and is parsed from the Text field, the parsed Json is then inserted underneath a property *stuff*

The resulting output would be:
```
  {
    "type": "Win32-FileLog",
    "ComputerName": "dev.vistaprint.net",
    "Text": "{\"Email\":\"james@example.com\",\"Active\":true,\"CreatedDate\":\"2013-01-20T00:00:00Z\",\"Roles\":[\"User\",\"Admin\"]}",
    "stuff": {
      "Email": "james@example.com",
      "Active": true,
      "CreatedDate": "2013-01-20T00:00:00Z",
      "Roles": [
        "User",
        "Admin"
      ]
    }
  }
```

### add_field ["fieldName", "fieldValue", ...]
The fields must be in pairs with fieldName first and value second.
```json
  "Filters": [     
    {
		"json": {      			
			"add_field": [
              "ComputerName", "Host",
              "Username", "%{SID}"				         
			]
		}                
    }     
  ]
```

### remove_field ["tag1", "tag2", ...]
Remove the fields.  More than one field can be specified at a time.
```json
  "Filters": [     
    {
		"json": {      			
			"remove_tag": [             
             "static_tag1",
             "Computer_%{Host}"
			]
		}                
    }     
  ]
```


### add_tag ["tag1", "tag2", ...]
Adds the tag(s) to the tag array.
```json
  "Filters": [     
    {
		"json": {      			
			"add_tag": [
               "foo_%{Host}",
			   "static_tag1"      
			]
		}                
    }     
  ]
```

### remove_tag ["tag1", "tag2", ...]
Remove the tag(s) to the tag array.  More than one tag can be specified at a time.
```json
  "Filters": [     
    {
		"json": {      			
			"remove_tag": [             
             "static_tag1",
             "Username"
			]
		}                
    }     
  ]
```
