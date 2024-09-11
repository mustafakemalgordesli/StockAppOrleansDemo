using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Orleans;
using Orleans.Runtime;
using StockApp.Contracts;

// Halo Gear of Wars, Skype

// Aktörler 70lerin sonunda Carl Hwitt

// Grains - Cloud native objects

namespace StockApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IClusterClient _client;

        public StocksController(AppDbContext context, IClusterClient client)
        {
            _context = context;
            _client = client;
        }

        // GET: api/Stocks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            return await _context.Stocks.ToListAsync();
        }

        // GET: api/Stocks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Stock>> GetStock(Guid id, [FromQuery] int sleep)
        {
            Stopwatch stopwatch = new Stopwatch();

            
            stopwatch.Start();

            Func<Task<Stock>> func = async () =>
            {
                var grain = _client.GetGrain<IStockGrain>(id);
                return await grain.GetStock(sleep);
            };

            var stock = await Retry(func, 1);


            stopwatch.Stop();


            if (stock == null) NotFound();
                
            return Ok(new { time = stopwatch.Elapsed.TotalSeconds, stock = stock });
        }



        [HttpGet("db/{id}")]
        public async Task<ActionResult<Stock>> GetDbStock(Guid id)
        {
            var stock = await _context.Stocks.FirstOrDefaultAsync(x => x.Id == id);

            if (stock == null) NotFound();

            return Ok(stock);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStock([FromRoute] Guid id, [FromBody] UpdateStockDto dto)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            var grain = _client.GetGrain<IStockGrain>(id);

            var (res, quantity) = await grain.ChangeStock(dto.Quantity, dto.Sleep);

            stopwatch.Stop();

            if (res) return Ok(new { time = stopwatch.Elapsed.TotalSeconds, quantity });

            return BadRequest("Başarısız");
        }


        [HttpPost]
        public async Task<ActionResult<Stock>> PostStock(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            var grain = _client.GetGrain<IStockGrain>(stock.Id);

            await grain.SetStock(stock);

            return CreatedAtAction("GetStock", new { id = stock.Id }, stock);
        }

        private static T Retry<T>(Func<T> func, int maxRetries)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch when (retryCount < maxRetries)
                {
                    retryCount++;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Getting Exception : {ex.Message} after {retryCount} retries.", ex);
                }
            }
        }



    }
}

