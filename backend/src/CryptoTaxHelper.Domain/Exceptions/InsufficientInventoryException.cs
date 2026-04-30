namespace CryptoTaxHelper.Domain.Exceptions;

public class InsufficientInventoryException : Exception
{
    public InsufficientInventoryException(string currency, double requested, double available)
        : base($"Not enough {currency} inventory to sell. Requested: {requested}, Available: {available}")
    {
        Currency = currency;
        Requested = requested;
        Available = available;
    }

    public string Currency { get; }
    public double Requested { get; }
    public double Available { get; }
}
