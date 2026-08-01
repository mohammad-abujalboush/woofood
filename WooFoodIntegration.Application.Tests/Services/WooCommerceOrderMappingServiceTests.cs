using Xunit;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Services;
using System.Threading;
using System.Collections.Generic;

namespace WooFoodIntegration.Application.Tests.Services
{
    public class WooCommerceOrderMappingServiceTests
    {
        private readonly WooCommerceOrderMappingService _mappingService;

        public WooCommerceOrderMappingServiceTests()
        {
            _mappingService = new WooCommerceOrderMappingService();
        }

        [Fact]
        public void MapToFoodicsOrderCreateDto_ValidWooCommerceOrder_ReturnsCorrectFoodicsOrder()
        {
            // Arrange
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 123,
                Status = "processing",
                Total = 100.50m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>
                {
                    new WooCommerceOrderWebhookDto.LineItemDto { Id = 1, Name = "Product A", ProductId = "SKU001", Quantity = 2, Price = 25.00m, Total = 50.00m },
                    new WooCommerceOrderWebhookDto.LineItemDto { Id = 2, Name = "Product B", ProductId = "SKU002", Quantity = 1, Price = 50.50m, Total = 50.50m }
                },
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Address1 = "123 Main St",
                    Address2 = "Apt 1",
                    City = "Anytown",
                    State = "Anystate",
                    Postcode = "12345",
                    Country = "USA",
                    Email = "john.doe@example.com",
                    Phone = "555-1234"
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Address1 = "123 Main St",
                    Address2 = "Apt 1",
                    City = "Anytown",
                    State = "Anystate",
                    Postcode = "12345",
                    Country = "USA"
                }
            };

            var cancellationToken = CancellationToken.None;

            // Act
            var foodicsOrder = _mappingService.MapToFoodicsOrderCreateDto(wooCommerceOrder, cancellationToken);

            // Assert
            Assert.NotNull(foodicsOrder);
            Assert.Equal(wooCommerceOrder.Id.ToString(), foodicsOrder.Reference);
            Assert.Equal(wooCommerceOrder.Total, foodicsOrder.TotalPrice);
            Assert.Equal("pending", foodicsOrder.Status); // WooCommerce 'processing' maps to Foodics 'pending'
            Assert.Equal(wooCommerceOrder.LineItems.Count, foodicsOrder.Products.Count);
            Assert.Equal($"{wooCommerceOrder.Billing.FirstName} {wooCommerceOrder.Billing.LastName}", foodicsOrder.Customer.Name);
            Assert.Equal(wooCommerceOrder.Billing.Phone, foodicsOrder.Customer.Phone);
            Assert.Equal(wooCommerceOrder.Billing.Email, foodicsOrder.Customer.Email);

            foreach (var wooItem in wooCommerceOrder.LineItems)
            {
                var foodicsItem = foodicsOrder.Products.FirstOrDefault(p => p.ProductReference == wooItem.ProductId);
                Assert.NotNull(foodicsItem);
                Assert.Equal(wooItem.Quantity, foodicsItem.Quantity);
                Assert.Equal(wooItem.Price, foodicsItem.Price);
            }
        }

        [Fact]
        public void MapToFoodicsOrderCreateDto_OtherWooCommerceStatus_MapsDirectly()
        {
            // Arrange
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 456,
                Status = "completed", // Other status
                Total = 50.00m,
                Currency = "SAR",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>
                {
                    new WooCommerceOrderWebhookDto.LineItemDto { Id = 3, Name = "Product C", ProductId = "SKU003", Quantity = 1, Price = 50.00m, Total = 50.00m }
                },
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Address1 = "456 Oak Ave",
                    Address2 = "",
                    City = "Anothercity",
                    State = "Otherstate",
                    Postcode = "54321",
                    Country = "KSA",
                    Email = "jane.smith@example.com",
                    Phone = "555-5678"
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Address1 = "456 Oak Ave",
                    Address2 = "",
                    City = "Anothercity",
                    State = "Otherstate",
                    Postcode = "54321",
                    Country = "KSA"
                }
            };

            var cancellationToken = CancellationToken.None;

            // Act
            var foodicsOrder = _mappingService.MapToFoodicsOrderCreateDto(wooCommerceOrder, cancellationToken);

            // Assert
            Assert.NotNull(foodicsOrder);
            Assert.Equal("completed", foodicsOrder.Status); // Should map directly
        }
    }
}