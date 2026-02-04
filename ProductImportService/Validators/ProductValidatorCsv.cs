using FluentValidation;
using ProductImportService.DTO;

namespace ProductImportService.Validators
{
    public class ProductCsvRowValidator : AbstractValidator<ProductImportCsv>
    {
        public ProductCsvRowValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        }
    }
}
