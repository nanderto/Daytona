using System;
using System.Threading.Tasks;

namespace Daytona.Store
{
    interface IScope
    {
        int Save<T>(T input) where T : IPayload;
        Task<int> SaveAsync<T>(T input) where T : IPayload;
    }
}
