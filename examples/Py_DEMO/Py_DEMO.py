import clr
import numpy as np
import time as sw

clr.AddReference(r"C:\Users\Way\source\repos\MACOs.Services\HAS_DEMO_net472\bin\Debug\HAS_DEMO_net472.dll")
clr.AddReference(r"C:\Users\Way\source\repos\MACOs.Services\HAS_DEMO_net472\bin\Debug\MACOs.Services.dll")
clr.AddReference(r"C:\Users\Way\source\repos\MACOs.Services\packages\System.Runtime.CompilerServices.Unsafe.4.5.3\lib\netstandard2.0\System.Runtime.CompilerServices.Unsafe.dll")

from HAS_DEMO_net472 import *
from MACOs.Services import *
from MACOs.Services.Clients import *
from MACOs.Services.Utilities import *

def intTryParse(value):
    try:
        int(value)
        return True
    except ValueError:
        return False

    
list=ServiceManager.LoadJsonAndRun()

clientConnInfo = NetMQClientContext(9999, "DEMO");
client=clientConnInfo.Search()

while True:
    str = input("Enter Command: ")
    if str == "Q":
        break
    elif intTryParse(str):
        data=np.zeros(int(str))        
        cmd=TestService.QueryCommand(data)
        start=sw.time()
        client.Send(cmd)
        res=client.Receive()
        ellaps=1000*(sw.time()-start);
        print(res)
        print(f'{ellaps} ms')
        print()

    else :
        start=sw.time()
        cmd=TestService.TestCommand()
        cmd.Content=str
        client.Send(cmd)
        res=client.Receive()
        ellaps=1000*(sw.time()-start);
        print(res)
        print(f'{ellaps} ms')
        print()


ServiceManager.StopServices(list)
Utility.CleanAll()




