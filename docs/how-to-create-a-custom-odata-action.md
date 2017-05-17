# Custom OData actions
The [REST API](odata-rest-api.md) of sensenet ECM is built on OData actions, and you can create your own custom ones too to extend this API.

## Action method
In most cases it is sufficient to implement a custom operation as a simple **method in .Net**, the same way as you would write an ASP.NET **Web API** method.
- [OData action method](http://wiki.sensenet.com/Generic_Sense/Net_OData_action)

## Action class
There are cases when you need to customize the behavior of your action further, e.g. hiding the action in certain cases. You can do that by implementing a few methods in an action class that inherits one of the built-in base classes.
- [Custom action class](http://wiki.sensenet.com/How_to_create_a_custom_OData_action)