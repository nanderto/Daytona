using System;
namespace Daytona.Store
{
    interface IScope
    {
        int Save<T>(T input);
    }
}
