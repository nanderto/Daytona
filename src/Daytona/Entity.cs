namespace Daytona
{
    using System;

    /// <summary>
    /// The entity is the business object you want to run as a Actor
    /// </summary>
    public class Entity 
    {
        public Entity(Type type)
        {
            this.EntityType = type;
            this.Name = type.FullName;
            this.AssemblyQualifiedName = type.AssemblyQualifiedName;
        }

        public Type EntityType { get; set; }

        public string Name { get; set; }

        public string FullAssemblyInfo { get; set; }

        public string AssemblyQualifiedName { get; set; }
    }
}