using Xunit;
using Moq;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Application.Services;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;
using System.Threading;
using System.Collections.Generic;

namespace WooFoodIntegration.Application.Tests.Services
{
    public class SynchronizationServiceTests
    {
        private readonly Mock<IWooCommerceOrderMappingService> _mockWooCommerceOrderMappingService;
        private readonly Mock<IWooCommerceService> _mockWooCommerceService;
        private readonly Mock<IFoodicsService> _mockFoodicsService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ISynchronizationLogRepository> _mockSynchronizationLogRepository;
        private readonly SynchronizationService _synchronizationService;

        public SynchronizationServiceTests()
        {
            _mockWooCommerceOrderMappingService = new Mock<IWooCommerceOrderMappingService>();
            _mockWooCommerceService = new Mock<IWooCommerceService>();
            _mockFoodicsService = new Mock<IFoodicsService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSynchronizationLogRepository = new Mock<ISynchronizationLogRepository>();

            _mockUnitOfWork.Setup(uow => uow.SynchronizationLogs).Returns(_mockSynchronizationLogRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _synchronizationService = new SynchronizationService(
                _mockWooCommerceOrderMappingService.Object,
                _mockWooCommerceService.Object,
                _mockFoodicsService.Object,
                _mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ProcessWooCommerceOrderCreatedAsync_FoodicsOrderCreationSuccessful_LogsSuccess()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 123,
                Status = "processing",
                Total = 100.50m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>(),
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            var foodicsOrderDto = new FoodicsOrderCreateDto
            {
                Reference = wooCommerceOrder.Id.ToString(),
                TotalPrice = wooCommerceOrder.Total,
                Status = "pending",
                Products = new List<FoodicsOrderCreateDto.FoodicsLineItemDto>(),
                Customer = new FoodicsOrderCreateDto.FoodicsCustomerDto { Name = "John Doe", Phone = "", Email = "" }
            };

            _mockWooCommerceOrderMappingService.Setup(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()))
                .Returns(foodicsOrderDto);
            _mockFoodicsService.Setup(fs => fs.CreateOrderAsync(tenantId, foodicsOrderDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderCreatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.Status);
            Assert.Equal("Foodics order created successfully.", result.Message);
            Assert.Equal(foodicsOrderDto.Reference, result.TargetEntityId);

            _mockWooCommerceOrderMappingService.Verify(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()), Times.Once);
            _mockFoodicsService.Verify(fs => fs.CreateOrderAsync(tenantId, foodicsOrderDto, It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessWooCommerceOrderCreatedAsync_FoodicsOrderCreationFailed_LogsFailure()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 123,
                Status = "processing",
                Total = 100.50m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>(),
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            var foodicsOrderDto = new FoodicsOrderCreateDto
            {
                Reference = wooCommerceOrder.Id.ToString(),
                TotalPrice = wooCommerceOrder.Total,
                Status = "pending",
                Products = new List<FoodicsOrderCreateDto.FoodicsLineItemDto>(),
                Customer = new FoodicsOrderCreateDto.FoodicsCustomerDto { Name = "John Doe", Phone = "", Email = "" }
            };

            _mockWooCommerceOrderMappingService.Setup(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()))
                .Returns(foodicsOrderDto);
            _mockFoodicsService.Setup(fs => fs.CreateOrderAsync(tenantId, foodicsOrderDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Simulate failure

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderCreatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.Status);
            Assert.Equal("Failed to create Foodics order.", result.Message);

            _mockWooCommerceOrderMappingService.Verify(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()), Times.Once);
            _mockFoodicsService.Verify(fs => fs.CreateOrderAsync(tenantId, foodicsOrderDto, It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessWooCommerceOrderCreatedAsync_MappingThrowsException_LogsFailure()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 123,
                Status = "processing",
                Total = 100.50m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>(),
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            _mockWooCommerceOrderMappingService.Setup(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()))
                .Throws(new Exception("Mapping error."));

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderCreatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Mapping error.", result.Message);

            _mockWooCommerceOrderMappingService.Verify(m => m.MapToFoodicsOrderCreateDto(wooCommerceOrder, It.IsAny<CancellationToken>()), Times.Once);
            _mockFoodicsService.Verify(fs => fs.CreateOrderAsync(It.IsAny<Guid>(), It.IsAny<FoodicsOrderCreateDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessWooCommerceOrderUpdatedAsync_RefundedOrder_UpdatesFoodicsStockAndLogsSuccess()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 456,
                Status = "refunded",
                Total = 50.00m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>
                {
                    new WooCommerceOrderWebhookDto.LineItemDto { Id = 1, Name = "Product A", ProductId = "SKU001", Quantity = 1, Price = 50.00m, Total = 50.00m }
                },
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "Jane", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "Jane", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            _mockFoodicsService.Setup(fs => fs.UpdateProductStockAsync(tenantId, It.IsAny<FoodicsStockUpdateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderUpdatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.Status);
            Assert.Equal("WooCommerce return processed, Foodics stock updated.", result.Message);

            _mockFoodicsService.Verify(fs => fs.UpdateProductStockAsync(tenantId, It.Is<FoodicsStockUpdateDto>(dto => dto.ProductReference == "SKU001" && dto.NewQuantity == 1), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessWooCommerceOrderUpdatedAsync_NonRefundedOrder_LogsSkipped()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 789,
                Status = "completed", // Not a refunded/cancelled status
                Total = 25.00m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>(),
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "", LastName = "", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "", LastName = "", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderUpdatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Skipped", result.Status);
            Assert.Equal("WooCommerce order update not a recognized return/refund event.", result.Message);

            _mockFoodicsService.Verify(fs => fs.UpdateProductStockAsync(It.IsAny<Guid>(), It.IsAny<FoodicsStockUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessWooCommerceOrderUpdatedAsync_FoodicsStockUpdateFails_LogsFailure()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var wooCommerceOrder = new WooCommerceOrderWebhookDto
            {
                Id = 456,
                Status = "refunded",
                Total = 50.00m,
                Currency = "USD",
                LineItems = new List<WooCommerceOrderWebhookDto.LineItemDto>
                {
                    new WooCommerceOrderWebhookDto.LineItemDto { Id = 1, Name = "Product A", ProductId = "SKU001", Quantity = 1, Price = 50.00m, Total = 50.00m }
                },
                Billing = new WooCommerceOrderWebhookDto.BillingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = "", Email = "", Phone = ""
                },
                Shipping = new WooCommerceOrderWebhookDto.ShippingDto
                {
                    FirstName = "John", LastName = "Doe", Address1 = "", City = "", State = "", Postcode = "", Country = ""
                }
            };

            _mockFoodicsService.Setup(fs => fs.UpdateProductStockAsync(tenantId, It.IsAny<FoodicsStockUpdateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Simulate failure

            SynchronizationLog? capturedLog = null;
            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => capturedLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _synchronizationService.ProcessWooCommerceOrderUpdatedAsync(tenantId, wooCommerceOrder, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Failed to update Foodics stock for product SKU001.", result.Message);

            _mockFoodicsService.Verify(fs => fs.UpdateProductStockAsync(tenantId, It.IsAny<FoodicsStockUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SyncFoodicsStockToWooCommerceAsync_SuccessfulStockSync_LogsSuccesses()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var foodicsStocks = new List<FoodicsStockUpdateDto>
            {
                new FoodicsStockUpdateDto { ProductReference = "SKU001", NewQuantity = 100 },
                new FoodicsStockUpdateDto { ProductReference = "SKU002", NewQuantity = 50 }
            };

            _mockFoodicsService.Setup(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(foodicsStocks);
            _mockWooCommerceService.Setup(ws => ws.UpdateProductStockAsync(tenantId, It.IsAny<WooCommerceStockUpdateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            SynchronizationLog? capturedInitialLog = null;
            List<SynchronizationLog> capturedUpdateLogs = new List<SynchronizationLog>();

            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) =>
                {
                    if (log.EventType == "StockSync") capturedInitialLog = log;
                    else capturedUpdateLogs.Add(log);
                })
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) =>
                {
                    if (log.EventType == "StockSync")
                    {
                        if (capturedInitialLog != null)
                        {
                            capturedInitialLog.Status = log.Status;
                            capturedInitialLog.Message = log.Message;
                            capturedInitialLog.UpdatedAt = log.UpdatedAt;
                        }
                    }
                    else
                    {
                        var existingLog = capturedUpdateLogs.FirstOrDefault(l => l.Id == log.Id);
                        if (existingLog != null) { existingLog.Status = log.Status; existingLog.Message = log.Message; existingLog.UpdatedAt = log.UpdatedAt; }
                    }
                })
                .Returns(Task.CompletedTask);

            // Act
            var results = await _synchronizationService.SyncFoodicsStockToWooCommerceAsync(tenantId, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(foodicsStocks.Count + 1, results.Count); // 1 initial log + 2 product update logs

            var overallLog = results.FirstOrDefault(l => l.EventType == "StockSync");
            Assert.NotNull(overallLog);
            Assert.Equal("Success", overallLog.Status);
            Assert.Contains("Foodics stock synchronization to WooCommerce completed.", overallLog.Message);

            _mockFoodicsService.Verify(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockWooCommerceService.Verify(ws => ws.UpdateProductStockAsync(tenantId, It.Is<WooCommerceStockUpdateDto>(dto => dto.ProductId == "SKU001" && dto.StockQuantity == 100), It.IsAny<CancellationToken>()), Times.Once);
            _mockWooCommerceService.Verify(ws => ws.UpdateProductStockAsync(tenantId, It.Is<WooCommerceStockUpdateDto>(dto => dto.ProductId == "SKU002" && dto.StockQuantity == 50), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(6)); // 1 for initial log, 2 for product updates, 1 for final log of initial, 2 for complete of update log

            var sku001Log = results.FirstOrDefault(l => l.SourceEntityId == "SKU001");
            Assert.NotNull(sku001Log);
            Assert.Equal("Success", sku001Log.Status);
            Assert.Contains("WooCommerce stock updated for product SKU001 to 100.", sku001Log.Message);

            var sku002Log = results.FirstOrDefault(l => l.SourceEntityId == "SKU002");
            Assert.NotNull(sku002Log);
            Assert.Equal("Success", sku002Log.Status);
            Assert.Contains("WooCommerce stock updated for product SKU002 to 50.", sku002Log.Message);
        }

        [Fact]
        public async Task SyncFoodicsStockToWooCommerceAsync_WooCommerceUpdateFails_LogsFailure()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var foodicsStocks = new List<FoodicsStockUpdateDto>
            {
                new FoodicsStockUpdateDto { ProductReference = "SKU001", NewQuantity = 100 }
            };

            _mockFoodicsService.Setup(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(foodicsStocks);
            // Simulate WooCommerce update failure for SKU001
            _mockWooCommerceService.Setup(ws => ws.UpdateProductStockAsync(tenantId, It.Is<WooCommerceStockUpdateDto>(dto => dto.ProductId == "SKU001"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            SynchronizationLog? capturedInitialLog = null;
            List<SynchronizationLog> capturedUpdateLogs = new List<SynchronizationLog>();

            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) =>
                {
                    if (log.EventType == "StockSync") capturedInitialLog = log;
                    else capturedUpdateLogs.Add(log);
                })
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) =>
                {
                    if (log.EventType == "StockSync")
                    {
                        if (capturedInitialLog != null)
                        {
                            capturedInitialLog.Status = log.Status;
                            capturedInitialLog.Message = log.Message;
                            capturedInitialLog.UpdatedAt = log.UpdatedAt;
                        }
                    }
                    else
                    {
                        var existingLog = capturedUpdateLogs.FirstOrDefault(l => l.Id == log.Id);
                        if (existingLog != null) { existingLog.Status = log.Status; existingLog.Message = log.Message; existingLog.UpdatedAt = log.UpdatedAt; }
                    }
                })
                .Returns(Task.CompletedTask);

            // Act
            var results = await _synchronizationService.SyncFoodicsStockToWooCommerceAsync(tenantId, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(foodicsStocks.Count + 1, results.Count); // 1 initial log + 1 product update log

            var overallLog = results.FirstOrDefault(l => l.EventType == "StockSync");
            Assert.NotNull(overallLog);
            Assert.Equal("Failed", overallLog.Status); // Overall sync should fail if any individual update fails
            Assert.Contains("Foodics stock synchronization to WooCommerce partially failed. See individual logs for details.", overallLog.Message);

            _mockFoodicsService.Verify(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockWooCommerceService.Verify(ws => ws.UpdateProductStockAsync(tenantId, It.IsAny<WooCommerceStockUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(4));

            var sku001Log = results.FirstOrDefault(l => l.SourceEntityId == "SKU001");
            Assert.NotNull(sku001Log);
            Assert.Equal("Failed", sku001Log.Status);
            Assert.Contains("Failed to update WooCommerce stock for product SKU001.", sku001Log.Message);
        }

        [Fact]
        public async Task SyncFoodicsStockToWooCommerceAsync_FoodicsApiFails_LogsOverallFailure()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            _mockFoodicsService.Setup(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Foodics API down")); // Simulate Foodics API failure

            SynchronizationLog? initialLog = null;

            _mockSynchronizationLogRepository.Setup(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => initialLog = log)
                .Returns(Task.CompletedTask);
            _mockSynchronizationLogRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()))
                .Callback<SynchronizationLog, CancellationToken>((log, token) => initialLog = log)
                .Returns(Task.CompletedTask);

            // Act
            var results = await _synchronizationService.SyncFoodicsStockToWooCommerceAsync(tenantId, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results); // Only the initial log should exist

            var overallLog = results.FirstOrDefault(l => l.EventType == "StockSync");
            Assert.NotNull(overallLog);
            Assert.Equal("Failed", overallLog.Status);
            Assert.Contains("Error during Foodics stock synchronization to WooCommerce: Foodics API down", overallLog.Message);

            _mockFoodicsService.Verify(fs => fs.GetFoodicsStockAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockWooCommerceService.Verify(ws => ws.UpdateProductStockAsync(It.IsAny<Guid>(), It.IsAny<WooCommerceStockUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockSynchronizationLogRepository.Verify(repo => repo.AddAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockSynchronizationLogRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SynchronizationLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}