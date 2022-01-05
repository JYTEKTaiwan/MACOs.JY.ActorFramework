import clr
import numpy as np
import time as sw
import sys

sys.path.append(r"E:\repos\MACOs.JY.ActorFramework\src\Implement.NetMQ\bin\Release\net472")
sys.path.append(r"E:\repos\MACOs.JY.ActorFramework\src\Core\bin\Release\net472")
sys.path.append(r"E:\repos\MACOs.JY.ActorFramework\examples\net472_DEMO\bin\Release")
clr.AddReference("net472_DEMO")
clr.AddReference("MACOs.JY.ActorFramework.Implement.NetMQ")
clr.AddReference("MACOs.JY.ActorFramework.Core")


from MACOs.JY.ActorFramework.Implement.NetMQ import *
from MACOs.JY.ActorFramework.Core import *
from MACOs.JY.ActorFramework.Core.Commands import *
from System.Net import *
from System.Net.Sockets import *
from net472_DEMO import *


def intTryParse(value):
    try:
        int(value)
        return True
    except ValueError:
        return False



ipList=Dns.GetHostEntry(Dns.GetHostName()).AddressList
for x in ipList:
    if x.AddressFamily == AddressFamily.InterNetwork:
        ip=x.ToString()



context=NetMQDataBusContext()
context.AliasName="DEMO"

server=TestService()
server.LoadDataBus(context)


clientConnInfo = NetMQClientContext( "DEMO");
client=clientConnInfo.Search()
           
print("========================================================================================");
print("== Welcome to MACOs.JY.ActorFramework example, there are 3 types of command supported ==");
print("==      1. key in Q will leave the program                                            ==");
print("==      2. key in any text except Q will immediate response                           ==");
print("==      3. key in number will reponse the double array with the assigned size         ==");
print("========================================================================================");

while True:
    str = input("Enter Command: ")
    if str == "Q":
        break
    elif intTryParse(str):
        data=np.zeros(int(str))        
        res=server.ExecuteCommand(server.QueryCommand.Generate(data))
        start=sw.time()
        res=server.ExecuteCommand(server.QueryCommand.Generate(data))
        ellaps=1000*(sw.time()-start);
        print(res)
        print(f'{ellaps} ms')
        print()

    else :
        start=sw.time()
        res = server.ExecuteCommand(server.TestCommand.Generate(str));        
        ellaps=1000*(sw.time()-start);
        print(res)
        print(f'{ellaps} ms')
        print()


server.Dispose()
client.Dispose()




