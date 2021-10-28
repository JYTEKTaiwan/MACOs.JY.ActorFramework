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

The example below use NetMQ as the databus implementation object, also we bring a "Client" class to connect to Device as simple as possible (Thanks to NetMQ!!)

```c#
public class TestService : DeviceBase 
{   
    public string WalkyTalky(string content)
    {
        return string.Format($"[{DateTime.Now.ToString()}] Roger!\t{content}");
    }
}
```

```c#
class Program
{
    static void Main(string[] args)
    {        
        //initial the device object and configure the databus
        TestService server = new TestService();
        server.LoadDataBus(new NetMQDataBusContext()
        {
            AliasName="DEMO",
        });
        
        //Create an Client object to automatically search on the web
        var clientConnInfo = new NetMQClientContext("DEMO");
        var client = clientConnInfo.Search();
        
        var str = Console.ReadLine();
        
        //Execute the command call WalkyTalky with parameter str
        var res = server.ExecuteCommand(new Command<string>("WalkyTalky", str));
        
        //close the client and server
    	client.Dispose();
    	server.Dispose();
    }
}
```

