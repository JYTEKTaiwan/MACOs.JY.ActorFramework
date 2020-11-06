# MACOs.JY.ActorFramework

This library is designed to help customer using actor model to build a complicated system without taking care of threading issues. Unlike web-base coding, this library puts more focus on the test & measurement application, so we try our best to simplify the structure and programming procedure to make the library more light-weighted and more easy to use.

## What is Actor

### Object V.S. Threads

In object-oriented programming, everything is treated like an independent object. Object can get/set properties, execute methods in the caller threads. When it comes to a complex system contains many threads and many objects, sometimes there might be some unexpected block issues that influence the performance. Using threadpool to put the method to other thread is applicable, but in our test & measurement world, data flows continuously, it's better to keep an thread alive until user manually close it.

To handle both of the problems (blocking and thread-alive), we're adopting the actor model in this framework

### Actor Model for the framework

Actor model is not a new term, it was first originated in 1973. there are 3 basic rules for actor model 

https://en.wikipedia.org/wiki/Actor_model

- send a finite number of messages to other actors;
- create a finite number of new actors;
- designate the behavior to be used for the next message it receives.

Actor model uses it own internal thread to wait for new  messages. When new messages are received, they are queued inside the actor and executed one by one (FIFO). Actor model can easily keep the execution and message handling in the same internal thread, which mean every actor has their own execution thread to avoid blocking between each of them.

## Structures for MACOs.JY.ActorFramework

This framework has several classes included

- ActorFactory

  Factory that create, keep, delete the actor instances

- Actor

  Abstract class the implements most of the actor model, user should inherit from this class

- ActorCommand

  Message basis class

## Dependencies

This framework uses several different open source libraries to achieve the goal

- NetMQ v4.0.1.6
- NLog v4.7.5
- Newtonsoft.Json v12.0.3

## How to use it

User can use the library to build their own actor class in two steps:

1. Inherited from actor class (abstract)
2. Add ActorCommandAttribute on the method you would like to turn into message, and then assign the method name for it.

For example;

```c#
public class Test:Actor
{
	[ActorCommand("A")]
    public string MethodA(int a)
    {
        return a.ToString();
    }
    public void MethodB()
    {
        //do something
    }
}
```

To simply operate the actor, only 3 steps should be done:

```c#
var dev = ActorFactory.Create<TestActor>(true, "Dummy");
dev.ExecuteAsync("A", (int)5);
ActorFactory.StopAllActors();
```


1. We use factory class to create, store, delete actors
2. when actor is created, user can choose whether enable the log mode or not, and assign the alias name to the actor
3. actor can now call "ExecuteAsync" method to process the command


