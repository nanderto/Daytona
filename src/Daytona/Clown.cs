namespace Daytona
{
    using System;

    public class Clown 
    {
        public Clown(Type type)
        {
            this.ClownType = type;
            this.Name = type.FullName;
            this.AssemblyQualifiedName = type.AssemblyQualifiedName;
        }

        public Type ClownType { get; set; }

        public string Name { get; set; }

        public string FullAssemblyInfo { get; set; }

        public string AssemblyQualifiedName { get; set; }
    }
}