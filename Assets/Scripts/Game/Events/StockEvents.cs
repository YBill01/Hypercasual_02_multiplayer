using R3;
using System.Collections.Generic;

public class StockEvents
{
	public readonly Subject<StockLink> OnLink = new();
	public readonly Subject<StockLink> OnUnlink = new();

	public readonly Subject<StockTransfer> OnTransfer = new();
	public readonly Subject<List<StockTransfer>> OnTransferExecute = new();

	public readonly Subject<(Stock, StockItem)> OnAdd = new();
	public readonly Subject<(Stock, StockItem)> OnTake = new();
}