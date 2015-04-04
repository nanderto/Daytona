namespace ConsoleReaderWriter
{
    using System;

    using Daytona;

    using Newtonsoft.Json;

    public interface IConsoleReaderActor
    {
        void Read();

        void ReadAgain();
    }

    [Serializable]
    public class ConsoleReaderActor : ActorFactory, IConsoleReaderActor
    {
        public const string ExitCommand = "exit";

        private IValidationActor validator;

        //public ConsoleReaderActor(Actor factory)
        //    : base(factory)
        //{
        //    if (this.validator == null)
        //    {
        //        this.validator = this.Factory.CreateInstance<IValidationActor>(typeof(ValidationActor));
        //    }
        //}

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
}