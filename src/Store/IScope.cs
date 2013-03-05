using System;
using System.Threading.Tasks;

namespace Daytona.Store
{
    interface IScope
    {
        int Save<T>(T input);
        Task<int> SaveAsync<T>(T input) where T : IPayload;
    }
}
