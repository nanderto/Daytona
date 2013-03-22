using System;
using System.Threading.Tasks;

namespace Daytona.Store
{
    interface IScope
    {
        Actor actor { get; set; }
        int Save<T>(T input) where T : IPayload;
        Task<int> SaveAsync<T>(T input) where T : IPayload;
    }
}
