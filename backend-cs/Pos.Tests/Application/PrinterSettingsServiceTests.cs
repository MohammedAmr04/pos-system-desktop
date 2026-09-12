using System;
using System.Collections.Generic;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakePrinterSettingsRepository : IPrinterSettingsRepository
    {
        public PrinterSettings Stored;
        public int SaveCalls;

        public PrinterSettings Get() =>
            Stored ?? PrinterSettings.Defaults();

        public void Save(PrinterSettings settings)
        {
            settings.Normalize();
            Stored = settings;
            SaveCalls++;
        }
    }

    public class PrinterSettingsServiceTests
    {
        [Fact]
        public void Get_Returns_Defaults_When_Nothing_Stored()
        {
            var settings = new PrinterSettingsService(new FakePrinterSettingsRepository()).Get();

            Assert.Equal("Xprinter", settings.ReceiptPrinterName);
            Assert.Equal(80, settings.PaperWidthMm);
            Assert.Equal(576, settings.RasterWidth);
            Assert.Equal(1, settings.Copies);
            Assert.True(settings.AutoCut);
            Assert.False(settings.OpenCashDrawer);
        }

        [Fact]
        public void Paper_Width_58_Uses_Narrow_Raster()
        {
            var repo = new FakePrinterSettingsRepository();
            var service = new PrinterSettingsService(repo);

            var saved = service.Save(new PrinterSettings { PaperWidthMm = 58 });

            Assert.Equal(384, saved.RasterWidth);
            Assert.Equal(58, saved.PaperWidthMm);
        }

        [Fact]
        public void Invalid_Paper_Width_Is_Rejected()
        {
            Assert.Throws<DomainValidationException>(() =>
                new PrinterSettingsService(new FakePrinterSettingsRepository()).Save(
                    new PrinterSettings { PaperWidthMm = 72 }));
        }

        [Fact]
        public void Missing_Printer_Names_Are_Rejected_And_Copies_Clamped_By_Normalize()
        {
            var service = new PrinterSettingsService(new FakePrinterSettingsRepository());

            Assert.Throws<DomainValidationException>(() =>
                service.Save(new PrinterSettings { ReceiptPrinterName = " ", LabelPrinterName = "L" }));
            Assert.Throws<DomainValidationException>(() =>
                service.Save(new PrinterSettings { ReceiptPrinterName = "R", LabelPrinterName = " " }));

            var repo = new FakePrinterSettingsRepository();
            var saved = service.Save(new PrinterSettings
            {
                ReceiptPrinterName = "R",
                LabelPrinterName = "L",
                Copies = 99
            });
            Assert.Equal(5, saved.Copies);
        }

        [Fact]
        public void Save_Persists_And_Get_Roundtrips_All_Fields()
        {
            var repo = new FakePrinterSettingsRepository();
            var service = new PrinterSettingsService(repo);

            service.Save(new PrinterSettings
            {
                ReceiptPrinterName = "POS-80",
                LabelPrinterName = "Label-58",
                PaperWidthMm = 58,
                Copies = 2,
                AutoCut = false,
                OpenCashDrawer = true,
                ShowLogo = false,
                StoreName = "متجر التجربة",
                StorePhone = "01000000000",
                ReceiptFooter = "نراك قريباً"
            });

            var loaded = service.Get();
            Assert.Equal("POS-80", loaded.ReceiptPrinterName);
            Assert.Equal("Label-58", loaded.LabelPrinterName);
            Assert.Equal(2, loaded.Copies);
            Assert.False(loaded.AutoCut);
            Assert.True(loaded.OpenCashDrawer);
            Assert.False(loaded.ShowLogo);
            Assert.Equal("متجر التجربة", loaded.StoreName);
            Assert.Equal("نراك قريباً", loaded.ReceiptFooter);
        }
    }
}
