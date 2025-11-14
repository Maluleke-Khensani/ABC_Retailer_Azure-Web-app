using ABC_Retailers.Models;

namespace ABC_Retailers.Azure_Services
{
    public interface IFunctionsApi
    {
        Task CreateCustomerAsync(Customers customer);

        Task GetCustomerAsync(string CustomerId);

        Task SendProductUpdateAsync(ProductCatalog product);
        Task<string> UploadProductImageAsync(IFormFile file);
        Task CreateProductAsync(Products product);
        Task CreateOrderAsync(Orders order);
        Task<string> UploadFileToShareAsync(IFormFile file);

        Task<Customers> GetCustomerByUsernameAsync(string username);
        Task<Orders> GetOrderByCustomerIdAsync(string customerId);  


    }
}
