# Mutate Filter
The mutate filter allows you to perform general mutations on fields. You can rename, remove, replace and modify fields in your events.  This filter will automatically be applied to all inputs before sending to the outputs.   If you want to make a
filter conditional, use the ***condition*** property to specify a legal C# expression.

## Mutate Parameters
The following parameters are allowed when configuring WindowsEvents.

| Parameter   |     Type        | Description                                                            
| :-----------|:----------------|:-----------------------------------------------------------------------
| *condition* | property:string |Windows event logs
|```Code goes here```
| *rename*    | property:array  |Rename one or more fields                                       
| *replace*   | property:string |Replace a field with a new value.  The new value can include %{foo} strings to help you build a new value from other parts of the event.                                   
| *split*     | property:string |Separator between values of the "Strings" field.                         


