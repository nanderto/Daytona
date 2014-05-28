using System;
using System.Threading.Tasks;

namespace Daytona.Store
{
    using System.Collections.Generic;

    interface IScope
    {
        Actor actor { get; set; }
        
        int Save<T>(T input) where T : IPayload;
        
        Task<int> SaveAsync<T>(T input) where T : IPayload;

        Task<T> GetAsync<T>(int id) where T : IPayload;

        Task<List<T>> GetAllAsync<T>() where T : IPayload;
    }
}
