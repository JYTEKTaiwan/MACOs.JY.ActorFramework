# MACOs.JY.ActorFramework

## TERMINOLOGY
The Main purpose of this library is to bring a simpe framework that contains the transmission bus (DataBus) and the coresponding command pattern object (Command). Any object ("Device") inherit from this framwork can easily  be accessed and controlled through "DataBus" using "Command".

These 3 terms "Device", "DataBus", "Command" are the key concepts of this framework and  explained below

### Device class (abstract)
Device is the abstract class that contains "DataBus" as the communication interface. User can inherit from this abstract class to easily access the method by any "Command" coming through the "DataBus" 

### Databus class (abstract)
The purpose of the Databus object is simple: use it as an interface so any implementation can be easily inject into the device. JYTEK bring an implementation using NetMQ as the communication bus, but user can write their own databus as needed. 

### Command
The Command object consists of two items, the name of the method in "Device" object and the parameters for the method (up to 7 parameters). The command will be search and executed using reflection. Normally the return value will be string using default "ConvertToString" method, but user can override it using their own command class by inheriting "Command" class.

## EXAMPLES

We're trying to make the customized class acted like an actor as easy as possible. This example below use NetMQ as the databus implementation object, also we bring a "Client" class to connect to Device as simple as possible (Thanks to NetMQ!!)

### Make your own class

First, like everything we learned from C# 101, create a class named "TestService" and a method called "WalkyTalky". Then inherits from DeviceBase class. Now your custom class can be access through the DataBus.

```c#
public class TestService : DeviceBase 
{   
    public string WalkyTalky(string content)
    {
        return string.Format($"[{DateTime.Now.ToString()}] Roger!\t{content}");
    }
}
```



### How to make your class online and close it

After the object is being created, you should call another method "LoadDataBus" to inject the DataBus object into the "TestService" object. JYTEK now provides the DataBus implementation class that uses NetMQ, which can make your object being searched and use through network.

Call "Dispose" when you're finished.


```c#
//initial the device object and configure the databus
TestService server = new TestService();
//Load databus
server.LoadDataBus(new NetMQDataBusContext()
{
	AliasName="DEMO",
});

//Do Something
server.Dispose();
```



### How to connect to the object?

In the implementation class, JYTEK provides searching functionality so user don't need to worry about the ip configuration, just put the alias name you're looking for. JYTEK also provide a NetMQClient class which can make it easier to finish the connection.

```c#
//Create an context with alias name (can also speficy the ip and port)
var clientConnInfo = new NetMQClientContext("DEMO");
//call search to automatiacally look for the peer in the web
var client = clientConnInfo.Search();
```

### Let's commute!

JYTEK provides "CommandBase" abstract class, user can create the command object by using "Create" Method. The name and parameters must be perfectly matched with the custom method. Use "Query" to parse, execute and return the result.

This is being done by NetMQ implementaion class, which makes your object online

```C#
var str = Console.ReadLine();
//Create command object with the name and parameters
CommandBase cmd = CommandBase.Create("WalkyTalky", str);
//Call the ExecuteCommand and get the response
var res = client.Query(cmd);
```



Remember to call "Dispose" after you're finished.



Here's the total example code:

```c#
class Program
{
    static void Main(string[] args)
    {        
		//initial the device object and configure the databus
		TestService server = new TestService();
		//Load databus
		server.LoadDataBus(new NetMQDataBusContext()
		{
			AliasName="DEMO",
		});
        
        //Create an context with alias name (can also speficy the ip and port)
		var clientConnInfo = new NetMQClientContext("DEMO");
		//call search to automatiacally look for the peer in the web
		var client = clientConnInfo.Search();		
        
        var str = Console.ReadLine();
        
        //Create command object with the name and parameters
		CommandBase cmd = CommandBase.Create("WalkyTalky", str);
		//Call the ExecuteCommand and get the response
		var res = client.Query(cmd);	
        
        //close the client and server
    	client.Dispose();
    	server.Dispose();
    }
}
```

