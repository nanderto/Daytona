Daytona is an experimental Actor Model Framework written in C# and using ZeroMQ as the messaging library to support messaging between Actors. It is capable of the following requirements in support of the actor model
1. Single threaded within the actor.
2. Only communicates with other actors via messaging
3. Actors can create other actors

This is version 2 of this framework. It is built on version 1 but this version has been a little more influenced by Orleans than the first version was. The original version allowed you to create Actors, which were intentionalyy created and started, stoped and disposed of. In this version you can create virtual actors as well. Just create a silo (name also borrowed from Orleans), register some entities (yes I am tempted to call them grains) in the silo, then use the Silo.ActorFactory to create your entity. Instead of creating an entity it uses NProxy to generate a dynamic proxy. This proxy intercepts the call to a method and instead sends a message to the object. The silo has been configured with a listener that will instantiate an actor of the type of your entity and start it with the data from the message. All subsequent calls to that address will go to this object.
Another major change is the update to the NetMq library, this library had not started when this project was started but it appears to be the only way forward for a .Net Implmentation of ZeroMQ.

Under the hood the framework is still substantially the same. The Actor is the central object in the framework, and works in a recursive manor. You create an Actor then register additional actors which are started and will run on their own thread. As the Actors are created they set themselves up to listen for messages on a common ZeroMQ socket. They respond only to messages on that socket that are addressed to them. When sending messages to another actor they send on a separate ZeroMQ socket which is forwarded to the Receive socket by the Exchange Object. The Exchange object is a simple class that implements message forwarding from the send socket to the receive socket.

**Usage**

You host this in a Windows application, Console, forms or Windows service.

1. First thing you need to do is create a Silo. The Silo will construct it self via the create method, and tear itself down when being disposed of. So you should use the "Using" pattern

    using (var silo = Silo.Create())
    {}

You should create only one per application, it will under the hood create a NetMQContext (also limited to one per application), and once the context is created it will set up anexchange to forward messages to the correct address, and its own internal actors that run create the virtual framework to manage your actors.


2. Then you can use your silo. All that is required is to register your actors prior to starting the Silo.

    
    using (var silo = Silo.Create())
    {
        silo.RegisterEntity(typeof(ConsoleReaderActor));
        silo.RegisterEntity(typeof(ConsoleWriterActor));
        silo.RegisterEntity(typeof(ValidationActor));
        silo.Start();
        var reader =    silo.ActorFactory.CreateInstance<IConsoleReaderActor>(typeof(ConsoleReaderActor));
        reader.Read();
        do
        {
            Thread.Sleep(1000);
        }
        while (DontBreak);
        silo.Stop();
    }


In the above sample you can see a consoleReaderActor, ValidationActor and a ConsoleWriterActor being registered. Then you see the silo being started, which must occur in this order. Next you can see the reader object being created by the Actorfactory, this is a proxy object and will take what ever parameters you have passed the method and the method name and send them to the actual object. The reader.Read call sends a message to the address inproc://<fullobjectname>/objectId the silo has an actor set up to monitor all addresses. If it can not find a working version it will create a new one. If one already exists then it will also read the message and respond to it.
The last few lines of code are just there to ensure that the thread does not exit before it is supposed to. The last lines silo.Stop, the close of the using statement for the Silo are part of the gracefull shut down of the app.

3. Creating Actors

Just create a class and give it some methods that return void

    
    [Serializable]
    public class ConsoleReaderActor : ActorFactory, IConsoleReaderActor
    {
        public const string ExitCommand = "exit";

        private IValidationActor validator;

        [JsonIgnore]
        public override Actor Factory { get; set; }

        public void Read()
        {
            if (this.validator == null)
            {
                this.validator = this.Factory.CreateInstance<IValidationActor>(typeof(ValidationActor));
            }

            this.GetAndValidateInput(this.validator);
        }

        public void ReadAgain()
        {
            this.Read();
        }


        private void GetAndValidateInput(IValidationActor validator)
        {
            var message = Console.ReadLine();

            if (String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the entire actor system (allows the process to exit)
                Program.DontBreak = false;
            }
            else
            {
                this.validator.Validate(message);
            }
        }
    }  

Above is the ConsoleReaderActor, you need to mark it as Serializable because it is persisted to a store via serialization. You also need to declare an Interface for the methods that you want to send messages to your actor. In this case the "Read" and "ReadAgain" methods form the interface. Because this actor is creating other actors you need to inherit from the ActorFactory, this will make the Factory avaiable to create the other actors. This is not necessary if you do not want your actor to create other actors. A couple of idiosyncracies The Actor Factory cannot and should not be serialized, since you can use your own serializer you have to override the property and apply your own tag to it to avoid it being serialized. In this case it has the JsonIgnore attribut applied to it (this might be fixable). Also you cannot currently create your own constructor, this object gets deserialized from persistant storage, and its not possible to have the actorfactory available during this instantiation. The result is that if you want to call another actor you have to check if it has been cre3ated by a previous call. See the first 5 lines of code of the read method. (Also looking into this)


Not shown here is the validator and writer objects. On seperate threads one reads the console, then passes the information to the validator actor which validates it then it passes the message to the writer so that it can write out the message to the console. After writing out to the console but prior to waiting on the next message it tells the reader to read another line.

**Running Stuff**
This project is still in discovery phase so please excuse the mess. If you want to run anything you need to build and run the Monitor program first. The actors are sending messages to it to display in the console, I am using a request response pattern to guarantee the messages. At some point I will pull this out but for now its a (mostly) well behaved way to get messages.

The easiest place to start is with the ConsoleReaderWriter project. After starting the monitor, it should fire up and run quite nicely, I borrowed the example from the training offered by AKKA.Net, so shout out to them for the idea.
