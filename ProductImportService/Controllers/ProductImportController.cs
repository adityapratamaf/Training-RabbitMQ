using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using MassTransit;
using Messaging;
using Microsoft.AspNetCore.Mvc;
using ProductImportService.DTO;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ProductImportService.Controllers;

[ApiController]
[Route("api/import/products")]
public class ProductImportController : ControllerBase
{
    private readonly IValidator<ProductImportCsv> _validator;
    private readonly IPublishEndpoint _publish;
   
    public ProductImportController(IValidator<ProductImportCsv> validator, IPublishEndpoint publish)
    {
        _validator = validator;
        _publish = publish;
    }

    // File Upload
    public class ImportCsvRequest
    {
        public IFormFile File { get; set; } = default!;
    }

    [HttpPost("csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCsv([FromForm] ImportCsvRequest request)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("File Required.");
        
        // buat BatchId
        var batchId = Guid.NewGuid();

        // baca CSV
        List<(int RowNumber, ProductImportCsv Row)> rows;
        try
        {
            rows = await ReadCsv(request.File);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "CSV format invalid", detail = ex.Message });
        }

        // validasi & cek duplikat SKU
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // HashSet untuk cek duplikat
        var errors = new List<object>();
        var validRows = new List<(int RowNumber, ProductImportCsv Row)>();

        // validasi baris per baris
        foreach (var (rowNumber, row) in rows)
        {
            var sku = (row.Sku ?? "").Trim();

            // duplikasi SKU cek
            if (!string.IsNullOrWhiteSpace(sku) && !seen.Add(sku))
            {
                errors.Add(new 
                { 
                    row = rowNumber, 
                    field = "Sku", 
                    error = $"Duplikat SKU in File: {sku}" 
                });
                continue;
            }

            // validasi menggunakan FluentValidation
            var result = await _validator.ValidateAsync(row);
            if (!result.IsValid)
            {
                foreach (var e in result.Errors)
                    errors.Add(new { row = rowNumber, field = e.PropertyName, error = e.ErrorMessage });
                continue;
            }

            validRows.Add((rowNumber, row));
        }

        // publish pesan untuk baris yang valid ke RabbitMQ
        foreach (var (rowNumber, row) in validRows)
        {
            await _publish.Publish(
                new ProductUpsertMessage(batchId, rowNumber, row.Sku.Trim(), row.Name, row.Description, row.Price, row.Stock, row.IsActive),
                ctx => ctx.SetRoutingKey("staging") // routing key
            );
        }

        return Ok(new
        {
            batchId,
            totalRows = rows.Count,
            acceptedRows = validRows.Count,
            rejectedRows = errors.Count,
            errors 
        });
    }

    // helper baca CSV 
    private static async Task<List<(int RowNumber, ProductImportCsv Row)>> ReadCsv(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        // deteksi delimiter dari header
        var headerLine = await reader.ReadLineAsync();
        if (headerLine is null) throw new Exception("CSV empty");

        var delimiter = headerLine.Contains(';') ? ";" : ",";

        stream.Position = 0;
        reader.DiscardBufferedData();

        // konfigurasi CsvHelper
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = delimiter,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using var csv = new CsvReader(reader, config);

        var result = new List<(int, ProductImportCsv)>();

        await csv.ReadAsync();
        csv.ReadHeader();

        // baca baris per baris
        while (await csv.ReadAsync())
        {
            var row = csv.GetRecord<ProductImportCsv>()!;
            var rowNumber = csv.Context.Parser!.Row;
            result.Add((rowNumber, row));
        }

        return result;
    }
}
