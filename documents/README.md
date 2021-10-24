# MACOs.JY.ActorFramework

The Main purpose of this library is to bring a simpe framework that contains the transmission bus (IDataBus) and the coresponding command pattern object (ICommand). Any object ("Device") inherit from this framwork can easily  be accessed and controlled through "DataBus" using "Command".

The 3 terms "Device", "DataBus", "Command" are the key concept of this framework and  explained below

## [Terminology]
### IDevice
	Strictly defined the behavior of device
### IDeviceContext
	An alternative way to create Device object using context object
### DeviceBase
	Default abstract class, already include the logic and behvior. User can create their own by inherit IDevice.

### ICommand
	Interface for Command object
### CommandBase
	Abstract class for Command object

### IDataBus
	Interface for DataBus
### IDataBusContext
	An alternative way to create Databus object using context object

By Default , JYTEK provides the implementation class that using NetMQ as the DataBus, and also a Client class that can connect the Device across different PC throught ethernet.

