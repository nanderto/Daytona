namespace ConsoleReaderWriter
{
    using System;

    using Daytona;

    using Newtonsoft.Json;

    public interface IValidationActor
    {
        void Validate(object message);
    }

    public class ValidationActor : ActorFactory, IValidationActor
    {
        private IConsoleWriterActor writer;

        public int callCounter = 0;
        
        [JsonIgnore]
        public override Actor Factory { get; set; }

        public void Validate(object message)
        {
            ++ callCounter;

            if (this.writer == null)
            {
                this.writer = this.Factory.CreateInstance<IConsoleWriterActor>(typeof(ConsoleWriterActor));
            }

            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                // signal that the user needs to supply an input
                this.writer.WriteOutput(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);
                if (valid)
                {
                    // send success to console writer
                    this.writer.WriteOutput(new Messages.InputSuccess($"Call Counter is {callCounter}. Thank you! Message was valid. The message is: {msg}"));

                    ////for a test for throwing exceptions this is a strange place to have the exception
                    ////read below for details
                    if(callCounter > 3) throw new Exception("my exception");
                    
                    //// if the exception above gets thrown before this line gets hit then this application will fail eventhough
                    //// the actor will gracefully exit and restart on the next time it is called. 
                    //// this is becasue this design chains calls from one actor to the next,
                    //// if the chain is broken then it eill not continue the loop of calling actors.
                    //// this is more a pitfall of the actor model I think thana a problem with the daytona 
                    //// framework, yhou need to be carefull even if your objects will fail and excit gracefully
                    //// that they still complete the work they have to do for the whole app to survive
                    // this.writer.WriteOutput(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    // signal that input was bad
                    this.writer.WriteOutput(
                        new Messages.ValidationError($"Call Counter is {callCounter}. Invalid: input had odd number of characters."));
                }
            }
        }

        /// <summary>
        /// Determines if the message received is valid.
        /// Currently, arbitrarily checks if number of chars in message received is even.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool IsValid(string msg)
        {
            var valid = msg.Length % 2 == 0;
            return valid;
        }
    }
}