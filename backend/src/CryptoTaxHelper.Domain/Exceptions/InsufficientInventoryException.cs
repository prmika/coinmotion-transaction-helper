namespace CryptoTaxHelper.Domain.Exceptions;

public class InsufficientInventoryException : Exception
{
    public InsufficientInventoryException(string currency, decimal requested, decimal available)
        : base($"Not enough {currency} inventory to sell. Requested: {requested}, Available: {available}")
    {
        Currency = currency;
        Requested = requested;
        Available = available;
    }

    public string Currency { get; }
    public decimal Requested { get; }
    public decimal Available { get; }
}
