using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApp.Contracts;

public interface IGrainStateAccessor<T> : IGrainExtension
{
    Task<T> GetState();
    Task SetState(T state);
}

public sealed class GrainStateAccessor<T> : IGrainStateAccessor<T>
{
    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    public GrainStateAccessor(Func<T> getter, Action<T> setter)
    {
        _getter = getter;
        _setter = setter;
    }

    public Task<T> GetState()
    {
        return Task.FromResult(_getter.Invoke());
    }

    public Task SetState(T state)
    {
        _setter.Invoke(state);
        return Task.CompletedTask;
    }
}