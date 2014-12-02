Daytona is an experimental Actor Model Framework written in C# and using ZeroMQ as the messaging library to support messaging between Actors. It is capable of the following requirements in support of the actor model 1. Single threaded within the actor. 2. Only communicates with other actors via messaging 3. Actors can create other actors

This is the original implementationthat has been branced for safe keeping. 

The framework uses Delegates to provide extensibility rather than using classes interfaces and inheritance to provide extension points. This was done simply to experiment with an alternative method, and has resulted in a nice feel where the actor factory essentially stores a dictionary of functions that create the Actor when started on a new thread. This is achieved at the expense of some complication in the setup of the actors.

The Actor is the Central object in the framework, and works in a recursive manor. You create an Actor then register additional actors which are started and will run on their own thread. I have considered separating this into an actorfactory, but for now, since a sub actor could act as a factory, I have left it in the same class. As the Actors are created they set themselves up to listen for messages on a common ZeroMQ socket. They respond only to messages on that socket that are addressed to them. When sending messages to another actor they send on a separate ZeroMQ socket which is forwarded to the Receive socket by the Pipe Object. This is a simple class that implements message forwarding from the send to the receive socket, It�s a lame implementation right now, but I am working to increate my ZeroMQ knowledge.

**Usage**

You host this in a Windows application, Console, forms or Windows service.

1. First thing you need to do is create a ZeroMQ Context object their should be only one in your application and  you pass it in to all of the actors that you create. It is used to set up the send and receive sockets, and is thread safe to pass around. 
using (var context = new Context(1))
2. Next Set up the Pipe, again ZeroMQ handles the threading for you.
##
    using (var actor = new Actor(context))
    {
         actor.RegisterActor("XXXX", "11111 ", "10001 ", (Message, 			InRoute, OutRoute, OutputChannel) =>
        {
           Your code that does something goes here
        }
    }

In the above sample the XXXX is a unique name for the key to a dictionary object where your Lambda is stored so it can be executed buy name
The 11111 is the address, the actor will respond to messages sent to this address. IN this implementation it uses pattern matching to match the start or the message with this address.
The 10001 is the address to send output messages to. In a more advance version this will likely need to be changed allow sending multiple other Actors. Finally a Lambda is passed into the actor which is the workload for the actor, and should be a function that takes the message, the inroute, the OutRoute and the OutputChannel for sending messages. There are a couple of other overloaded RegisterActor Methods that allow for simler and more complex workloads.
Here is another example of the possibility of adding a couple of actors. The second registered actor you can see adds a count to a property bag for the actor and increments it each time it is called. Again my implementation of the property bag is a little lame but it�s a proof of concept at this stage.

##
    actor.RegisterActor("Default", "10001 ", (Message, Route) =>
    {
        if (Message.Substring(0, 6) == Route)
        {
            Console.WriteLine("yeah baby " + Route + " Message: " + Message);
        }
        else
        {
            Console.WriteLine("handled by wrong handler");
         }
     }).RegisterActor("YYYY", "", "no out route",(Message, Route,           OutRoute, OutputChannel, Actor) =>
        {
            string value;
            Int64 Count = 0;
            if (Actor.PropertyBag.TryGetValue("Count", out value))
            {
                Count = int.Parse(value);
                ++Count;
            }
            Actor.PropertyBag["Count"] = Count.ToString();
            Console.WriteLine("Current Count: " + Count.ToString() + " " + Route + " Message: " + Message);
        });
            
      actor.StartAllActors();

Finally after registering actors you can Start them all.
 
Your registration Lambda can also contain sub actors so that the actor can create other actors here is an example of how to do this 

    .RegisterActor("YYYY", "", "no out route",(Message, Route, OutRoute, OutputChannel, Actor) =>
      {
         using (var innerActor = new Actor(context))
                        {
                            innerActor.RegisterActor("Pinger", "Ping ", (innerMessage, innerRoute) =>
                            {
                                Console.WriteLine("In Pinger ");
                            });
                            innerActor.StartNewActor("Pinger");
                        }
    });****


