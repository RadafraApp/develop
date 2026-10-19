using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RadafraApp.Binding
{
    // Accepts both "." and "," as the decimal separator regardless of the request culture.
    // Without this, an invariant-culture parse turns a client-sent "0,5" into 5
    // (a comma is the thousands separator in the invariant culture).
    public class FlexibleDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

            var raw = valueResult.FirstValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Task.CompletedTask;
            }

            var normalized = raw.Trim().Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    "El valor debe ser un número válido.");
            }

            return Task.CompletedTask;
        }
    }

    public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var type = context.Metadata.ModelType;
            if (type == typeof(decimal) || type == typeof(decimal?))
            {
                return new FlexibleDecimalModelBinder();
            }

            return null;
        }
    }
}
