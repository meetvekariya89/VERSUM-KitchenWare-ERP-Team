
using System;
using System.Collections.Generic;

namespace VERSUM.KitchenWareERP.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalRawMaterials { get; set; }
        public int LowStockProducts { get; set; }
        public int LowStockRawMaterials { get; set; }
        public int TotalDealers { get; set; }
        public int TotalProductionBatches { get; set; }
        public int TotalSalesInvoices { get; set; }

        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal FinishedInventoryValue { get; set; }

        public List<Product> RecentProducts { get; set; } = new();
        public List<Sale> RecentSales { get; set; } = new();
        public List<Purchase> RecentPurchases { get; set; } = new();

        public List<DashboardMonth> MonthlySales { get; set; } = new();
        public List<DashboardLowStock> StockAlerts { get; set; } = new();
    }

    public class DashboardMonth
    {
        public string Label { get; set; } = "";
        public decimal Sales { get; set; }
    }

    public class DashboardLowStock
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
    }
}
