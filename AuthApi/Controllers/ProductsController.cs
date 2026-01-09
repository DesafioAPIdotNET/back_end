using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Models;

namespace AuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Buscando todos os produtos.");
            return await _context.Products.ToListAsync();
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            // A validação do modelo (DataAnnotations) ocorre automaticamente aqui
            // Se o modelo for inválido, retorna 400 Bad Request
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Produto criado com ID: {product.Id}");

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }
    }
}