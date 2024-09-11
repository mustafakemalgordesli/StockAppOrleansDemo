using Microsoft.EntityFrameworkCore;
using Orleans.Concurrency;
using Orleans.Timers;
using StockApp.Contracts;

namespace StockApp.OrleansHost;

public class DecreaseStockGrain: Grain, IStockGrain, IDisposable
{
    private Guid _id;

    private const string ReminderName = "ExampleReminder";

    private readonly IReminderRegistry _reminderRegistry;

    private IGrainReminder? _reminder;
    public IGrainContext GrainContext { get; }

    private readonly IPersistentState<Stock> _state;
    private readonly AppDbContext _context;

    public DecreaseStockGrain(
        [PersistentState(stateName: "stock", storageName:"MongoStorage")] IPersistentState<Stock> state,
        AppDbContext context,
        ITimerRegistry timerRegistry,
        IReminderRegistry reminderRegistry,
        IGrainContext grainContext)
    {
        _state = state;
        _context = context;

        timerRegistry.RegisterGrainTimer(
            grainContext,
            callback: static async (state, cancellationToken) =>
            {
                var workerGrain = state.GrainFactory.GetGrain<IStockWorkerGrain>(state._state.State.Id);

                await workerGrain.SaveDb(state._state.State);

                await Task.CompletedTask;
            },
            state: this,
            options: new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(10), // Zamanlayıcının 10 saniye sonra ilk kez tetikleneceğini belirtir
                Period = TimeSpan.FromSeconds(10)  // Zamanlayıcının her 10 saniyede bir çalışacağını gösterir.
            });

        _reminderRegistry = reminderRegistry;

        GrainContext = grainContext;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _id = this.GetPrimaryKey();

        await base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task SetStock(Stock stock)
    {
        _state.State = stock;
        await _state.WriteStateAsync();
    }

    public async Task<(bool, int)> ChangeStock(int quantity, int sleep)
    {
        if (_state.State == null) return (false, 0);

        await Task.Delay(sleep);

        if (_state.State.Quantity >= quantity)
        {
            _state.State.Quantity = _state.State.Quantity - quantity;
            await _state.WriteStateAsync();
            return (true, _state.State.Quantity);
        }

        return (false, 0);
    }

    [ReadOnly]
    public async Task<Stock> GetStock(int sleep)
    {
        await Task.Delay(sleep);
        return _state.State;
    }

    void IDisposable.Dispose()
    {
        if (_reminder is not null)
        {
            _reminderRegistry.UnregisterReminder(
                GrainContext.GrainId, _reminder);
        }
    }
}


public interface IStockWorkerGrain : IGrainWithGuidKey
{
    Task SaveDb(Stock stock);
}

[StatelessWorker(1)]
public class StockWorkerGrain : Grain, IGrain, IStockWorkerGrain
{
    private readonly AppDbContext _context;
    private Guid _id;
    public StockWorkerGrain(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _id = this.GetPrimaryKey();

        await base.OnActivateAsync(cancellationToken);
    }
    public async Task SaveDb(Stock stock)
    {
        var id = this.GetPrimaryKey();
        
        var _stock = await _context.Stocks.FirstOrDefaultAsync(x => x.Id == id);

        if (_stock is null) return;

        _stock.Quantity = stock.Quantity;

        await _context.SaveChangesAsync();
    }
}
